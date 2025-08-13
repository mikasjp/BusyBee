using Ignis.Queue;
using System.Diagnostics;
using System.Threading.Channels;
using Ignis.Processor.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Ignis.Processor;

internal sealed class ProcessorService(
    Queue.Queue queue,
    IOptions<ProcessorOptions> options,
    IServiceProvider serviceProvider,
    ILogger<ProcessorService> logger) : BackgroundService

{
    private readonly ActivitySource _activitySource = new(Tracing.Constants.TraceSourceName);
    private readonly Channel<object> _slotTracker = Channel.CreateBounded<object>(
        new BoundedChannelOptions(options.Value.ParallelJobsCount ?? 1)
        {
            FullMode = BoundedChannelFullMode.Wait
        });

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        HashSet<Task> activeTasks = [];
        for (var i = 0; i < options.Value.ParallelJobsCount; i++)
        {
            await _slotTracker.Writer.WriteAsync(new object(), stoppingToken);
        }
        
        while (!stoppingToken.IsCancellationRequested)
        {
            var availableSlots = await _slotTracker.Reader.ReadAvailable(stoppingToken);
            try
            {
                var jobItems = (await queue.DequeueBatch(availableSlots.Count, stoppingToken))
                    .ToArray();

                foreach (var slot in availableSlots.Take(availableSlots.Count - jobItems.Length))
                {
                    await _slotTracker.Writer.WriteAsync(slot, stoppingToken);
                }
                
                var tasks = jobItems
                    .Select(jobItem => ProcessJob(jobItem, stoppingToken));
                foreach (var task in tasks)
                {
                    activeTasks.Add(task);
                }
                
                await Task.WhenAny(activeTasks);
                activeTasks.RemoveWhere(t => t.IsCompleted || t.IsFaulted || t.IsCanceled);
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

    private async Task ProcessJob(JobWrapper jobItem, CancellationToken stoppingToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var activityName = BuildActivityName(jobItem.JobId);
        using var activity = _activitySource
            .StartActivity(activityName, ActivityKind.Internal, jobItem.ActivityContext ?? new ActivityContext());
        activity?.AddTag(nameof(jobItem.JobId), jobItem.JobId);
        var jobTimeoutCancellationToken = GetJobCancellationToken(jobItem.Timeout);
        var combinedCancellationToken = CancellationTokenSource
            .CreateLinkedTokenSource(stoppingToken, jobTimeoutCancellationToken)
            .Token;
        var stopwatch = Stopwatch.StartNew();
        try
        {
            logger.LogDebug("Starting job {JobId}", jobItem.JobId);
            var context = new JobContext(
                JobId: jobItem.JobId,
                QueuedAt: jobItem.QueuedAt,
                StartedAt: DateTimeOffset.UtcNow);
            await jobItem.Job(scope.ServiceProvider, context, combinedCancellationToken);
        }
        catch (TaskCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogDebug("Job {JobId} was cancelled due to graceful shutdown", jobItem.JobId);
            throw;
        }
        catch (TaskCanceledException) when (jobTimeoutCancellationToken.IsCancellationRequested)
        {
            logger.Log(
                options.Value.JobTimeoutLogLevel ?? LogLevel.None,
                "Job {JobId} was cancelled due to timeout",
                jobItem.JobId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while processing job {JobId}", jobItem.JobId);
        }
        finally
        {
            logger.LogDebug("Job {JobId} finished in {ElapsedMilliseconds} ms",
                jobItem.JobId,
                stopwatch.ElapsedMilliseconds);
            await _slotTracker.Writer.WriteAsync(new object(), CancellationToken.None);
        }
    }

    private CancellationToken GetJobCancellationToken(TimeSpan? timeout)
    {
        var jobTimeout = timeout ?? options.Value.JobTimeout;
        return jobTimeout is not null
            ? new CancellationTokenSource(jobTimeout.Value).Token
            : CancellationToken.None;
    }

    private static string BuildActivityName(Guid jobId)
    {
        return $"IgnisJob-{jobId:D}";
    }
}