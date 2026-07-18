namespace GBFR.InfinityFullAssist.Runtime;

internal sealed class BytePattern
{
    private readonly byte?[] _bytes;

    private BytePattern(byte?[] bytes)
    {
        _bytes = bytes;
    }

    public int Length => _bytes.Length;

    public static BytePattern Parse(string signature)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(signature);

        var bytes = signature
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(token => token is "?" or "??"
                ? (byte?)null
                : Convert.ToByte(token, 16))
            .ToArray();

        return new BytePattern(bytes);
    }

    public bool Matches(ReadOnlySpan<byte> actual)
    {
        if (actual.Length < _bytes.Length)
        {
            return false;
        }

        for (var index = 0; index < _bytes.Length; index++)
        {
            if (_bytes[index] is { } expected && actual[index] != expected)
            {
                return false;
            }
        }

        return true;
    }
}
