namespace GBFR.InfinityFullAssist.Runtime;

internal sealed class RuntimeExecutableMemorySnapshotter
{
    internal const uint MemoryCommit = 0x1000;
    internal const uint PageGuard = 0x100;
    internal const uint PageExecuteRead = 0x20;
    internal const uint PageExecuteReadWrite = 0x40;
    internal const uint PageExecuteWriteCopy = 0x80;

    private readonly IRuntimeMemoryRegionSource _source;

    public RuntimeExecutableMemorySnapshotter(
        IRuntimeMemoryRegionSource source)
    {
        _source = source;
    }

    public bool TryCapture(
        ModuleAddressRange module,
        out IReadOnlyList<ExecutableMemorySnapshot> snapshots,
        out string failure)
    {
        var captured = new List<ExecutableMemorySnapshot>();
        snapshots = captured;
        failure = string.Empty;

        if (module.Size <= 0)
        {
            failure = "the main module range is invalid";
            return false;
        }

        var moduleStart = (nuint)module.BaseAddress;
        var moduleEnd = moduleStart + (nuint)module.Size;
        var current = moduleStart;

        while (current < moduleEnd)
        {
            if (!_source.TryQuery((nint)current, out var region) ||
                region.Size == 0)
            {
                failure =
                    $"VirtualQuery failed at module offset " +
                    $"0x{current - moduleStart:X}";
                return false;
            }

            var regionStart = (nuint)region.BaseAddress;
            var regionEnd = regionStart + region.Size;
            if (regionEnd <= current)
            {
                failure =
                    $"VirtualQuery returned a non-advancing region at " +
                    $"module offset 0x{current - moduleStart:X}";
                return false;
            }

            var captureStart = current > regionStart
                ? current
                : regionStart;
            var captureEnd = regionEnd < moduleEnd
                ? regionEnd
                : moduleEnd;

            if (captureEnd > captureStart &&
                IsReadableExecutable(region.State, region.Protection))
            {
                var captureSize = captureEnd - captureStart;
                if (captureSize > int.MaxValue)
                {
                    failure =
                        "an executable memory region is too large to snapshot";
                    return false;
                }

                var bytes = new byte[(int)captureSize];
                if (!_source.TryRead((nint)captureStart, bytes))
                {
                    failure =
                        $"ReadProcessMemory failed at module offset " +
                        $"0x{captureStart - moduleStart:X}";
                    return false;
                }

                captured.Add(new ExecutableMemorySnapshot(
                    (nint)captureStart,
                    bytes));
            }

            current = regionEnd;
        }

        if (captured.Count == 0)
        {
            failure = "the main module has no readable executable regions";
            return false;
        }

        return true;
    }

    internal static bool IsReadableExecutable(
        uint state,
        uint protection)
    {
        if (state != MemoryCommit || (protection & PageGuard) != 0)
        {
            return false;
        }

        return (protection & 0xFF) is
            PageExecuteRead or
            PageExecuteReadWrite or
            PageExecuteWriteCopy;
    }
}
