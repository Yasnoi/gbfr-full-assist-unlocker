namespace GBFR.InfinityFullAssist.Runtime;

internal readonly record struct SignatureSearchRegion(
    nint BaseAddress,
    int Size,
    Func<int, int?> FindFromOffset);
