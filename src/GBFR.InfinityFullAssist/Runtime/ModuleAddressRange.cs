namespace GBFR.InfinityFullAssist.Runtime;

internal readonly record struct ModuleAddressRange(
    nint BaseAddress,
    int Size)
{
    public bool Contains(nint address, int length = 1)
    {
        if (Size <= 0 || length <= 0)
        {
            return false;
        }

        var start = (nuint)BaseAddress;
        var value = (nuint)address;
        var size = (nuint)Size;
        var requestedLength = (nuint)length;

        return value >= start &&
               value - start < size &&
               requestedLength <= size - (value - start);
    }
}
