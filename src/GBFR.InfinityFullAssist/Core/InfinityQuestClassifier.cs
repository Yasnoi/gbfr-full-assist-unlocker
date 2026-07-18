namespace GBFR.InfinityFullAssist.Core;

public sealed class InfinityQuestClassifier
{
    private readonly HashSet<uint> _fallbackQuestIds;

    public InfinityQuestClassifier(IEnumerable<uint> fallbackQuestIds)
    {
        ArgumentNullException.ThrowIfNull(fallbackQuestIds);
        _fallbackQuestIds = [.. fallbackQuestIds];
    }

    public bool IsInfinity(in QuestSnapshot quest) => quest.TypeResolution switch
    {
        QuestTypeResolution.Infinity => true,
        QuestTypeResolution.NonInfinity => false,
        QuestTypeResolution.Unknown or QuestTypeResolution.Unavailable =>
            _fallbackQuestIds.Contains(quest.QuestId),
        _ => false
    };
}
