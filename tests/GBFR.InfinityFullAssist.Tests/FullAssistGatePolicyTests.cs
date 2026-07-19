using GBFR.InfinityFullAssist.Core;
using GBFR.InfinityFullAssist.Runtime;

namespace GBFR.InfinityFullAssist.Tests;

public sealed class FullAssistGatePolicyTests
{
    private readonly FullAssistGatePolicy _policy = new(
        new InfinityQuestClassifier([0xF00D]),
        new AssistModeUnlockPolicy(
            VerifiedInfinityData.AssistMode,
            VerifiedInfinityData.FullAssistMode));

    [Fact]
    public void OriginalTrueIsNeverChanged() =>
        Assert.True(_policy.Decide(
            originalResult: true,
            enabled: false,
            enableAssistMode: false,
            assistMode: 0,
            new(QuestTypeResolution.NonInfinity, 0)));

    [Fact]
    public void DisabledModPreservesOriginalFalse() =>
        Assert.False(_policy.Decide(
            originalResult: false,
            enabled: false,
            enableAssistMode: true,
            VerifiedInfinityData.AssistMode,
            new(QuestTypeResolution.Infinity, 0)));

    [Fact]
    public void UnsupportedAssistModePreservesOriginalFalse() =>
        Assert.False(_policy.Decide(
            originalResult: false,
            enabled: true,
            enableAssistMode: true,
            assistMode: 0,
            new(QuestTypeResolution.Infinity, 0)));

    [Fact]
    public void InfinityWithFullAssistOverridesFalse() =>
        Assert.True(_policy.Decide(
            originalResult: false,
            enabled: true,
            enableAssistMode: false,
            VerifiedInfinityData.FullAssistMode,
            new(QuestTypeResolution.Infinity, 0)));

    [Fact]
    public void InfinityWithAssistModeRequiresItsSetting() =>
        Assert.False(_policy.Decide(
            originalResult: false,
            enabled: true,
            enableAssistMode: false,
            VerifiedInfinityData.AssistMode,
            new(QuestTypeResolution.Infinity, 0)));

    [Fact]
    public void InfinityWithEnabledAssistModeOverridesFalse() =>
        Assert.True(_policy.Decide(
            originalResult: false,
            enabled: true,
            enableAssistMode: true,
            VerifiedInfinityData.AssistMode,
            new(QuestTypeResolution.Infinity, 0)));

    [Theory]
    [InlineData(VerifiedInfinityData.AssistMode, true)]
    [InlineData(VerifiedInfinityData.FullAssistMode, false)]
    public void OtherQuestPreservesOriginalFalse(
        byte assistMode,
        bool enableAssistMode) =>
        Assert.False(_policy.Decide(
            originalResult: false,
            enabled: true,
            enableAssistMode,
            assistMode,
            new(QuestTypeResolution.NonInfinity, 0)));
}
