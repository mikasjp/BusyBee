using Ignis.Queue.Exceptions;

namespace Ignis.Queue;

public interface IBackgroundQueue
{
    /// <summary>
    /// Enqueues a job to be processed in the background.
    /// </summary>
    /// <param name="job">The job to be executed.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the job if needed.</param>
    /// <returns>A task that represents the asynchronous operation, containing the job ID.</returns>
    /// <exception cref="QueueCapacityExceededException">Thrown when the queue has reached its maximum capacity and <see cref="OverflowStrategy"/> is set to <see cref="OverflowStrategy.ThrowException"/>.</exception>
    /// <remarks>
    /// The job is a function that takes an <see cref="IServiceProvider"/> and returns a <see cref="Task"/>.
    /// This allows the job to resolve dependencies from the service provider.
    /// </remarks>
    Task<Guid> Enqueue(Func<IServiceProvider, CancellationToken, Task> job, CancellationToken cancellationToken);
}