namespace GBFR.InfinityFullAssist.Core;

public sealed class FullAssistGatePolicy
{
    private readonly InfinityQuestClassifier _classifier;

    public FullAssistGatePolicy(InfinityQuestClassifier classifier)
    {
        _classifier = classifier ?? throw new ArgumentNullException(nameof(classifier));
    }

    public bool Decide(
        bool originalResult,
        bool enabled,
        bool fullAssistSelected,
        in QuestSnapshot quest)
    {
        if (originalResult)
        {
            return true;
        }

        if (!enabled || !fullAssistSelected)
        {
            return false;
        }

        return _classifier.IsInfinity(quest);
    }
}
