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
    .WithGlobalJobTimeout(TimeSpan.FromMilliseconds(4000))
    .WithLevelOfParallelism(5)
    .WithJobTimeoutHandler<JobTimeoutHandler>()
    .WithJobFailureHandler<JobFailureHandler>();

var app = builder.Build();
app.MapEnqueueJob();
app.MapGetQueueExecutionLog();

app.UseSwagger();
app.UseSwaggerUI();
app.MapPrometheusScrapingEndpoint();

app.Run();