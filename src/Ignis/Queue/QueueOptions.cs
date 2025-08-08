using System.ComponentModel.DataAnnotations;
using Ignis.Queue.Exceptions;

namespace Ignis.Queue;

public sealed class QueueOptions
{
    /// <summary>
    /// The maximum number of jobs that can be queued.
    /// If not specified, the queue will have no capacity limit.
    /// If specified, there must be a valid <see cref="OverflowStrategy"/> set.
    /// </summary>
    [Range(1, int.MaxValue)] public int? Capacity { get; set; }
    
    /// <summary>
    /// The strategy to use when the queue is full.
    /// Must be set if Capacity is specified.
    /// </summary>
    public OverflowStrategy? OverflowStrategy { get; set; }
}

public enum OverflowStrategy
{
    /// <summary>
    /// Wait for space to become available in the queue.
    /// If the queue is full, the write operation will block until space is available.
    /// </summary>
    Wait = 1,
    /// <summary>
    /// Ignore the write operation if the queue is full.
    /// If the queue is full, the write operation will not block and will simply discard the job.
    /// </summary>
    Ignore,
    /// <summary>
    /// Throw an exception if the queue is full.
    /// If the queue is full, the write operation will throw a <see cref="QueueCapacityExceededException"/>.
    /// </summary>
    ThrowException,
    /// <summary>
    /// Discard the oldest job in the queue if the queue is full.
    /// If the queue is full, the oldest job will be discarded to make space for the new job.
    /// </summary>
    DiscardOldest,
    /// <summary>
    /// Discard the newest job in the queue if the queue is full.
    /// If the queue is full, the newest job will be discarded to make space for the new job.
    /// </summary>
    DiscardNewest
}