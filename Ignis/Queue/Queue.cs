using System.Diagnostics;
using Ignis.Queue.Exceptions;
using System.Threading.Channels;
using Microsoft.Extensions.Options;

namespace Ignis.Queue;

internal class Queue(IOptions<QueueOptions> options) : IBackgroundQueue
{
    private readonly QueueOptions _options = options.Value ?? throw new ArgumentNullException(nameof(options), "Queue options cannot be null.");
    private readonly Channel<JobWrapper> _channel = CreateChannel(options.Value);

    public async Task<Guid> Enqueue(Func<IServiceProvider, CancellationToken, Task> job, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);

        var jobId = Guid.NewGuid();

        var jobWrapper = new JobWrapper(
            jobId,
            Activity.Current?.Context,
            job);

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

    public async Task<IEnumerable<JobWrapper>> DequeueBatch(int batchSize, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize, nameof(batchSize));

        var jobs = new List<JobWrapper>(batchSize);
        var firstJob = await _channel.Reader.ReadAsync(cancellationToken);
        jobs.Add(firstJob);
        for (var i = 1; i < batchSize; i++)
        {
            if (_channel.Reader.TryRead(out var job))
            {
                jobs.Add(job);
            }
            else
            {
                break;
            }
        }

        return jobs;
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