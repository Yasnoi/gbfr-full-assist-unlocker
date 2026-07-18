using Reloaded.Memory;

namespace GBFR.InfinityFullAssist.Runtime;

internal sealed class ReloadedRuntimeMemoryReader : IRuntimeMemoryReader
{
    public bool TryReadPointer(nint address, out nint value) =>
        TryRead(address, out value);

    public bool TryReadUInt32(nint address, out uint value) =>
        TryRead(address, out value);

    public bool TryReadByte(nint address, out byte value) =>
        TryRead(address, out value);

    public bool TryReadBytes(nint address, Span<byte> destination)
    {
        try
        {
            Memory.Instance.ReadRaw((nuint)address, destination);
            return true;
        }
        catch (Exception)
        {
            destination.Clear();
            return false;
        }
    }

    private static bool TryRead<T>(nint address, out T value)
        where T : unmanaged
    {
        value = default;
        if (!IsLikelyPointer(address))
        {
            return false;
        }

        try
        {
            value = Memory.Instance.Read<T>((nuint)address);
            return true;
        }
        catch (Exception)
        {
            value = default;
            return false;
        }
    }

    internal static bool IsLikelyPointer(nint address)
    {
        var unsigned = (nuint)address;
        return unsigned >= 0x10000 &&
               unsigned <= 0x00007FFF_FFFF_FFFF;
    }
}
