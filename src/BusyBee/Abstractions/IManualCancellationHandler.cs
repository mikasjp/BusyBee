using BusyBee.Processor;
using JetBrains.Annotations;

namespace BusyBee.Abstractions;

[PublicAPI]
public interface IManualCancellationHandler
{
    /// <summary>
    /// Handles a manual job cancellation.
    /// </summary>
    /// <param name="jobContext">Context of the manually cancelled job.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation if needed.</param>
    Task Handle(JobContext jobContext, CancellationToken cancellationToken);
}