using Ignis;
using Ignis.Queue;
using System.Collections.Concurrent;
using Ignis.Observability;
using OpenTelemetry.Metrics;

var executionLog = new ConcurrentDictionary<DateTimeOffset, string>();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services
    .AddOpenTelemetry()
    .WithTracing(b => b
        .AddSource(TracingConstants.TraceSourceName))
    .WithMetrics(b => b
        .AddMeter(MetricsConstants.MeterName)
        .AddPrometheusExporter());
builder.Services
    .AddIgnis()
    .WithUnboundedQueue()
    .WithGlobalJobTimeout(TimeSpan.FromMilliseconds(4000))
    .WithLevelOfParallelism(5);

var app = builder.Build();

app
    .MapPost("/queue", async (IBackgroundQueue queue, CancellationToken cancellationToken) =>
    {
        var jobId = await queue.Enqueue(async (_, ctx, ct) =>
        {
            executionLog.TryAdd(DateTimeOffset.UtcNow,
                $"{ctx.JobId} waited for {(ctx.StartedAt - ctx.QueuedAt).TotalMilliseconds} ms and started at {ctx.StartedAt}");
            await Task.Delay(Random.Shared.Next(3000, 5000), ct); // Simulate work
            var finishedAt = DateTimeOffset.UtcNow;
            executionLog.TryAdd(
                finishedAt,
                $"Job {ctx.JobId} finished at {finishedAt}, took {(finishedAt - ctx.StartedAt).TotalMilliseconds} ms");
        }, cancellationToken);

        return Results.Ok(new { JobId = jobId });
    })
    .WithName("EnqueueJob");
app
    .MapGet("/queue", () => Results.Ok(new
    {
        JobsExecutionLog = executionLog.OrderBy(x => x.Key).Select(x => x.Value)
    }))
    .WithName("GetQueueExecutionLog");

app.UseSwagger();
app.UseSwaggerUI();
app.MapPrometheusScrapingEndpoint();

app.Run();