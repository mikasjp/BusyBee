using BusyBee.Processor;
using JetBrains.Annotations;

namespace BusyBee.Abstractions;

[PublicAPI]
public interface IJobTimeoutHandler
{
    Task Handle(JobContext jobContext, CancellationToken cancellationToken);
}