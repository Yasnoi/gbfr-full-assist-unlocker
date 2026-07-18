namespace GBFR.InfinityFullAssist.Runtime;

internal readonly record struct RuntimeAssistState(
    uint QuestId,
    byte AssistMode,
    bool? DisableAssistTerm);
