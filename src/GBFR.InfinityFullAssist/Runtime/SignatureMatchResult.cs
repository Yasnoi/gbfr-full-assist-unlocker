namespace GBFR.InfinityFullAssist.Runtime;

internal readonly record struct SignatureMatchResult(
    SignatureMatchStatus Status,
    nint Address = 0,
    int FirstOffset = -1,
    int SecondOffset = -1);
