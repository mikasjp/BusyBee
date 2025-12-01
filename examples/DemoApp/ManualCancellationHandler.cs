using BusyBee.Abstractions;
using BusyBee.Processor;

namespace DemoApp;

internal sealed class ManualCancellationHandler(ILogger<IManualCancellationHandler> logger) : IManualCancellationHandler
{
    public Task Handle(JobContext jobContext, CancellationToken cancellationToken)
    {
        logger.LogWarning(
            "Manual cancellation captured for job {JobId} by ManualCancellationHandler",
            jobContext.JobId);
        return Task.CompletedTask;
    }
}