using BusyBee.Abstractions;
using BusyBee.Processor;

namespace BusyBee.Tests.DependencyInjection;

internal sealed class TestJobFailureHandler : IJobFailureHandler
{
    public Task Handle(JobContext jobContext, Exception exception, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}

internal sealed class SecondTestJobFailureHandler : IJobFailureHandler
{
    public Task Handle(JobContext jobContext, Exception exception, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}