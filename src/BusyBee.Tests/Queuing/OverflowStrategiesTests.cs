using BusyBee.Queue;
using BusyBee.Queue.Exceptions;
using Shouldly;
using Microsoft.Extensions.DependencyInjection;

namespace BusyBee.Tests.Queuing;

public sealed class OverflowStrategiesTests
{
    [Test]
    public async Task ThrowExceptionStrategy_ShouldThrowException_WhenQueueIsFull()
    {
        // Arrange
        var queue = new ServiceCollection()
            .AddBusyBee()
            .WithBoundedQueue(1, OverflowStrategy.ThrowException)
            .Services
            .BuildServiceProvider()
            .GetRequiredService<IBackgroundQueue>();

        // Act
        await queue.Enqueue((_,_,_) => Task.CompletedTask, CancellationToken.None);
        var action =  async () => await queue.Enqueue((_,_,_) => Task.CompletedTask, CancellationToken.None);

        // Assert
        action.ShouldThrow<QueueCapacityExceededException>();
    }
    
    [Test]
    public async Task WaitStrategy_ShouldWait_WhenQueueIsFull()
    {
        // Arrange
        var queue = new ServiceCollection()
            .AddBusyBee()
            .WithBoundedQueue(1, OverflowStrategy.Wait)
            .Services
            .BuildServiceProvider()
            .GetRequiredService<IBackgroundQueue>();

        // Act
        await queue.Enqueue((_,_,_) => Task.CompletedTask, CancellationToken.None);
        var enqueueTask = queue.Enqueue((_,_,_) => Task.CompletedTask, CancellationToken.None);
        var timeoutTask = Task.Delay(TimeSpan.FromMilliseconds(100));
        var completedTask = await Task.WhenAny(enqueueTask, timeoutTask);
        
        // Assert
        completedTask.ShouldBe(timeoutTask);
    }
}