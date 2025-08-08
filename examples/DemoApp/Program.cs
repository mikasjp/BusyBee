using Ignis;
using Ignis.Queue;
using System.Collections.Concurrent;

var executionLog = new ConcurrentBag<string>();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services
    .AddIgnis()
    .WithUnboundedQueue()
    .WithGlobalJobTimeout(TimeSpan.FromMilliseconds(4500))
    .WithJobTimeoutLogging()
    .WithJobBatchSize(1);

var app = builder.Build();

app
    .MapPost("/queue", async (IBackgroundQueue queue, CancellationToken cancellationToken) =>
    {
        var jobId = await queue.Enqueue(async (_, ctx, ct) =>
        {
            executionLog.Add($"{ctx.JobId} waited for {(ctx.StartedAt-ctx.QueuedAt).TotalMilliseconds} ms and started at {ctx.StartedAt}");
            await Task.Delay(Random.Shared.Next(3000, 6000), ct); // Simulate work
            var finishedAt = DateTimeOffset.UtcNow;
            executionLog.Add(
                $"Job {ctx.JobId} finished at {finishedAt}, took {(finishedAt - ctx.StartedAt).TotalMilliseconds} ms");
        }, cancellationToken);

        return Results.Ok(new { JobId = jobId });
    })
    .WithName("EnqueueJob");
app
    .MapGet("/queue", () => Results.Ok(new
    {
        JobsExecutionLog = executionLog
    }))
    .WithName("GetQueueExecutionLog");

app.UseSwagger();
app.UseSwaggerUI();

app.Run();