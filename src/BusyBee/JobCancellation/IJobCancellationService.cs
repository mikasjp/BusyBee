using JetBrains.Annotations;

namespace BusyBee.JobCancellation;

[PublicAPI]
public interface IJobCancellationService
{
    /// <summary>
    /// Requests cancellation of a job with the specified ID.
    /// </summary>
    /// <param name="jobId">The ID of the job to cancel.</param>
    /// <returns>Returns true when the cancellation request was accepted, false if the job was not found.</returns>
    Task<bool> RequestCancellation(Guid jobId);
}