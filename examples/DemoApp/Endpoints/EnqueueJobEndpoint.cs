using System.Collections.Concurrent;
using BusyBee.Processor;
using BusyBee.Queue;

namespace DemoApp.Endpoints;

internal static class EnqueueJobEndpoint
{
    public static IEndpointRouteBuilder MapEnqueueJob(this IEndpointRouteBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder
            .MapPost("/queue", async (
                IBackgroundQueue queue,
                CancellationToken cancellationToken) =>
            {
                var jobId = await queue.Enqueue(Job, cancellationToken);

                return Results.Ok(new { JobId = jobId });
            })
            .WithName("EnqueueJob")
            .WithDescription("Enqueues a job that simulates work and may fail and time out randomly.");

        return builder;
    }

    private static async Task Job(IServiceProvider serviceProvider, JobContext ctx, CancellationToken cancellationToken)
    {
        // Retrieve the execution log from the service provider
        var executionLog = serviceProvider.GetRequiredService<ConcurrentBag<LogEntry>>();

        // Simulate a job failure with a 10% chance
        if (Random.Shared.Next(0, 9) == 0)
        {
            executionLog.Add(
                new LogEntry(
                    Timestamp: DateTimeOffset.UtcNow,
                    JobEnqueuedAt: ctx.QueuedAt,
                    JobId: ctx.JobId,
                    Message: $"Job {ctx.JobId} failed due to simulated error."));
            throw new Exception("Simulated job failure");
        }

        executionLog.Add(
            new LogEntry(
                Timestamp: DateTimeOffset.UtcNow,
                JobEnqueuedAt: ctx.QueuedAt,
                JobId: ctx.JobId,
                Message:
                $"{ctx.JobId} waited for {(ctx.StartedAt - ctx.QueuedAt).TotalMilliseconds} ms and started at {ctx.StartedAt}"));

        await Task.Delay(Random.Shared.Next(3000, 5000), cancellationToken); // Simulate work which may time out

        var finishedAt = DateTimeOffset.UtcNow;
        executionLog.Add(
            new LogEntry(
                Timestamp: finishedAt,
                JobEnqueuedAt: ctx.QueuedAt,
                JobId: ctx.JobId,
                Message:
                $"Job {ctx.JobId} finished at {finishedAt}, took {(finishedAt - ctx.StartedAt).TotalMilliseconds} ms"));
    }
}