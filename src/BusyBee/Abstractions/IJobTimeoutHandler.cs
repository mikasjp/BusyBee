using BusyBee.Processor;
using JetBrains.Annotations;

namespace BusyBee.Abstractions;

[PublicAPI]
public interface IJobTimeoutHandler
{
    /// <summary>
    /// Handles a job timeout.
    /// </summary>
    /// <param name="jobContext">Context of the timed-out job.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation if needed.</param>
    Task Handle(JobContext jobContext, CancellationToken cancellationToken);
}