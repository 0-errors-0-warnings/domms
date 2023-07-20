using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace CalcEngineService.Extensions;

public static class ChannelReaderExtension
{
    public static async IAsyncEnumerable<IEnumerable<T>> ReadAllAsync<T>(
        this ChannelReader<T> reader,
        [EnumeratorCancellation] CancellationToken stoppingToken = default
    )
    {
        while (await reader.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
        {
            yield return reader.Flush().ToList();
        }
    }

    private static IEnumerable<T> Flush<T>(this ChannelReader<T> reader)
    {
        while (reader.TryRead(out T item))
        {
            yield return item;
        }
    }
}
