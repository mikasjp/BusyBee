using Ignis.Processor;
using JetBrains.Annotations;

namespace Ignis.Abstractions;

[PublicAPI]
public interface IJobFailureHandler
{
    Task Handle(JobContext jobContext, Exception exception, CancellationToken cancellationToken);
}