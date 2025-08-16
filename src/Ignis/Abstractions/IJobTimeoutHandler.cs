using Ignis.Processor;
using JetBrains.Annotations;

namespace Ignis.Abstractions;

[PublicAPI]
public interface IJobTimeoutHandler
{
    Task Handle(JobContext jobContext, CancellationToken cancellationToken);
}