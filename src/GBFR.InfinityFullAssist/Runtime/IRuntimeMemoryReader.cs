namespace GBFR.InfinityFullAssist.Runtime;

internal interface IRuntimeMemoryReader
{
    bool TryReadPointer(nint address, out nint value);

    bool TryReadUInt32(nint address, out uint value);

    bool TryReadByte(nint address, out byte value);

    bool TryReadBytes(nint address, Span<byte> destination);
}
