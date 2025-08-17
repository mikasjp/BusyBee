using BusyBee.Abstractions;
using BusyBee.Queue;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace BusyBee.Tests.DependencyInjection;

public sealed class DependencyInjectionTests
{
    [Test]
    public void AddBusyBee_ShouldRegisterBackgroundQueue()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBusyBee();

        // Assert
        services.ShouldContain(sp =>
            sp.ServiceType == typeof(IBackgroundQueue)
            && sp.Lifetime == ServiceLifetime.Singleton);
    }

    [Test]
    public void AddBusyBee_CalledMultipleTimes_ShouldRegisterSingleQueue()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBusyBee();
        services.AddBusyBee();

        // Assert
        services.Where(service => service.ServiceType == typeof(IBackgroundQueue))
            .ShouldHaveSingleItem();
    }

    [TestCase(ServiceLifetime.Transient)]
    [TestCase(ServiceLifetime.Scoped)]
    public void RegisteringMultipleJobFailureHandlers_ShouldOverwritePrevious(ServiceLifetime handlerLifetime)
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddBusyBee();

        // Act
        builder.WithJobFailureHandler<TestJobFailureHandler>();
        builder.WithJobFailureHandler<SecondTestJobFailureHandler>(handlerLifetime);

        // Assert
        services.ShouldContain(sp =>
            sp.ServiceType == typeof(IJobFailureHandler)
            && sp.ImplementationType == typeof(SecondTestJobFailureHandler)
            && sp.Lifetime == handlerLifetime);
        services.ShouldNotContain(sp =>
            sp.ServiceType == typeof(IJobFailureHandler)
            && sp.ImplementationType == typeof(TestJobFailureHandler));
    }

    [TestCase(ServiceLifetime.Transient)]
    [TestCase(ServiceLifetime.Scoped)]
    public void RegisteringMultipleJobTimeoutHandlers_ShouldOverwritePrevious(ServiceLifetime handlerLifetime)
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddBusyBee();

        // Act
        builder.WithJobTimeoutHandler<TestJobTimeoutHandler>();
        builder.WithJobTimeoutHandler<SecondTestJobTimeoutHandler>(handlerLifetime);

        // Assert
        services.ShouldContain(sp =>
            sp.ServiceType == typeof(IJobTimeoutHandler)
            && sp.ImplementationType == typeof(SecondTestJobTimeoutHandler)
            && sp.Lifetime == handlerLifetime);
        services.ShouldNotContain(sp =>
            sp.ServiceType == typeof(IJobTimeoutHandler)
            && sp.ImplementationType == typeof(TestJobTimeoutHandler));
    }
}