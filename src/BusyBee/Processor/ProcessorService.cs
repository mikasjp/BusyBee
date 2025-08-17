using BusyBee.Observability;
using BusyBee.Queue;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BusyBee.Processor;

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
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await slotTracker.ReserveSlot(stoppingToken);
                var jobItem = await queue.Dequeue(stoppingToken);
                logger.LogDebug("Dequeued {JobId} job for processing", jobItem.JobId);

                ActivateJob(jobItem, stoppingToken);
                
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

    private void ActivateJob(JobWrapper jobItem, CancellationToken stoppingToken)
    {
        var jobWaitingTime = (DateTimeOffset.UtcNow - jobItem.QueuedAt).TotalMilliseconds;
        metrics.WaitingTimeHistogram.Record(jobWaitingTime);
        var task = jobRunner.RunJob(jobItem, stoppingToken);
        _activeTasks.Add(task);
        metrics.ActiveJobsCounter.Add(1);
    }
}