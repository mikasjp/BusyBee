using Microsoft.Extensions.Options;

namespace Ignis.Processor;

internal sealed class SlotsTracker(IOptions<ProcessorOptions> options)
{
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(options.Value.ParallelJobsCount ?? 1);

    public Task ReserveSlot(CancellationToken cancellationToken)
    {
        return _semaphore.WaitAsync(cancellationToken);
    }
    
    public Task ReleaseSlot()
    {
        _semaphore.Release();
        return Task.CompletedTask;
    }
}