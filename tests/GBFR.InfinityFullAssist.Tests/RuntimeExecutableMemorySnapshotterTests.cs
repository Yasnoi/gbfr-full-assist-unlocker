using System.Diagnostics;
using GBFR.InfinityFullAssist.Runtime;

namespace GBFR.InfinityFullAssist.Tests;

public sealed class RuntimeExecutableMemorySnapshotterTests
{
    private static readonly nint ModuleBase = 0x10000000;
    private readonly ModuleAddressRange _module = new(ModuleBase, 0x3000);

    [Fact]
    public void CapturesCurrentProcessExecutableMemoryThroughSafeReads()
    {
        using var process = Process.GetCurrentProcess();
        var mainModule = Assert.IsType<ProcessModule>(process.MainModule);
        var module = new ModuleAddressRange(
            mainModule.BaseAddress,
            mainModule.ModuleMemorySize);
        var snapshotter = new RuntimeExecutableMemorySnapshotter(
            new WindowsRuntimeMemoryRegionSource());

        Assert.True(snapshotter.TryCapture(
            module,
            out var snapshots,
            out var failure),
            failure);
        Assert.NotEmpty(snapshots);
        Assert.All(
            snapshots,
            snapshot => Assert.True(module.Contains(
                snapshot.BaseAddress,
                snapshot.Bytes.Length)));
    }

    [Fact]
    public void CapturesOnlyCommittedReadableExecutableRegions()
    {
        var source = new FakeMemoryRegionSource(
            [
                Region(
                    ModuleBase,
                    0x1000,
                    RuntimeExecutableMemorySnapshotter.MemoryCommit,
                    RuntimeExecutableMemorySnapshotter.PageExecuteRead),
                Region(
                    ModuleBase + 0x1000,
                    0x1000,
                    RuntimeExecutableMemorySnapshotter.MemoryCommit,
                    protection: 0x04),
                Region(
                    ModuleBase + 0x2000,
                    0x1000,
                    RuntimeExecutableMemorySnapshotter.MemoryCommit,
                    RuntimeExecutableMemorySnapshotter.PageExecuteRead |
                    RuntimeExecutableMemorySnapshotter.PageGuard)
            ]);
        var snapshotter = new RuntimeExecutableMemorySnapshotter(source);

        Assert.True(snapshotter.TryCapture(
            _module,
            out var snapshots,
            out var failure));
        Assert.Empty(failure);
        var snapshot = Assert.Single(snapshots);
        Assert.Equal(ModuleBase, snapshot.BaseAddress);
        Assert.Equal(0x1000, snapshot.Bytes.Length);
        Assert.All(snapshot.Bytes, value => Assert.Equal(0xA5, value));
    }

    [Fact]
    public void ReadFailureFailsClosed()
    {
        var source = new FakeMemoryRegionSource(
            [
                Region(
                    ModuleBase,
                    0x3000,
                    RuntimeExecutableMemorySnapshotter.MemoryCommit,
                    RuntimeExecutableMemorySnapshotter.PageExecuteRead)
            ])
        {
            FailReads = true
        };
        var snapshotter = new RuntimeExecutableMemorySnapshotter(source);

        Assert.False(snapshotter.TryCapture(
            _module,
            out _,
            out var failure));
        Assert.Contains("ReadProcessMemory failed", failure, StringComparison.Ordinal);
    }

    [Fact]
    public void QueryFailureFailsClosed()
    {
        var snapshotter = new RuntimeExecutableMemorySnapshotter(
            new FakeMemoryRegionSource([]));

        Assert.False(snapshotter.TryCapture(
            _module,
            out _,
            out var failure));
        Assert.Contains("VirtualQuery failed", failure, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(
        RuntimeExecutableMemorySnapshotter.MemoryCommit,
        RuntimeExecutableMemorySnapshotter.PageExecuteRead,
        true)]
    [InlineData(
        RuntimeExecutableMemorySnapshotter.MemoryCommit,
        RuntimeExecutableMemorySnapshotter.PageExecuteReadWrite,
        true)]
    [InlineData(
        RuntimeExecutableMemorySnapshotter.MemoryCommit,
        RuntimeExecutableMemorySnapshotter.PageExecuteWriteCopy,
        true)]
    [InlineData(
        RuntimeExecutableMemorySnapshotter.MemoryCommit,
        0x04,
        false)]
    [InlineData(
        0x2000,
        RuntimeExecutableMemorySnapshotter.PageExecuteRead,
        false)]
    public void ClassifiesReadableExecutableProtection(
        uint state,
        uint protection,
        bool expected)
    {
        Assert.Equal(
            expected,
            RuntimeExecutableMemorySnapshotter.IsReadableExecutable(
                state,
                protection));
    }

    private static VirtualMemoryRegion Region(
        nint baseAddress,
        nuint size,
        uint state,
        uint protection) =>
        new(baseAddress, size, state, protection);

    private sealed class FakeMemoryRegionSource :
        IRuntimeMemoryRegionSource
    {
        private readonly IReadOnlyList<VirtualMemoryRegion> _regions;

        public FakeMemoryRegionSource(
            IReadOnlyList<VirtualMemoryRegion> regions)
        {
            _regions = regions;
        }

        public bool FailReads { get; init; }

        public bool TryQuery(
            nint address,
            out VirtualMemoryRegion region)
        {
            var value = (nuint)address;
            foreach (var candidate in _regions)
            {
                var start = (nuint)candidate.BaseAddress;
                if (value >= start && value < start + candidate.Size)
                {
                    region = candidate;
                    return true;
                }
            }

            region = default;
            return false;
        }

        public bool TryRead(nint address, byte[] destination)
        {
            if (FailReads)
            {
                return false;
            }

            Array.Fill(destination, (byte)0xA5);
            return true;
        }
    }
}
