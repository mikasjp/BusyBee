using BusyBee.Abstractions;
using BusyBee.Processor;
using BusyBee.Queue;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BusyBee;

[PublicAPI]
public sealed class BusyBeeBuilder
{
    public IServiceCollection Services { get; }

    internal BusyBeeBuilder(IServiceCollection services)
    {
        Services = services;
    }

    public BusyBeeBuilder WithUnboundedQueue()
    {
        Services.Configure<QueueOptions>(options =>
        {
            options.Capacity = null;
            options.OverflowStrategy = null;
        });

        return this;
    }

    public BusyBeeBuilder WithBoundedQueue(int capacity,
        OverflowStrategy overflowStrategy = OverflowStrategy.ThrowException)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than zero.");
        }

        Services.Configure<QueueOptions>(options =>
        {
            options.Capacity = capacity;
            options.OverflowStrategy = overflowStrategy;
        });

        return this;
    }

    public BusyBeeBuilder WithGlobalJobTimeout(TimeSpan timeout)
    {
        Services.Configure<ProcessorOptions>(options => { options.JobTimeout = timeout; });

        return this;
    }

    public BusyBeeBuilder WithJobTimeoutHandler<TTimeoutHandler>(
        ServiceLifetime handlerLifetime = ServiceLifetime.Transient) where TTimeoutHandler : class, IJobTimeoutHandler
    {
        Services.Replace(
            ServiceDescriptor.Describe(typeof(IJobTimeoutHandler), typeof(TTimeoutHandler), handlerLifetime));

        return this;
    }

    public BusyBeeBuilder WithJobFailureHandler<TFailureHandler>(
        ServiceLifetime handlerLifetime = ServiceLifetime.Transient) where TFailureHandler : IJobFailureHandler
    {
        Services.Replace(
            ServiceDescriptor.Describe(typeof(IJobFailureHandler), typeof(TFailureHandler), handlerLifetime));

        return this;
    }

    public BusyBeeBuilder WithLevelOfParallelism(int levelOfParallelism)
    {
        if (levelOfParallelism <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(levelOfParallelism),
                "Level of parallelism must be greater than zero.");
        }

        Services.Configure<ProcessorOptions>(options => { options.ParallelJobsCount = levelOfParallelism; });

        return this;
    }
}