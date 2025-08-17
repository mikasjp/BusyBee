using BusyBee.Processor;
using JetBrains.Annotations;

namespace BusyBee.Abstractions;

[PublicAPI]
public interface IJobFailureHandler
{
    Task Handle(JobContext jobContext, Exception exception, CancellationToken cancellationToken);
}