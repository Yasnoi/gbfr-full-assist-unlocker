namespace GBFR.InfinityFullAssist.Core;

public sealed class AssistModeUnlockPolicy
{
    private readonly byte _assistMode;
    private readonly byte _fullAssistMode;

    public AssistModeUnlockPolicy(byte assistMode, byte fullAssistMode)
    {
        if (assistMode == fullAssistMode)
        {
            throw new ArgumentException(
                "Assist Mode and Full Assist Mode must use different mode values.");
        }

        _assistMode = assistMode;
        _fullAssistMode = fullAssistMode;
    }

    public bool IsUnlocked(byte assistMode, bool enableAssistMode) =>
        assistMode == _fullAssistMode ||
        (enableAssistMode && assistMode == _assistMode);
}
