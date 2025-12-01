using BusyBee.JobCancellation;
using BusyBee.Queue;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace BusyBee.Tests.JobCancellation;

public sealed class JobCancellationServiceTests
{
    [Test]
    public async Task RequestCancellation_ShouldReturnTrue_WhenJobExists()
    {
        // Arrange
        var serviceProvider = new ServiceCollection()
            .AddBusyBee()
            .Services
            .BuildServiceProvider();
        var queue = serviceProvider.GetRequiredService<IBackgroundQueue>();
        var cancellationService = serviceProvider.GetRequiredService<IJobCancellationService>();
        var enqueuedJobId = await queue.Enqueue((_, _, _) => Task.CompletedTask, CancellationToken.None);

        // Act
        var result = await cancellationService.RequestCancellation(enqueuedJobId);
        
        // Assert
        result.ShouldBe(true);
    }
    
    [Test]
    public async Task RequestCancellation_ShouldReturnFalse_WhenJobDoesNotExist()
    {
        // Arrange
        var serviceProvider = new ServiceCollection()
            .AddBusyBee()
            .Services
            .BuildServiceProvider();
        var cancellationService = serviceProvider.GetRequiredService<IJobCancellationService>();
        var nonExistentJobId = Guid.NewGuid();

        // Act
        var result = await cancellationService.RequestCancellation(nonExistentJobId);
        
        // Assert
        result.ShouldBe(false);
    }
}