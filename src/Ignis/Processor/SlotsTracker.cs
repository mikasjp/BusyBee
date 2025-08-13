using System.Threading.Channels;
using Ignis.Processor.Extensions;
using Microsoft.Extensions.Options;

namespace Ignis.Processor;

internal sealed class SlotsTracker(IOptions<ProcessorOptions> options)
{
    private readonly Channel<object> _slotTracker = Channel.CreateBounded<object>(
        new BoundedChannelOptions(options.Value.ParallelJobsCount ?? 1)
        {
            FullMode = BoundedChannelFullMode.Wait
        });

    public async Task InitializeSlots(CancellationToken stoppingToken)
    {
        for (var i = 0; i < options.Value.ParallelJobsCount; i++)
        {
            await _slotTracker.Writer.WriteAsync(new object(), stoppingToken);
        }
    }

    public async Task<int> ReleaseAvailableSlots(CancellationToken cancellationToken)
    {
        var availableSlots = await _slotTracker.Reader.ReadAvailable(cancellationToken);
        return availableSlots.Count;
    }
    
    public async Task ReleaseSlots(int count, CancellationToken cancellationToken)
    {
        for (var i = 0; i < count; i++)
        {
            await _slotTracker.Writer.WriteAsync(new object(), cancellationToken);
        }
    }
}