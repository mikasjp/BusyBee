using BusyBee.Processor;
using JetBrains.Annotations;

namespace BusyBee.Abstractions;

[PublicAPI]
public interface IJobFailureHandler
{
    /// <summary>
    /// Handles a job failure.
    /// </summary>
    /// <param name="jobContext">Context of the failed job.</param>
    /// <param name="exception">The exception that caused the job to fail.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation if needed.</param>
    Task Handle(JobContext jobContext, Exception exception, CancellationToken cancellationToken);
}