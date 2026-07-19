namespace GBFR.InfinityFullAssist.Runtime;

internal readonly record struct ExecutableMemorySnapshot(
    nint BaseAddress,
    byte[] Bytes);
