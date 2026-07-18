namespace GBFR.InfinityFullAssist.Core;

public sealed class QuestTypeResolver
{
    private readonly (int Category, int SubCategory) _infinityType;
    private readonly HashSet<(int Category, int SubCategory)> _knownNonInfinityTypes;

    public QuestTypeResolver(
        (int Category, int SubCategory) infinityType,
        IEnumerable<(int Category, int SubCategory)> knownNonInfinityTypes)
    {
        ArgumentNullException.ThrowIfNull(knownNonInfinityTypes);
        _infinityType = infinityType;
        _knownNonInfinityTypes = [.. knownNonInfinityTypes];
        _knownNonInfinityTypes.Remove(infinityType);
    }

    public QuestTypeResolution Resolve(in QuestTypeObservation observation)
    {
        if (!observation.IsReadable)
        {
            return QuestTypeResolution.Unavailable;
        }

        var value = (observation.Category, observation.SubCategory);
        if (value == _infinityType)
        {
            return QuestTypeResolution.Infinity;
        }

        return _knownNonInfinityTypes.Contains(value)
            ? QuestTypeResolution.NonInfinity
            : QuestTypeResolution.Unknown;
    }
}
