using GBX.NET;
using System.Runtime.InteropServices;

namespace EnvimixWebAPI.Security;

public static class GbxIpRemover
{
    private const int IpOffsetAfterPattern = 20;
    private const int IpLength = 4;

    /// <summary>
    /// Returns a compressed copy of <paramref name="data"/> with all found IPs zeroed, along with the number of IPs removed.
    /// </summary>
    public static async Task<(byte[] Data, int RemovedCount)> RemoveIpsAsync(byte[] data, CancellationToken cancellationToken)
    {
        using var input = new MemoryStream(data, writable: false);
        return await RemoveIpsAsync(input, cancellationToken);
    }

    /// <summary>
    /// Returns a compressed copy of the Gbx read from the current position of <paramref name="input"/> with all found IPs zeroed,
    /// along with the number of IPs removed.
    /// </summary>
    public static async Task<(byte[] Data, int RemovedCount)> RemoveIpsAsync(Stream input, CancellationToken cancellationToken)
    {
        using var decompressed = new MemoryStream();

        await Gbx.DecompressAsync(input, decompressed, cancellationToken);

        var removedCount = ClearIps(decompressed.GetBuffer().AsSpan(0, checked((int)decompressed.Length)));

        decompressed.Position = 0;

        using var output = new MemoryStream((int)decompressed.Length);
        await Gbx.CompressAsync(decompressed, output, cancellationToken);

        return (output.ToArray(), removedCount);
    }

    /// <summary>
    /// Same as <see cref="RemoveIpsAsync"/>, but returns null unless exactly one IP was removed
    /// (expected for a single ghost, or a replay of a single run).
    /// </summary>
    public static async Task<byte[]?> RemoveSingleIpAsync(byte[] data, CancellationToken cancellationToken)
    {
        using var input = new MemoryStream(data, writable: false);
        return await RemoveSingleIpAsync(input, cancellationToken);
    }

    /// <inheritdoc cref="RemoveSingleIpAsync(byte[], CancellationToken)"/>
    public static async Task<byte[]?> RemoveSingleIpAsync(Stream input, CancellationToken cancellationToken)
    {
        var (cleaned, removedCount) = await RemoveIpsAsync(input, cancellationToken);
        return removedCount == 1 ? cleaned : null;
    }

    public static int ClearIps(Span<byte> data)
    {
        ReadOnlySpan<int> patternInts = [0x03092023, 0x534B4950, 47, 3, 0];
        var pattern = MemoryMarshal.AsBytes(patternInts);

        var count = 0;
        var offset = 0;

        while (offset < data.Length)
        {
            var index = data[offset..].IndexOf(pattern);

            if (index < 0)
            {
                break;
            }

            index += offset;

            var ipIndex = index + pattern.Length + IpOffsetAfterPattern;

            if (ipIndex + IpLength <= data.Length)
            {
                data.Slice(ipIndex, IpLength).Clear();
                count++;
            }

            offset = index + pattern.Length;
        }

        return count;
    }
}
