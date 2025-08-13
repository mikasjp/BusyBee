using Ignis.Processor;

namespace Ignis.Abstractions;

public interface IJobFailureHandler
{
    Task Handle(JobContext jobContext, Exception exception, CancellationToken cancellationToken);
}