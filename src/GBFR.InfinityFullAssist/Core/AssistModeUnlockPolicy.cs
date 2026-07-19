namespace GBFR.InfinityFullAssist.Core;

public sealed class AssistModeUnlockPolicy
{
    private readonly byte _partialAssistMode;
    private readonly byte _fullAssistMode;

    public AssistModeUnlockPolicy(byte partialAssistMode, byte fullAssistMode)
    {
        if (partialAssistMode == fullAssistMode)
        {
            throw new ArgumentException(
                "Partial Assist and Full Assist must use different mode values.");
        }

        _partialAssistMode = partialAssistMode;
        _fullAssistMode = fullAssistMode;
    }

    public bool IsUnlocked(byte assistMode, bool enablePartialAssist) =>
        assistMode == _fullAssistMode ||
        (enablePartialAssist && assistMode == _partialAssistMode);
}
