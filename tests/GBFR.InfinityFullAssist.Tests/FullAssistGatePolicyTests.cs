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
            enableOnlineSessions: false,
            assistMode: 0,
            onlineState: QuestOnlineState.Unknown,
            new(QuestTypeResolution.NonInfinity, 0)));

    [Fact]
    public void DisabledModPreservesOriginalFalse() =>
        Assert.False(_policy.Decide(
            originalResult: false,
            enabled: false,
            enableAssistMode: true,
            enableOnlineSessions: true,
            VerifiedInfinityData.AssistMode,
            QuestOnlineState.Online,
            new(QuestTypeResolution.Infinity, 0)));

    [Fact]
    public void UnsupportedAssistModePreservesOriginalFalse() =>
        Assert.False(_policy.Decide(
            originalResult: false,
            enabled: true,
            enableAssistMode: true,
            enableOnlineSessions: true,
            assistMode: 0,
            onlineState: QuestOnlineState.Online,
            new(QuestTypeResolution.Infinity, 0)));

    [Fact]
    public void InfinityWithFullAssistOverridesFalse() =>
        Assert.True(_policy.Decide(
            originalResult: false,
            enabled: true,
            enableAssistMode: false,
            enableOnlineSessions: true,
            VerifiedInfinityData.FullAssistMode,
            QuestOnlineState.Online,
            new(QuestTypeResolution.Infinity, 0)));

    [Fact]
    public void InfinityWithAssistModeRequiresItsSetting() =>
        Assert.False(_policy.Decide(
            originalResult: false,
            enabled: true,
            enableAssistMode: false,
            enableOnlineSessions: true,
            VerifiedInfinityData.AssistMode,
            QuestOnlineState.Online,
            new(QuestTypeResolution.Infinity, 0)));

    [Fact]
    public void InfinityWithEnabledAssistModeOverridesFalse() =>
        Assert.True(_policy.Decide(
            originalResult: false,
            enabled: true,
            enableAssistMode: true,
            enableOnlineSessions: true,
            VerifiedInfinityData.AssistMode,
            QuestOnlineState.Online,
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
            enableOnlineSessions: true,
            assistMode,
            onlineState: QuestOnlineState.Online,
            new(QuestTypeResolution.NonInfinity, 0)));

    [Theory]
    [InlineData(QuestOnlineState.Offline, true)]
    [InlineData(QuestOnlineState.Online, false)]
    [InlineData(QuestOnlineState.Unknown, false)]
    public void DisabledOnlineSessionsOnlyUnlocksKnownOfflineQuests(
        QuestOnlineState onlineState,
        bool expected) =>
        Assert.Equal(
            expected,
            _policy.Decide(
                originalResult: false,
                enabled: true,
                enableAssistMode: false,
                enableOnlineSessions: false,
                VerifiedInfinityData.FullAssistMode,
                onlineState,
                new(QuestTypeResolution.Infinity, 0)));

    [Theory]
    [InlineData(QuestOnlineState.Offline)]
    [InlineData(QuestOnlineState.Online)]
    [InlineData(QuestOnlineState.Unknown)]
    public void EnabledOnlineSessionsDoesNotRequireSessionState(
        QuestOnlineState onlineState) =>
        Assert.True(
            _policy.Decide(
                originalResult: false,
                enabled: true,
                enableAssistMode: false,
                enableOnlineSessions: true,
                VerifiedInfinityData.FullAssistMode,
                onlineState,
                new(QuestTypeResolution.Infinity, 0)));
}
