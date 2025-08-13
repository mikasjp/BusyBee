using System.Diagnostics;
using Ignis.Observability;
using Ignis.Queue;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ignis.Processor;

internal sealed class JobRunner(
    IOptions<ProcessorOptions> options,
    ILogger<JobRunner> logger,
    IServiceProvider serviceProvider,
    Metrics metrics,
    SlotsTracker slotTracker)
{
    private readonly ActivitySource _activitySource = new(TracingConstants.TraceSourceName);
    
    public async Task RunJob(JobWrapper jobItem, CancellationToken stoppingToken)
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
            metrics.TotalSuccessfulJobsCounter.Add(1);
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
            metrics.TotalTimedOutJobsCounter.Add(1);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while processing job {JobId}", jobItem.JobId);
            metrics.TotalFailedJobsCounter.Add(1);
        }
        finally
        {
            stopwatch.Stop();
            logger.LogDebug("Job {JobId} finished in {ElapsedMilliseconds} ms",
                jobItem.JobId,
                stopwatch.ElapsedMilliseconds);
            await slotTracker.ReleaseSlots(1, stoppingToken);
            metrics.ActiveJobsCounter.Add(-1);
            metrics.TotalProcessedJobsCounter.Add(1);
            metrics.JobProcessingDurationHistogram.Record(stopwatch.ElapsedMilliseconds);
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