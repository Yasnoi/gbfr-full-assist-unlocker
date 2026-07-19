using GBFR.InfinityFullAssist.Runtime;

namespace GBFR.InfinityFullAssist.Tests;

public sealed class ModuleAddressRangeTests
{
    private readonly ModuleAddressRange _module = new(0x10000000, 0x1000);

    [Fact]
    public void ContainsAddressesAndWindowsInsideModule()
    {
        Assert.True(_module.Contains(0x10000000));
        Assert.True(_module.Contains(0x10000800, 0x200));
        Assert.True(_module.Contains(0x10000FFF));
    }

    [Fact]
    public void RejectsAddressesAndWindowsOutsideModule()
    {
        Assert.False(_module.Contains(0x0FFFFFFF));
        Assert.False(_module.Contains(0x10001000));
        Assert.False(_module.Contains(0x10000F80, 0x100));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RejectsInvalidWindowLength(int length)
    {
        Assert.False(_module.Contains(0x10000100, length));
    }
}
