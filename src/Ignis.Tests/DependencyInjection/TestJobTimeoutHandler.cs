using Ignis.Abstractions;
using Ignis.Processor;

namespace Ignis.Tests.DependencyInjection;

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