using System.Collections.Concurrent;
using BusyBee.Abstractions;
using BusyBee.Processor;
using BusyBee.Queue;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace BusyBee.Tests.Processing;

public sealed class JobFailureTests
{
    [Test]
    [CancelAfter(1000)]
    public async Task JobFailure_ShouldTriggerFailureHandler(CancellationToken cancellationToken)
    {
        // Arrange
        var bag = new ConcurrentBag<object>();
        var builder = Host.CreateApplicationBuilder();
        builder.Services
            .AddSingleton(bag)
            .AddLogging(x => x.ClearProviders().AddDebug().SetMinimumLevel(LogLevel.Trace))
            .AddBusyBee()
            .WithGlobalJobTimeout(TimeSpan.FromMilliseconds(1))
            .WithJobFailureHandler<JobFailureHandler>();
        using var host = builder.Build();
        await host.StartAsync(cancellationToken);
        await using var scope = host.Services.CreateAsyncScope();
        var queue = scope.ServiceProvider.GetRequiredService<IBackgroundQueue>();
        
        // Act
        await queue.Enqueue(Job, cancellationToken);
        while (bag.IsEmpty)
        {
            await Task.Delay(1, cancellationToken);
        }
        
        // Assert
        bag.ShouldHaveSingleItem();
    }
    
    private static Task Job(IServiceProvider serviceProvider, JobContext jobContext, CancellationToken ct)
    {
        throw new MyCustomTestException();
    }

    private class JobFailureHandler(ConcurrentBag<object> bag) : IJobFailureHandler
    {
        public Task Handle(JobContext jobContext, Exception exception, CancellationToken cancellationToken)
        {
            if (exception is MyCustomTestException)
            {
                bag.Add(new object());
            }
            
            return Task.CompletedTask;
        }
    }

    private class MyCustomTestException : Exception;
}