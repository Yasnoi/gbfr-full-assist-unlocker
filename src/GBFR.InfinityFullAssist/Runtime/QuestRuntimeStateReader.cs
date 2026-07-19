using System.Runtime.InteropServices;
using GBFR.InfinityFullAssist.Core;

namespace GBFR.InfinityFullAssist.Runtime;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void CurrentQuestIdGetter(
    nint questState,
    out uint questId);

internal sealed class QuestRuntimeStateReader
{
    // Verified against ER 2.0.2 and guarded by runtime instruction validation.
    internal const int AssistModeOffset = 0x10;
    internal const int DisableAssistTermOffset = 0xE4E;
    internal const int OnlineQuestModeOffset = 0x4;

    private readonly IRuntimeMemoryReader _memory;
    private readonly nint _questStateGlobalPointer;
    private readonly nint _assistSelectionGlobalPointer;
    private nint _onlineQuestModeGlobalPointer;
    private readonly CurrentQuestIdGetter _questIdGetter;

    public QuestRuntimeStateReader(
        IRuntimeMemoryReader memory,
        nint questStateGlobalPointer,
        nint assistSelectionGlobalPointer,
        nint onlineQuestModeGlobalPointer,
        CurrentQuestIdGetter questIdGetter)
    {
        _memory = memory ?? throw new ArgumentNullException(nameof(memory));
        _questStateGlobalPointer = questStateGlobalPointer;
        _assistSelectionGlobalPointer = assistSelectionGlobalPointer;
        _onlineQuestModeGlobalPointer = onlineQuestModeGlobalPointer;
        _questIdGetter = questIdGetter ??
            throw new ArgumentNullException(nameof(questIdGetter));
    }

    public void UpdateOnlineQuestModeGlobalPointer(nint pointer) =>
        Volatile.Write(ref _onlineQuestModeGlobalPointer, pointer);

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

        state = new RuntimeAssistState(
            questId,
            assistMode,
            disableAssistTerm,
            ReadOnlineState());
        return true;
    }

    private QuestOnlineState ReadOnlineState()
    {
        if (!_memory.TryReadPointer(
                Volatile.Read(ref _onlineQuestModeGlobalPointer),
                out var onlineQuestModeState) ||
            !ReloadedRuntimeMemoryReader.IsLikelyPointer(onlineQuestModeState) ||
            !_memory.TryReadUInt32(
                onlineQuestModeState + OnlineQuestModeOffset,
                out var mode))
        {
            return QuestOnlineState.Unknown;
        }

        return mode == VerifiedInfinityData.OnlineQuestMode
            ? QuestOnlineState.Online
            : QuestOnlineState.Offline;
    }
}
