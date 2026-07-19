namespace GBFR.InfinityFullAssist.Runtime;

internal readonly record struct VirtualMemoryRegion(
    nint BaseAddress,
    nuint Size,
    uint State,
    uint Protection);
