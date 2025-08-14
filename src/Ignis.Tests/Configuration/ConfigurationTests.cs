using Ignis.Processor;
using Ignis.Queue;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Ignis.Tests.Configuration;

public sealed class ConfigurationTests
{
    [Test]
    public void QueueOptions_WithCapacityAndMissingOverflowStrategy_ShouldFail()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddIgnis(configureQueue: queueOptions =>
        {
            queueOptions.Capacity = 10;
            queueOptions.OverflowStrategy = null;
        });
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var action = () => serviceProvider.GetRequiredService<IBackgroundQueue>();

        // Assert
        action.ShouldThrow<ArgumentNullException>();
    }

    [Test]
    public void ProcessorOptions_WithParallelJobsCountOutOfRange_ShouldFail()
    {
        // Arrange
        var services = new ServiceCollection();
        services
            .AddIgnis(configureProcessor: processorOptions =>
            {
                processorOptions.ParallelJobsCount = 0;
                processorOptions.JobTimeout = TimeSpan.FromSeconds(5);
            });
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var action = () => serviceProvider.GetRequiredService<IOptions<ProcessorOptions>>().Value;

        // Assert
        action.ShouldThrow<OptionsValidationException>();
    }

    [Test]
    public void ProcessorOptions_WithJobTimeoutOutOfRange_ShouldFail()
    {
        // Arrange
        var services = new ServiceCollection();
        services
            .AddIgnis(configureProcessor: processorOptions =>
            {
                processorOptions.ParallelJobsCount = 1;
                processorOptions.JobTimeout = TimeSpan.FromSeconds(0);
            });
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var action = () => serviceProvider.GetRequiredService<IOptions<ProcessorOptions>>().Value;

        // Assert
        action.ShouldThrow<OptionsValidationException>();
    }

    [Test]
    public void WithUnboundedQueue_ShouldConfigureQueueOptions()
    {
        // Arrange
        var services = new ServiceCollection()
            .AddIgnis()
            .WithUnboundedQueue()
            .Services
            .BuildServiceProvider();
        
        // Act
        var options = services.GetRequiredService<IOptions<QueueOptions>>().Value;
        
        // Assert
        options.Capacity.ShouldBeNull();
        options.OverflowStrategy.ShouldBeNull();
    }

    public static object[] ValidOverflowStrategies = Enum
        .GetValues<OverflowStrategy>()
        .Select(strategy => new object[] { strategy })
        .ToArray();
    
    [Test]
    [TestCaseSource(nameof(ValidOverflowStrategies))]
    public void WithBoundedQueue_ShouldConfigureQueueOptions(OverflowStrategy strategy)
    {
        // Arrange
        var capacity = Random.Shared.Next(1, 1000);
        var services = new ServiceCollection()
            .AddIgnis()
            .WithBoundedQueue(capacity, strategy)
            .Services
            .BuildServiceProvider();

        // Act
        var options = services.GetRequiredService<IOptions<QueueOptions>>().Value;

        // Assert
        options.Capacity.ShouldBe(capacity);
        options.OverflowStrategy.ShouldBe(strategy);
    }

    [Test]
    public void WithGlobalJobTimeout_ShouldConfigureProcessorOptions()
    {
        // Arrange
        var timeout = TimeSpan.FromSeconds(Random.Shared.Next(1, 1000));
        var services = new ServiceCollection()
            .AddIgnis()
            .WithGlobalJobTimeout(timeout)
            .Services
            .BuildServiceProvider();

        // Act
        var options = services.GetRequiredService<IOptions<ProcessorOptions>>().Value;

        // Assert
        options.JobTimeout.ShouldBe(timeout);
    }
    
    [Test]
    public void WithBoundedQueue_ShouldThrow_WhenCapacityIsZeroOrNegative()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Act
        var action = () => services.AddIgnis().WithBoundedQueue(0);
        
        // Assert
        action.ShouldThrow<ArgumentOutOfRangeException>();
    }
    
    [Test]
    public void WithLevelOfParallelism_ShouldConfigureProcessorOptions()
    {
        // Arrange
        var level = Random.Shared.Next(1, 1000);
        var services = new ServiceCollection()
            .AddIgnis()
            .WithLevelOfParallelism(level)
            .Services
            .BuildServiceProvider();

        // Act
        var options = services.GetRequiredService<IOptions<ProcessorOptions>>().Value;

        // Assert
        options.ParallelJobsCount.ShouldBe(level);
    }
    
    [Test]
    public void WithLevelOfParallelism_ShouldThrow_WhenLevelIsZeroOrNegative()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Act
        var action = () => services.AddIgnis().WithLevelOfParallelism(0);
        
        // Assert
        action.ShouldThrow<ArgumentOutOfRangeException>();
    }
}