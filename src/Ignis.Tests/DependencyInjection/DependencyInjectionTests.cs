using Ignis.Abstractions;
using Ignis.Queue;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Ignis.Tests.DependencyInjection;

public sealed class DependencyInjectionTests
{
    [Test]
    public void AddIgnis_ShouldRegisterBackgroundQueue()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddIgnis();

        // Assert
        services.ShouldContain(sp =>
            sp.ServiceType == typeof(IBackgroundQueue)
            && sp.Lifetime == ServiceLifetime.Singleton);
    }

    [Test]
    public void AddIgnis_CalledMultipleTimes_ShouldRegisterSingleQueue()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddIgnis();
        services.AddIgnis();

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
        var builder = services.AddIgnis();

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
        var builder = services.AddIgnis();

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