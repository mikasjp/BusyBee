using System.Diagnostics;
using System.Threading.Channels;
using BusyBee.Processor;
using BusyBee.Queue.Exceptions;
using Microsoft.Extensions.Options;

namespace BusyBee.Queue;

internal class Queue(IOptions<QueueOptions> options) : IBackgroundQueue
{
    private readonly QueueOptions _options = options.Value ?? throw new ArgumentNullException(nameof(options), "Queue options cannot be null.");
    private readonly Channel<JobWrapper> _channel = CreateChannel(options.Value);

    public async Task<Guid> Enqueue(
        Func<IServiceProvider, JobContext, CancellationToken, Task> job,
        CancellationToken cancellationToken,
        TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(job);

        var jobId = Guid.NewGuid();

        var jobWrapper = new JobWrapper(
            JobId: jobId,
            Timeout: timeout,
            QueuedAt: DateTimeOffset.UtcNow,
            ActivityContext: Activity.Current?.Context,
            Job: job);

        if (_options.OverflowStrategy == OverflowStrategy.ThrowException)
        {
            var writeResult = _channel.Writer.TryWrite(jobWrapper);
            if (!writeResult)
            {
                throw new QueueCapacityExceededException();
            }
        }
        else
        {
            await _channel.Writer.WriteAsync(jobWrapper, cancellationToken);
        }

        return jobId;
    }
    
    public async Task<JobWrapper> Dequeue(CancellationToken cancellationToken)
    {
        return await _channel.Reader.ReadAsync(cancellationToken);
    }

    private static Channel<JobWrapper> CreateChannel(QueueOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return options.Capacity is not null
            ? Channel.CreateBounded<JobWrapper>(new BoundedChannelOptions(options.Capacity.Value)
            {
                FullMode = options.OverflowStrategy switch
                {
                    OverflowStrategy.Wait => BoundedChannelFullMode.Wait,
                    OverflowStrategy.Ignore => BoundedChannelFullMode.DropWrite,
                    OverflowStrategy.ThrowException => BoundedChannelFullMode.Wait,
                    OverflowStrategy.DiscardOldest => BoundedChannelFullMode.DropOldest,
                    OverflowStrategy.DiscardNewest => BoundedChannelFullMode.DropNewest,
                    null => throw new ArgumentNullException(
                        nameof(options.OverflowStrategy),
                        "Overflow strategy cannot be null when queue capacity is set."),
                    _ => throw new ArgumentOutOfRangeException(
                        nameof(options),
                        $"'{options.OverflowStrategy:D}' is invalid overflow strategy.")
                }
            })
            : Channel.CreateUnbounded<JobWrapper>();
    }
}