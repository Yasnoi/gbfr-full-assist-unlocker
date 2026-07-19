using GBFR.InfinityFullAssist.Core;
using GBFR.InfinityFullAssist.Runtime;

namespace GBFR.InfinityFullAssist.Tests;

public sealed class AssistModeUnlockPolicyTests
{
    private readonly AssistModeUnlockPolicy _policy = new(
        VerifiedInfinityData.AssistMode,
        VerifiedInfinityData.FullAssistMode);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FullAssistIsAlwaysUnlocked(bool enableAssistMode) =>
        Assert.True(_policy.IsUnlocked(
            VerifiedInfinityData.FullAssistMode,
            enableAssistMode));

    [Fact]
    public void AssistModeRequiresItsSetting() =>
        Assert.False(_policy.IsUnlocked(
            VerifiedInfinityData.AssistMode,
            enableAssistMode: false));

    [Fact]
    public void AssistModeIsUnlockedWhenItsSettingIsEnabled() =>
        Assert.True(_policy.IsUnlocked(
            VerifiedInfinityData.AssistMode,
            enableAssistMode: true));

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(byte.MaxValue)]
    public void OtherAssistModesRemainLocked(byte assistMode) =>
        Assert.False(_policy.IsUnlocked(
            assistMode,
            enableAssistMode: true));

    [Fact]
    public void DuplicateModeValuesAreRejected() =>
        Assert.Throws<ArgumentException>(
            () => new AssistModeUnlockPolicy(1, 1));
}
