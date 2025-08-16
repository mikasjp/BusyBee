using System.Collections.Concurrent;
using Ignis.Processor;
using Ignis.Queue;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace Ignis.Tests.Processing;

public sealed class JobsProcessingTests
{
    [TestCase(1)]
    [TestCase(2)]
    [CancelAfter(1000)]
    public async Task EnqueuedJobs_ShouldBeProcessed(int jobsCount)
    {
        // Arrange
        var bag = new ConcurrentBag<object>();
        var builder = Host.CreateApplicationBuilder();
        builder.Services
            .AddSingleton(bag)
            .AddLogging(x => x.ClearProviders().AddDebug().SetMinimumLevel(LogLevel.Trace))
            .AddIgnis().WithGlobalJobTimeout(TimeSpan.FromSeconds(1));
        using var host = builder.Build();
        await host.StartAsync();
        await using var scope = host.Services.CreateAsyncScope();
        var queue = scope.ServiceProvider.GetRequiredService<IBackgroundQueue>();

        // Act
        for (var i = 0; i < jobsCount; i++)
        {
            await queue.Enqueue(Job, CancellationToken.None);
        }
        
        while (bag.Count < jobsCount)
        {
            await Task.Delay(1);
        }

        // Assert
        bag.Count.ShouldBe(jobsCount);
    }
    
    private static async Task Job(IServiceProvider serviceProvider, JobContext jobContext, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var bag = serviceProvider.GetRequiredService<ConcurrentBag<object>>();
        bag.Add(new object());
        await Task.CompletedTask;
    }
}