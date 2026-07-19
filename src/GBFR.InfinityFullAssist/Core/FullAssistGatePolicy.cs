namespace GBFR.InfinityFullAssist.Core;

public sealed class FullAssistGatePolicy
{
    private readonly InfinityQuestClassifier _classifier;
    private readonly AssistModeUnlockPolicy _assistModePolicy;

    public FullAssistGatePolicy(
        InfinityQuestClassifier classifier,
        AssistModeUnlockPolicy assistModePolicy)
    {
        _classifier = classifier ?? throw new ArgumentNullException(nameof(classifier));
        _assistModePolicy = assistModePolicy ??
            throw new ArgumentNullException(nameof(assistModePolicy));
    }

    public bool Decide(
        bool originalResult,
        bool enabled,
        bool enableAssistMode,
        byte assistMode,
        in QuestSnapshot quest)
    {
        if (originalResult)
        {
            return true;
        }

        if (!enabled ||
            !_assistModePolicy.IsUnlocked(assistMode, enableAssistMode))
        {
            return false;
        }

        return _classifier.IsInfinity(quest);
    }
}
