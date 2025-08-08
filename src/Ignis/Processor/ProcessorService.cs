using System.Diagnostics;
using Ignis.Queue;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Ignis.Processor;

internal sealed class ProcessorService(
    Queue.Queue queue,
    IOptions<ProcessorOptions> options,
    IServiceProvider serviceProvider,
    ILogger<ProcessorService> logger) : BackgroundService

{
    private readonly ActivitySource _activitySource = new(Tracing.Constants.TraceSourceName);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var batchSize = options.Value.JobsBatchSize ?? 1;

        while (!stoppingToken.IsCancellationRequested)
        {
            var jobItems = await queue.DequeueBatch(batchSize, stoppingToken);
            var tasks = jobItems
                .Select(jobItem => ProcessJob(jobItem, stoppingToken))
                .ToArray();
            try
            {
                logger.LogDebug("Processing a batch of {JobCount} jobs", tasks.Length);
                var stopwatch = Stopwatch.StartNew();
                await Task.WhenAll(tasks);
                stopwatch.Stop();
                logger.LogDebug(
                    "Processed a batch of {JobCount} jobs in {ElapsedMilliseconds} ms",
                    tasks.Length,
                    stopwatch.ElapsedMilliseconds);
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