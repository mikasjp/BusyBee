using BusyBee.Abstractions;
using BusyBee.Processor;

namespace BusyBee.Tests.DependencyInjection;

internal sealed class TestJobTimeoutHandler : IJobTimeoutHandler
{
    public Task Handle(JobContext jobContext, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}

internal sealed class SecondTestJobTimeoutHandler : IJobTimeoutHandler
{
    public Task Handle(JobContext jobContext, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}