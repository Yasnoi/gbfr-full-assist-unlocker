using Reloaded.Memory.Sigscan.Definitions;

namespace GBFR.InfinityFullAssist.Runtime;

internal sealed class RuntimeSignatureScanner : IDisposable
{
    private readonly IReadOnlyList<RegionScanner> _scanners;
    private readonly ModuleAddressRange _module;

    public RuntimeSignatureScanner(
        IScannerFactory scannerFactory,
        ModuleAddressRange module,
        IReadOnlyList<ExecutableMemorySnapshot> snapshots)
    {
        _module = module;

        var scanners = new List<RegionScanner>(snapshots.Count);
        try
        {
            foreach (var snapshot in snapshots)
            {
                scanners.Add(new RegionScanner(
                    snapshot.BaseAddress,
                    snapshot.Bytes.Length,
                    scannerFactory.CreateScanner(snapshot.Bytes)));
            }
        }
        catch (Exception)
        {
            foreach (var scanner in scanners)
            {
                scanner.Scanner.Dispose();
            }

            throw;
        }

        _scanners = scanners;
    }

    public SignatureMatchResult FindUnique(string signature) =>
        SignatureUniquenessVerifier.FindUnique(
            _module.BaseAddress,
            _module.Size,
            _scanners
                .Select(region => new SignatureSearchRegion(
                    region.BaseAddress,
                    region.Size,
                    offset =>
                    {
                        var result = region.Scanner.FindPattern(
                            signature,
                            offset);
                        return result.Found ? result.Offset : null;
                    }))
                .ToArray());

    public void Dispose()
    {
        foreach (var scanner in _scanners)
        {
            scanner.Scanner.Dispose();
        }
    }

    private readonly record struct RegionScanner(
        nint BaseAddress,
        int Size,
        IScanner Scanner);
}
