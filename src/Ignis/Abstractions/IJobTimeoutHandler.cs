using Ignis.Processor;

namespace Ignis.Abstractions;

public interface IJobTimeoutHandler
{
    Task Handle(JobContext jobContext, CancellationToken cancellationToken);
}