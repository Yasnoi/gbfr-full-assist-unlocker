using GBFR.InfinityFullAssist.Core;

namespace GBFR.InfinityFullAssist.Tests;

public sealed class QuestTypeResolverTests
{
    private readonly QuestTypeResolver _resolver = new(
        (Category: 4, SubCategory: 11),
        [(Category: 4, SubCategory: 8), (Category: 4, SubCategory: 10)]);

    [Fact]
    public void ExactVerifiedInfinityTypeResolvesToInfinity() =>
        Assert.Equal(
            QuestTypeResolution.Infinity,
            _resolver.Resolve(new(true, Category: 4, SubCategory: 11)));

    [Theory]
    [InlineData(4, 8)]
    [InlineData(4, 10)]
    public void VerifiedOtherTypesResolveToNonInfinity(int category, int subCategory) =>
        Assert.Equal(
            QuestTypeResolution.NonInfinity,
            _resolver.Resolve(new(true, category, subCategory)));

    [Fact]
    public void UnreadableTypeIsUnavailable() =>
        Assert.Equal(
            QuestTypeResolution.Unavailable,
            _resolver.Resolve(new(false, 0, 0)));

    [Fact]
    public void UnverifiedTypeIsUnknown() =>
        Assert.Equal(
            QuestTypeResolution.Unknown,
            _resolver.Resolve(new(true, Category: 99, SubCategory: 99)));
}
