using GBFR.InfinityFullAssist.Core;
using GBFR.InfinityFullAssist.Runtime;

namespace GBFR.InfinityFullAssist.Tests;

public sealed class InfinityQuestClassifierTests
{
    private const uint FallbackId = 0x12345678;
    private readonly InfinityQuestClassifier _classifier = new([FallbackId]);

    [Fact]
    public void InfinityTypeAlwaysWins() =>
        Assert.True(_classifier.IsInfinity(new(QuestTypeResolution.Infinity, 0)));

    [Fact]
    public void ExplicitNonInfinityNeverFallsBack() =>
        Assert.False(_classifier.IsInfinity(new(QuestTypeResolution.NonInfinity, FallbackId)));

    [Theory]
    [InlineData(QuestTypeResolution.Unknown)]
    [InlineData(QuestTypeResolution.Unavailable)]
    public void UnknownOrUnavailableTypeUsesKnownId(QuestTypeResolution resolution) =>
        Assert.True(_classifier.IsInfinity(new(resolution, FallbackId)));

    [Theory]
    [InlineData(QuestTypeResolution.Unknown)]
    [InlineData(QuestTypeResolution.Unavailable)]
    public void UnknownContextWithoutKnownIdFailsClosed(QuestTypeResolution resolution) =>
        Assert.False(_classifier.IsInfinity(new(resolution, 0)));

    [Fact]
    public void VerifiedFallbackContainsExactlyTheFiveInfinityQuestIds()
    {
        uint[] expected = [0x40B301, 0x40B314, 0x40B313, 0x40B309, 0x40B316];

        Assert.Equal(expected, VerifiedInfinityData.FallbackQuestIds.ToArray());
    }
}
