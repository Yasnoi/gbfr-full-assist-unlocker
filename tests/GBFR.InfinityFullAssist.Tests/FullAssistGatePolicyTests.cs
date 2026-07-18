using GBFR.InfinityFullAssist.Core;

namespace GBFR.InfinityFullAssist.Tests;

public sealed class FullAssistGatePolicyTests
{
    private readonly FullAssistGatePolicy _policy =
        new(new InfinityQuestClassifier([0xF00D]));

    [Fact]
    public void OriginalTrueIsNeverChanged() =>
        Assert.True(_policy.Decide(true, false, false, new(QuestTypeResolution.NonInfinity, 0)));

    [Fact]
    public void DisabledModPreservesOriginalFalse() =>
        Assert.False(_policy.Decide(false, false, true, new(QuestTypeResolution.Infinity, 0)));

    [Fact]
    public void NonFullAssistPreservesOriginalFalse() =>
        Assert.False(_policy.Decide(false, true, false, new(QuestTypeResolution.Infinity, 0)));

    [Fact]
    public void InfinityWithFullAssistOverridesFalse() =>
        Assert.True(_policy.Decide(false, true, true, new(QuestTypeResolution.Infinity, 0)));

    [Fact]
    public void OtherQuestPreservesOriginalFalse() =>
        Assert.False(_policy.Decide(false, true, true, new(QuestTypeResolution.NonInfinity, 0)));
}
