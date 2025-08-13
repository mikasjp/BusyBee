using Ignis.Abstractions;
using Ignis.Processor;

namespace DemoApp;

internal sealed class JobFailureHandler(ILogger<JobFailureHandler> logger) : IJobFailureHandler
{
    public Task Handle(JobContext jobContext, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(
            exception, 
            "Unhandled exception captured by JobFailureHandler for job {JobId}",
            jobContext.JobId);
        return Task.CompletedTask;
    }
}