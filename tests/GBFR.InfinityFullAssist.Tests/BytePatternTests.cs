using GBFR.InfinityFullAssist.Runtime;

namespace GBFR.InfinityFullAssist.Tests;

public sealed class BytePatternTests
{
    [Fact]
    public void WildcardsMatchChangingDisplacements()
    {
        var pattern = BytePattern.Parse("48 8B 05 ?? ?? ?? ?? 0F B6");

        Assert.True(pattern.Matches(
            [0x48, 0x8B, 0x05, 0x11, 0x22, 0x33, 0x44, 0x0F, 0xB6]));
    }

    [Fact]
    public void FixedOpcodeMismatchFails()
    {
        var pattern = BytePattern.Parse("48 8B 05 ??");

        Assert.False(pattern.Matches([0x49, 0x8B, 0x05, 0x00]));
    }
}
