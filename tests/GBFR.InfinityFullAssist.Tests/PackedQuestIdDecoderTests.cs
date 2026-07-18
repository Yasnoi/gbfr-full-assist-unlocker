using GBFR.InfinityFullAssist.Core;

namespace GBFR.InfinityFullAssist.Tests;

public sealed class PackedQuestIdDecoderTests
{
    private readonly PackedQuestIdDecoder _decoder = new(
        new QuestTypeResolver(
            (Category: 4, SubCategory: 11),
            [(Category: 4, SubCategory: 8), (Category: 4, SubCategory: 10)]));

    [Theory]
    [InlineData(0x40B301u)]
    [InlineData(0x40B309u)]
    [InlineData(0x40B313u)]
    [InlineData(0x40B314u)]
    [InlineData(0x40B316u)]
    public void VerifiedInfinityIdsDecodeTheirTypeBeforeFallback(uint questId)
    {
        var result = _decoder.Decode(questId);

        Assert.Equal(questId, result.QuestId);
        Assert.Equal(QuestTypeResolution.Infinity, result.TypeResolution);
    }

    [Theory]
    [InlineData(0x408301u)]
    [InlineData(0x40A301u)]
    public void VerifiedComparisonIdsDecodeAsNonInfinity(uint questId) =>
        Assert.Equal(
            QuestTypeResolution.NonInfinity,
            _decoder.Decode(questId).TypeResolution);

    [Theory]
    [InlineData(0u)]
    [InlineData(uint.MaxValue)]
    public void SentinelIdsAreUnavailable(uint questId) =>
        Assert.Equal(
            QuestTypeResolution.Unavailable,
            _decoder.Decode(questId).TypeResolution);

    [Fact]
    public void LayoutOutsideVerifiedUpperNibbleIsUnknown() =>
        Assert.Equal(
            QuestTypeResolution.Unknown,
            _decoder.Decode(0xF040B301).TypeResolution);
}
