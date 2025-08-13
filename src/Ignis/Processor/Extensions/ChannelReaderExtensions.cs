using System.Threading.Channels;

namespace Ignis.Processor.Extensions;

internal static class ChannelReaderExtensions
{
    public static async Task<IReadOnlyList<T>> ReadAvailable<T>(
        this ChannelReader<T> reader,
        CancellationToken cancellationToken)
    {
        List<T> availableItems = [];
        
        var firstItem = await reader.ReadAsync(cancellationToken);
        availableItems.Add(firstItem);
        
        while (reader.TryRead(out var item))
        {
            availableItems.Add(item);
        }
        
        return availableItems;
    }
}