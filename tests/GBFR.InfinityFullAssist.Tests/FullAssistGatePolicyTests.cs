using GBFR.InfinityFullAssist.Core;
using GBFR.InfinityFullAssist.Runtime;

namespace GBFR.InfinityFullAssist.Tests;

public sealed class FullAssistGatePolicyTests
{
    private readonly FullAssistGatePolicy _policy = new(
        new InfinityQuestClassifier([0xF00D]),
        new AssistModeUnlockPolicy(
            VerifiedInfinityData.PartialAssistMode,
            VerifiedInfinityData.FullAssistMode));

    [Fact]
    public void OriginalTrueIsNeverChanged() =>
        Assert.True(_policy.Decide(
            originalResult: true,
            enabled: false,
            enablePartialAssist: false,
            assistMode: 0,
            new(QuestTypeResolution.NonInfinity, 0)));

    [Fact]
    public void DisabledModPreservesOriginalFalse() =>
        Assert.False(_policy.Decide(
            originalResult: false,
            enabled: false,
            enablePartialAssist: true,
            VerifiedInfinityData.PartialAssistMode,
            new(QuestTypeResolution.Infinity, 0)));

    [Fact]
    public void UnsupportedAssistModePreservesOriginalFalse() =>
        Assert.False(_policy.Decide(
            originalResult: false,
            enabled: true,
            enablePartialAssist: true,
            assistMode: 0,
            new(QuestTypeResolution.Infinity, 0)));

    [Fact]
    public void InfinityWithFullAssistOverridesFalse() =>
        Assert.True(_policy.Decide(
            originalResult: false,
            enabled: true,
            enablePartialAssist: false,
            VerifiedInfinityData.FullAssistMode,
            new(QuestTypeResolution.Infinity, 0)));

    [Fact]
    public void InfinityWithPartialAssistRequiresItsSetting() =>
        Assert.False(_policy.Decide(
            originalResult: false,
            enabled: true,
            enablePartialAssist: false,
            VerifiedInfinityData.PartialAssistMode,
            new(QuestTypeResolution.Infinity, 0)));

    [Fact]
    public void InfinityWithEnabledPartialAssistOverridesFalse() =>
        Assert.True(_policy.Decide(
            originalResult: false,
            enabled: true,
            enablePartialAssist: true,
            VerifiedInfinityData.PartialAssistMode,
            new(QuestTypeResolution.Infinity, 0)));

    [Theory]
    [InlineData(VerifiedInfinityData.PartialAssistMode, true)]
    [InlineData(VerifiedInfinityData.FullAssistMode, false)]
    public void OtherQuestPreservesOriginalFalse(
        byte assistMode,
        bool enablePartialAssist) =>
        Assert.False(_policy.Decide(
            originalResult: false,
            enabled: true,
            enablePartialAssist,
            assistMode,
            new(QuestTypeResolution.NonInfinity, 0)));
}
