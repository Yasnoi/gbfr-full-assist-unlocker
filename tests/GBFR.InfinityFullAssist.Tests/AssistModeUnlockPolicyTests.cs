using GBFR.InfinityFullAssist.Core;
using GBFR.InfinityFullAssist.Runtime;

namespace GBFR.InfinityFullAssist.Tests;

public sealed class AssistModeUnlockPolicyTests
{
    private readonly AssistModeUnlockPolicy _policy = new(
        VerifiedInfinityData.PartialAssistMode,
        VerifiedInfinityData.FullAssistMode);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FullAssistIsAlwaysUnlocked(bool enablePartialAssist) =>
        Assert.True(_policy.IsUnlocked(
            VerifiedInfinityData.FullAssistMode,
            enablePartialAssist));

    [Fact]
    public void PartialAssistRequiresItsSetting() =>
        Assert.False(_policy.IsUnlocked(
            VerifiedInfinityData.PartialAssistMode,
            enablePartialAssist: false));

    [Fact]
    public void PartialAssistIsUnlockedWhenItsSettingIsEnabled() =>
        Assert.True(_policy.IsUnlocked(
            VerifiedInfinityData.PartialAssistMode,
            enablePartialAssist: true));

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(byte.MaxValue)]
    public void OtherAssistModesRemainLocked(byte assistMode) =>
        Assert.False(_policy.IsUnlocked(
            assistMode,
            enablePartialAssist: true));

    [Fact]
    public void DuplicateModeValuesAreRejected() =>
        Assert.Throws<ArgumentException>(
            () => new AssistModeUnlockPolicy(1, 1));
}
