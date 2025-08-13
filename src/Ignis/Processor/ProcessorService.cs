using Ignis.Queue;
using Ignis.Observability;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ignis.Processor;

internal sealed class ProcessorService(
    Queue.Queue queue,
    JobRunner jobRunner,
    ILogger<ProcessorService> logger,
    SlotsTracker slotTracker,
    Metrics metrics) : BackgroundService

{
    private readonly HashSet<Task> _activeTasks = [];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await slotTracker.InitializeSlots(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var availableSlotsCount = await slotTracker.ReleaseAvailableSlots(stoppingToken);
                var jobItems = (await queue.DequeueBatch(availableSlotsCount, stoppingToken))
                    .ToArray();

                logger.LogDebug("Dequeued {JobCount} jobs for processing", jobItems.Length);

                await slotTracker.ReleaseSlots(availableSlotsCount - jobItems.Length, stoppingToken);

                ActivateJobs(jobItems, stoppingToken);

                await Task.WhenAny(_activeTasks);
                _activeTasks.RemoveWhere(t => t.IsCompleted || t.IsFaulted || t.IsCanceled);
            }
            catch (TaskCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                logger.LogDebug("Gracefully stopping job processing as cancellation was requested");
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while processing jobs");
            }
        }
    }

    private void ActivateJobs(JobWrapper[] jobItems, CancellationToken stoppingToken)
    {
        var tasks = jobItems
            .Select(jobItem =>
            {
                var jobWaitingTime = (DateTimeOffset.UtcNow - jobItem.QueuedAt).TotalMilliseconds;
                metrics.WaitingTimeHistogram.Record(jobWaitingTime);
                return jobRunner.RunJob(jobItem, stoppingToken);
            });
        foreach (var task in tasks)
        {
            _activeTasks.Add(task);
            metrics.ActiveJobsCounter.Add(1);
        }
    }
}