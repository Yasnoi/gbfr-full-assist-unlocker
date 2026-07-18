namespace GBFR.InfinityFullAssist.Runtime;

internal static class VerifiedInfinityData
{
    // `category_`/`subCategory_` were compared across extracted 2.0.2 BaseInfo files:
    // 408301 => 4/8, 40A301/309/313/314/316 => 4/10,
    // 40B301/309/313/314/316 => 4/11.
    public const int MultiQuestCategory = 4;
    public const int InfinitySubCategory = 11;
    public const byte FullAssistMode = 2;

    // These are the only quest IDs referenced by the 2.0.2 quest_infinity UI assets.
    public static ReadOnlySpan<uint> FallbackQuestIds =>
    [
        0x40B301, // The World
        0x40B314, // Beelzebub
        0x40B313, // Lucilius
        0x40B309, // Bahamut Versa
        0x40B316  // The Final Vision / Omnis Zegalith
    ];
}
