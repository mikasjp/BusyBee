using BusyBee;
using DemoApp;
using System.Collections.Concurrent;
using DemoApp.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.AddSwaggerModule();
builder.AddOpenTelemetryModule();

builder.Services.AddSingleton<ConcurrentBag<LogEntry>>();

builder.Services
    .AddBusyBee()
    .WithUnboundedQueue()
    .WithGlobalJobTimeout(TimeSpan.FromMilliseconds(14000))
    .WithLevelOfParallelism(5)
    .WithJobTimeoutHandler<JobTimeoutHandler>()
    .WithJobFailureHandler<JobFailureHandler>()
    .WithManualCancellationHandler<ManualCancellationHandler>();

var app = builder.Build();
app.MapEnqueueJob();
app.MapGetQueueExecutionLog();
app.MapCancelJob();

app.UseSwagger();
app.UseSwaggerUI();
app.MapPrometheusScrapingEndpoint();

app.Run();