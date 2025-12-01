using System.Collections.Concurrent;

namespace BusyBee.JobCancellation;

internal sealed class JobCancellationTokenSourceRegistry : IJobCancellationService
{
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _cancellationTokenSources = new();

    public void Register(Guid jobId, CancellationTokenSource cancellationTokenSource)
    {
        if (_cancellationTokenSources.TryAdd(jobId, cancellationTokenSource))
        {
            return;
        }
        
        cancellationTokenSource.Dispose();
        throw new InvalidOperationException("Detected job ID collision when registering cancellation token source.");
    }

    public void Unregister(Guid jobId)
    {
        if (_cancellationTokenSources.TryRemove(jobId, out var cts))
        {
            cts.Dispose();
        }
    }

    public async Task<bool> RequestCancellation(Guid jobId)
    {
        if (!_cancellationTokenSources.TryGetValue(jobId, out var cts))
        {
            return false;
        }

        if (!cts.IsCancellationRequested)
        {
            await cts.CancelAsync();
        }
        
        return true;
    }
}