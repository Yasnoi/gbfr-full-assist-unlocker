namespace GBFR.InfinityFullAssist.Runtime;

internal interface IRuntimeMemoryRegionSource
{
    bool TryQuery(nint address, out VirtualMemoryRegion region);

    bool TryRead(nint address, byte[] destination);
}
