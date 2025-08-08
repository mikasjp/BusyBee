using Ignis.Processor;
using Ignis.Queue;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Ignis;

public sealed class IgnisBuilder
{
    public IServiceCollection Services { get; }

    internal IgnisBuilder(IServiceCollection services)
    {
        Services = services;
    }

    public IgnisBuilder WithUnboundedQueue()
    {
        Services.Configure<QueueOptions>(options =>
        {
            options.Capacity = null;
            options.OverflowStrategy = null;
        });

        return this;
    }
    
    public IgnisBuilder WithBoundedQueue(int capacity, OverflowStrategy overflowStrategy = OverflowStrategy.ThrowException)
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

    public IgnisBuilder WithJobTimeout(TimeSpan timeout)
    {
        Services.Configure<ProcessorOptions>(options =>
        {
            options.JobTimeout = timeout;
        });

        return this;
    }

    public IgnisBuilder WithJobTimeoutLogging(LogLevel logLevel = LogLevel.Debug)
    {
        Services.Configure<ProcessorOptions>(options =>
        {
            options.JobTimeoutLogLevel = logLevel;
        });

        return this;
    }
    
    public IgnisBuilder WithJobBatchSize(int batchSize)
    {
        if (batchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be greater than zero.");
        }

        Services.Configure<ProcessorOptions>(options =>
        {
            options.JobsBatchSize = batchSize;
        });

        return this;
    }
}