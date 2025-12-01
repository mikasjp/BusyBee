using System.Collections.Concurrent;
using BusyBee.Abstractions;
using BusyBee.JobCancellation;
using BusyBee.Processor;
using BusyBee.Queue;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace BusyBee.Tests.Processing;

public class ManualJobCancellationTests
{
    [Test]
    [CancelAfter(1000)]
    public async Task ManualCancellation_ShouldTriggerManualCancellationHandler(CancellationToken cancellationToken)
    {
        // Arrange
        var bag = new ConcurrentBag<object>();
        var builder = Host.CreateApplicationBuilder();
        builder.Services
            .AddSingleton(bag)
            .AddLogging(x => x.ClearProviders().AddDebug().SetMinimumLevel(LogLevel.Trace))
            .AddBusyBee()
            .WithGlobalJobTimeout(TimeSpan.FromMilliseconds(1))
            .WithManualCancellationHandler<ManualCancellationHandler>();
        using var host = builder.Build();
        await host.StartAsync(cancellationToken);
        await using var scope = host.Services.CreateAsyncScope();
        var queue = scope.ServiceProvider.GetRequiredService<IBackgroundQueue>();
        var cancellationService = scope.ServiceProvider.GetRequiredService<IJobCancellationService>();
        
        // Act
        var enqueuedJobId = await queue.Enqueue(Job, cancellationToken);
        await cancellationService.RequestCancellation(enqueuedJobId);
        while (bag.IsEmpty)
        {
            await Task.Delay(1, cancellationToken);
        }
        
        // Assert
        bag.ShouldHaveSingleItem();
    }
    
    private static async Task Job(IServiceProvider serviceProvider, JobContext jobContext, CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromHours(24), ct);
    }

    private class ManualCancellationHandler(ConcurrentBag<object> bag) : IManualCancellationHandler
    {
        public Task Handle(JobContext jobContext, CancellationToken cancellationToken)
        {
            bag.Add(new object());
            return Task.CompletedTask;
        }
    }
}