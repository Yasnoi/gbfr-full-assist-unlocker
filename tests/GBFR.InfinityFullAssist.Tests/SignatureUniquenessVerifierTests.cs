using GBFR.InfinityFullAssist.Runtime;

namespace GBFR.InfinityFullAssist.Tests;

public sealed class SignatureUniquenessVerifierTests
{
    private static readonly nint ModuleBase = 0x10000000;

    [Fact]
    public void NoMatchIsMissing()
    {
        var result = SignatureUniquenessVerifier.FindUnique(
            ModuleBase,
            moduleSize: 0x1000,
            [Region(ModuleBase, 0x1000, _ => null)]);

        Assert.Equal(SignatureMatchStatus.Missing, result.Status);
    }

    [Fact]
    public void OneMatchReturnsRuntimeAddress()
    {
        var result = SignatureUniquenessVerifier.FindUnique(
            ModuleBase,
            moduleSize: 0x1000,
            [Region(
                ModuleBase,
                0x1000,
                offset => offset == 0 ? 0x120 : null)]);

        Assert.Equal(SignatureMatchStatus.Unique, result.Status);
        Assert.Equal(ModuleBase + 0x120, result.Address);
        Assert.Equal(0x120, result.FirstOffset);
    }

    [Fact]
    public void SecondMatchIsAmbiguous()
    {
        var result = SignatureUniquenessVerifier.FindUnique(
            ModuleBase,
            moduleSize: 0x1000,
            [Region(
                ModuleBase,
                0x1000,
                offset => offset switch
                {
                    0 => 0x120,
                    0x121 => 0x700,
                    _ => null
                })]);

        Assert.Equal(SignatureMatchStatus.Ambiguous, result.Status);
        Assert.Equal(0x120, result.FirstOffset);
        Assert.Equal(0x700, result.SecondOffset);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0x1000)]
    public void MatchOutsideModuleIsInvalid(int invalidOffset)
    {
        var result = SignatureUniquenessVerifier.FindUnique(
            ModuleBase,
            moduleSize: 0x1000,
            [Region(
                ModuleBase,
                0x1000,
                offset => offset == 0 ? invalidOffset : null)]);

        Assert.Equal(SignatureMatchStatus.InvalidOffset, result.Status);
    }

    [Fact]
    public void SecondMatchBeforeRequestedOffsetIsInvalid()
    {
        var result = SignatureUniquenessVerifier.FindUnique(
            ModuleBase,
            moduleSize: 0x1000,
            [Region(
                ModuleBase,
                0x1000,
                offset => offset switch
                {
                    0 => 0x120,
                    0x121 => 0x100,
                    _ => null
                })]);

        Assert.Equal(SignatureMatchStatus.InvalidOffset, result.Status);
    }

    [Fact]
    public void MatchesInDifferentExecutableRegionsAreAmbiguous()
    {
        var result = SignatureUniquenessVerifier.FindUnique(
            ModuleBase,
            moduleSize: 0x1000,
            [
                Region(
                    ModuleBase,
                    0x400,
                    offset => offset == 0 ? 0x120 : null),
                Region(
                    ModuleBase + 0x800,
                    0x400,
                    offset => offset == 0 ? 0x20 : null)
            ]);

        Assert.Equal(SignatureMatchStatus.Ambiguous, result.Status);
        Assert.Equal(0x120, result.FirstOffset);
        Assert.Equal(0x820, result.SecondOffset);
    }

    [Fact]
    public void RegionOutsideModuleIsInvalid()
    {
        var result = SignatureUniquenessVerifier.FindUnique(
            ModuleBase,
            moduleSize: 0x1000,
            [Region(ModuleBase + 0xF00, 0x200, _ => null)]);

        Assert.Equal(SignatureMatchStatus.InvalidOffset, result.Status);
    }

    private static SignatureSearchRegion Region(
        nint baseAddress,
        int size,
        Func<int, int?> findFromOffset) =>
        new(baseAddress, size, findFromOffset);
}
