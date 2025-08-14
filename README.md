# Ignis

<p align="center">
  <img src="assets/logo.jpg" alt="Ignis Logo" width="200" />
</p>

<p align="center">
  <strong>🔥 Fast and observable background job processing for .NET</strong>
</p>

---

Ignis is a high-performance .NET background processing library built on native channels. It provides a simple, configurable, and observable solution for handling background tasks with built-in OpenTelemetry support and flexible queue management.

## Installation

```bash
dotnet add package Ignis
```

## Quick Start

Register Ignis in your DI container and start processing background jobs:

```csharp
// Program.cs
builder.Services.AddIgnis();

// Inject IBackgroundQueue and enqueue jobs
await queue.Enqueue(async (services, context, cancellationToken) =>
{
    var logger = services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Processing job {JobId}", context.JobId);
    
    await Task.Delay(1000, cancellationToken);
}, cancellationToken);
```

## Features

### 🚀 **High Performance**
- In-memory queues built on .NET channels for efficiency

### ⚙️ **Highly Configurable**
- Unbounded or bounded queues with multiple overflow strategies
- Configurable job timeouts both globally and per job
- Parallel job processing with configurable slots pool size

### 📊 **Built-in Observability**
- Job execution flow logging
- Tracing ready for OpenTelemetry
- Detailed OpenTelemetry ready metrics (jobs count, execution times, wait times, and more...)

### 🔧 **Developer Friendly**
- Fluent configuration API
- Full dependency injection support
- Comprehensive cancellation token support
- Rich job context information

## Configuration

### Basic Configuration

```csharp
builder.Services
    .AddIgnis()
    .WithUnboundedQueue()
    .WithGlobalJobTimeout(TimeSpan.FromSeconds(30))
    .WithLevelOfParallelism(10);
```

### Queue Configuration

**Unbounded Queue** - No capacity limits:
```csharp
builder.Services.AddIgnis().WithUnboundedQueue();
```

**Bounded Queue** - With capacity and overflow handling:
```csharp
// Throw exception when queue is full
builder.Services.AddIgnis()
    .WithBoundedQueue(capacity: 1000, OverflowStrategy.ThrowException);

// Drop oldest jobs when queue is full
builder.Services.AddIgnis()
    .WithBoundedQueue(capacity: 1000, OverflowStrategy.DropOldest);
```

Supported overflow strategies:

* Wait - Wait until space is available,
* Ignore - Ignore the job if queue is full,
* ThrowException - Throw an exception if queue is full,
* DiscardOldest - Discard the oldest job in the queue,
* DiscardNewest - Discard the newest job in the queue

### Timeout Management

```csharp
// Set global timeout for all jobs
builder.Services.AddIgnis()
    .WithGlobalJobTimeout(TimeSpan.FromSeconds(30));
```

### Performance Tuning

```csharp
// Process multiple jobs in parallel
builder.Services.AddIgnis()
    .WithLevelOfParallelism(20);
```

## Job Context

Every job receives a rich context with useful information:

```csharp
await queue.Enqueue(async (services, context, cancellationToken) =>
{
    // Unique job identifier
    var jobId = context.JobId;
    
    // Timing information
    var queuedAt = context.QueuedAt;
    var startedAt = context.StartedAt;
    var waitTime = startedAt - queuedAt;
    
    // Access any registered service
    var myService = services.GetRequiredService<IMyService>();
});
```

## OpenTelemetry Integration
Ignis supports OpenTelemetry for metrics and tracing. This allows you to monitor and analyze job performance in production environments.
Enable OpenTelemetry in your application:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource(Ignis.Observability.TracingConstants.TraceSourceName)
        .AddConsoleExporter())
    .WithMetrics(metrics => metrics
        .AddMeter(Ignis.Observability.MetricsConstants.MeterName)
        .AddPrometheusExporter());
```

## Real-World Example

Here's a complete example of using Ignis in a web API:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Configure Ignis for production workload
builder.Services
    .AddIgnis()
    .WithBoundedQueue(capacity: 10000, OverflowStrategy.DropOldest)
    .WithGlobalJobTimeout(TimeSpan.FromMinutes(5))
    .WithLevelOfParallelism(5);

// Register your services
builder.Services.AddScoped<IEmailService, EmailService>();

var app = builder.Build();

// API endpoint for sending emails
app.MapPost("/send-email", async (
    IBackgroundQueue queue,
    EmailRequest request,
    CancellationToken cancellationToken) =>
{
    var jobId = await queue.Enqueue(async (services, context, ct) =>
    {
        var emailService = services.GetRequiredService<IEmailService>();
        var logger = services.GetRequiredService<ILogger<Program>>();
        
        logger.LogInformation("Sending email for job {JobId}", context.JobId);
        
        await emailService.SendAsync(request.To, request.Subject, request.Body, ct);
        
        logger.LogInformation("Email sent successfully for job {JobId}", context.JobId);
    }, cancellationToken);

    return Results.Accepted(new { JobId = jobId });
});

app.Run();
```

## Advanced Usage

### Errors and timeouts handling

Implement and register your own `IJobFailureHandler` to handle job failures.

```csharp
services.AddIgnis()
    .WithJobFailureHandler<MyCustomJobFailureHandler>();
```

To handle job timeouts, you can implement and register `IJobTimeoutHandler`:

```csharp
services.AddIgnis()
    .WithJobTimeoutHandler<MyCustomJobTimeoutHandler>();
```

### Long-Running Jobs

```csharp
await queue.Enqueue(async (services, context, cancellationToken) =>
{
    var logger = services.GetRequiredService<ILogger>();
    
    for (int i = 0; i < 1000; i++)
    {
        // Check for cancellation periodically
        cancellationToken.ThrowIfCancellationRequested();
        
        await ProcessItemAsync(i, cancellationToken);
        
        // Log progress
        if (i % 100 == 0)
        {
            logger.LogInformation("Job {JobId}: Processed {Count}/1000 items", 
                context.JobId, i);
        }
    }
}, cancellationToken);
```

## Best Practices

1. **Keep jobs idempotent** - Design jobs to be safely retryable
2. **Use appropriate timeouts** - Set realistic timeouts based on your job complexity
3. **Monitor jobs** - Use OpenTelemetry to track jobs
4. **Handle cancellation** - Always respect `CancellationToken` in long-running jobs

## Contributing

We welcome contributions! Please feel free to:

- 🐛 Report bugs
- 💡 Suggest new features
- 🔧 Submit pull requests
- 📖 Improve documentation