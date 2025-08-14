using System.Collections.Concurrent;
using Ignis.Queue;

namespace DemoApp.Endpoints;

internal static class EnqueueJobEndpoint
{
    public static IEndpointRouteBuilder MapEnqueueJob(this IEndpointRouteBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder
            .MapPost("/queue", async (
                IBackgroundQueue queue,
                ConcurrentBag<LogEntry> executionLog,
                CancellationToken cancellationToken) =>
            {
                var jobId = await queue.Enqueue(async (_, ctx, ct) =>
                {
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
                            Message: $"{ctx.JobId} waited for {(ctx.StartedAt - ctx.QueuedAt).TotalMilliseconds} ms and started at {ctx.StartedAt}"));
                    
                    await Task.Delay(Random.Shared.Next(3000, 5000), ct); // Simulate work
                    
                    var finishedAt = DateTimeOffset.UtcNow;
                    executionLog.Add(
                        new LogEntry(
                            Timestamp: finishedAt,
                            JobEnqueuedAt: ctx.QueuedAt,
                            JobId: ctx.JobId,
                            Message: $"Job {ctx.JobId} finished at {finishedAt}, took {(finishedAt - ctx.StartedAt).TotalMilliseconds} ms"));
                }, cancellationToken);

                return Results.Ok(new { JobId = jobId });
            })
            .WithName("EnqueueJob");

        return builder;
    }
}