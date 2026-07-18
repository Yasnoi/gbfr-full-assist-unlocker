using System.Runtime.InteropServices;

namespace GBFR.InfinityFullAssist.Runtime;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void CurrentQuestIdGetter(
    nint questState,
    out uint questId);

internal sealed class QuestRuntimeStateReader
{
    // Verified against the exact 2.0.2 executable.
    internal const int AssistModeOffset = 0x10;
    internal const int DisableAssistTermOffset = 0xE4E;

    private readonly IRuntimeMemoryReader _memory;
    private readonly nint _questStateGlobalPointer;
    private readonly nint _assistSelectionGlobalPointer;
    private readonly CurrentQuestIdGetter _questIdGetter;

    public QuestRuntimeStateReader(
        IRuntimeMemoryReader memory,
        nint questStateGlobalPointer,
        nint assistSelectionGlobalPointer,
        CurrentQuestIdGetter questIdGetter)
    {
        _memory = memory ?? throw new ArgumentNullException(nameof(memory));
        _questStateGlobalPointer = questStateGlobalPointer;
        _assistSelectionGlobalPointer = assistSelectionGlobalPointer;
        _questIdGetter = questIdGetter ??
            throw new ArgumentNullException(nameof(questIdGetter));
    }

    public bool TryRead(out RuntimeAssistState state)
    {
        state = default;
        if (!_memory.TryReadPointer(_questStateGlobalPointer, out var questState) ||
            !ReloadedRuntimeMemoryReader.IsLikelyPointer(questState) ||
            !_memory.TryReadPointer(
                _assistSelectionGlobalPointer,
                out var assistSelection) ||
            !ReloadedRuntimeMemoryReader.IsLikelyPointer(assistSelection) ||
            !_memory.TryReadByte(
                assistSelection + AssistModeOffset,
                out var assistMode))
        {
            return false;
        }

        uint questId;
        try
        {
            _questIdGetter(questState, out questId);
        }
        catch (Exception)
        {
            return false;
        }

        bool? disableAssistTerm = null;
        if (_memory.TryReadByte(
                questState + DisableAssistTermOffset,
                out var disabled))
        {
            disableAssistTerm = disabled != 0;
        }

        state = new RuntimeAssistState(questId, assistMode, disableAssistTerm);
        return true;
    }
}
