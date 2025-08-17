using BusyBee.Abstractions;
using BusyBee.Processor;

namespace DemoApp;

internal sealed class JobTimeoutHandler(ILogger<JobTimeoutHandler> logger) : IJobTimeoutHandler
{
    public Task Handle(JobContext jobContext, CancellationToken cancellationToken)
    {
        logger.LogWarning(
            "Job timeout captured for job {JobId} by JobTimeoutHandler",
            jobContext.JobId);
        return Task.CompletedTask;
    }
}