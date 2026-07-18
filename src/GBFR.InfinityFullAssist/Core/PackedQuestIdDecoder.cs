namespace GBFR.InfinityFullAssist.Core;

public sealed class PackedQuestIdDecoder
{
    private readonly QuestTypeResolver _typeResolver;

    public PackedQuestIdDecoder(QuestTypeResolver typeResolver)
    {
        _typeResolver = typeResolver ?? throw new ArgumentNullException(nameof(typeResolver));
    }

    public QuestSnapshot Decode(uint questId)
    {
        if (questId is 0 or uint.MaxValue)
        {
            return new QuestSnapshot(QuestTypeResolution.Unavailable, questId);
        }

        // The 2.0.2 BaseInfo loader constructs the packed ID as:
        // category_ << 20 | subCategory_ << 12 | sequence.
        // Values using the upper nibble are outside that verified layout.
        if ((questId & 0xF0000000u) != 0)
        {
            return new QuestSnapshot(QuestTypeResolution.Unknown, questId);
        }

        var category = (int)((questId >> 20) & 0xFF);
        var subCategory = (int)((questId >> 12) & 0xFF);
        var observation = new QuestTypeObservation(true, category, subCategory);
        return new QuestSnapshot(_typeResolver.Resolve(observation), questId);
    }
}
