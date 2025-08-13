using System.Diagnostics;
using Ignis.Abstractions;
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
        var context = new JobContext(
            JobId: jobItem.JobId,
            QueuedAt: jobItem.QueuedAt,
            StartedAt: DateTimeOffset.UtcNow);
        try
        {
            logger.LogDebug("Starting job {JobId}", jobItem.JobId);
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
            await HandleCancellation(context, scope.ServiceProvider, stoppingToken);
        }
        catch (Exception ex)
        {
            await HandleFailure(context, ex, scope.ServiceProvider, stoppingToken);
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

    private async Task HandleFailure(
        JobContext context, Exception ex, IServiceProvider scopedServiceProvider, CancellationToken stoppingToken)
    {
        logger.LogError(ex, "An error occurred while processing job {JobId}", context.JobId);
        metrics.TotalFailedJobsCounter.Add(1);
        var failureHandler = scopedServiceProvider.GetService<IJobFailureHandler>();
        if (failureHandler is not null)
        {
            try
            {
                await failureHandler.Handle(context, ex, stoppingToken);
            }
            catch (Exception e)
            {
                logger.LogError(e,
                    "An error occurred while handling failure for job {JobId} using {FailureHandler}",
                    context.JobId, failureHandler.GetType().Name);
            }
        }
    }

    private async Task HandleCancellation(
        JobContext context, IServiceProvider scopedServiceProvider, CancellationToken stoppingToken)
    {
        logger.LogDebug("Job {JobId} was cancelled due to timeout", context.JobId);
        metrics.TotalTimedOutJobsCounter.Add(1);
        var timeoutHandler = scopedServiceProvider.GetService<IJobTimeoutHandler>();
        if (timeoutHandler is not null)
        {
            try
            {
                await timeoutHandler.Handle(context, stoppingToken);
            }
            catch (Exception e)
            {
                logger.LogError(e,
                    "An error occurred while handling timeout for job {JobId} using {TimeoutHandler}",
                    context.JobId, timeoutHandler.GetType().Name);
            }
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