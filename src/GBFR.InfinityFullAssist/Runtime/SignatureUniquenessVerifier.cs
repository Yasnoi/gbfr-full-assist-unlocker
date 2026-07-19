namespace GBFR.InfinityFullAssist.Runtime;

internal static class SignatureUniquenessVerifier
{
    public static SignatureMatchResult FindUnique(
        nint moduleBase,
        int moduleSize,
        IReadOnlyList<SignatureSearchRegion> regions)
    {
        ArgumentNullException.ThrowIfNull(regions);

        if (moduleSize <= 0)
        {
            return new SignatureMatchResult(SignatureMatchStatus.InvalidOffset);
        }

        var module = new ModuleAddressRange(moduleBase, moduleSize);
        int? firstModuleOffset = null;
        foreach (var region in regions)
        {
            if (region.Size <= 0 ||
                !module.Contains(region.BaseAddress, region.Size))
            {
                return new SignatureMatchResult(
                    SignatureMatchStatus.InvalidOffset);
            }

            var searchOffset = 0;
            while (searchOffset < region.Size)
            {
                var regionOffset = region.FindFromOffset(searchOffset);
                if (regionOffset is null)
                {
                    break;
                }

                if (regionOffset < searchOffset ||
                    regionOffset >= region.Size)
                {
                    return new SignatureMatchResult(
                        SignatureMatchStatus.InvalidOffset,
                        FirstOffset: firstModuleOffset ?? -1);
                }

                var moduleOffsetValue =
                    (nuint)region.BaseAddress - (nuint)moduleBase +
                    (nuint)regionOffset.Value;
                if (moduleOffsetValue >= (nuint)moduleSize)
                {
                    return new SignatureMatchResult(
                        SignatureMatchStatus.InvalidOffset,
                        FirstOffset: firstModuleOffset ?? -1);
                }

                var moduleOffset = (int)moduleOffsetValue;
                if (firstModuleOffset is not null)
                {
                    return new SignatureMatchResult(
                        SignatureMatchStatus.Ambiguous,
                        FirstOffset: firstModuleOffset.Value,
                        SecondOffset: moduleOffset);
                }

                firstModuleOffset = moduleOffset;
                searchOffset = regionOffset.Value + 1;
            }
        }

        if (firstModuleOffset is null)
        {
            return new SignatureMatchResult(SignatureMatchStatus.Missing);
        }

        return new SignatureMatchResult(
            SignatureMatchStatus.Unique,
            moduleBase + firstModuleOffset.Value,
            firstModuleOffset.Value);
    }
}
