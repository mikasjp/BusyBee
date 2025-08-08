using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Ignis.Processor;

internal sealed class ProcessorService(
    Queue.Queue queue,
    IServiceProvider serviceProvider) : BackgroundService

{
    private readonly ActivitySource _activitySource = new(Tracing.Constants.TraceSourceName);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // TODO: Use async Linq to process multiple jobs concurrently in a batch (configurable degree of parallelism) - Take + Task.WhenAll
            
            var jobItem = await queue.Dequeue(stoppingToken);

            await using var scope = serviceProvider.CreateAsyncScope();
            using var activity = _activitySource
                .StartActivity(ActivityKind.Internal, jobItem.ActivityContext ?? new ActivityContext());
            try
            {
                // TODO: Implement configurable job timeout handling by using a CancellationTokenSource
                // TODO: Implement configurable job retry logic
                await jobItem.Job(scope.ServiceProvider, stoppingToken);
            }
            catch (Exception ex)
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<ProcessorService>>();
                logger.LogError(ex, "An error occurred while processing job {JobId}", jobItem.JobId);
            }
        }
    }
}