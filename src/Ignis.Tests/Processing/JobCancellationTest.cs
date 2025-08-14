using System.Collections.Concurrent;
using Ignis.Abstractions;
using Ignis.Processor;
using Ignis.Queue;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace Ignis.Tests.Processing;

public sealed class JobCancellationTest
{
    [Test]
    [Timeout(1000)]
    public async Task JobCancellation_ShouldTriggerCancellationHandler()
    {
        // Arrange
        var bag = new ConcurrentBag<object>();
        var builder = Host.CreateApplicationBuilder();
        builder.Services
            .AddSingleton(bag)
            .AddLogging(x => x.ClearProviders().AddDebug().SetMinimumLevel(LogLevel.Trace))
            .AddIgnis()
            .WithGlobalJobTimeout(TimeSpan.FromMilliseconds(1))
            .WithJobTimeoutHandler<JobTimeoutHandler>();
        using var host = builder.Build();
        await host.StartAsync();
        await using var scope = host.Services.CreateAsyncScope();
        var queue = scope.ServiceProvider.GetRequiredService<IBackgroundQueue>();
        
        // Act
        await queue.Enqueue(Job, CancellationToken.None);
        while (bag.IsEmpty)
        {
            await Task.Delay(1);
        }
        
        // Assert
        bag.ShouldHaveSingleItem();
    }
    
    private static async Task Job(IServiceProvider serviceProvider, JobContext jobContext, CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromHours(24), ct);
        ct.ThrowIfCancellationRequested();
    }

    private class JobTimeoutHandler(ConcurrentBag<object> bag) : IJobTimeoutHandler
    {
        public Task Handle(JobContext jobContext, CancellationToken cancellationToken)
        {
            bag.Add(new object());
            return Task.CompletedTask;
        }
    }
}