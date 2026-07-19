using System.Diagnostics;
using GBFR.InfinityFullAssist.Configuration;
using GBFR.InfinityFullAssist.Core;
using GBFR.InfinityFullAssist.Runtime;
using gbfrelink.utility.manager.Interfaces;
using NenTools.Reloaded.ScanManager.Interfaces;
using Reloaded.Mod.Interfaces;
using Reloaded.Memory.Sigscan.Definitions;
using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace GBFR.InfinityFullAssist;

internal sealed class Mod : IDisposable
{
    private readonly ILogger _logger;
    private readonly IModConfig _modConfig;
    private readonly ConfigStore _configStore;
    private FullAssistGateHook? _gateHook;

    public Mod(IModLoader modLoader, ILogger logger, IModConfig modConfig)
    {
        _logger = logger;
        _modConfig = modConfig;
        _configStore = new ConfigStore(modLoader.GetModConfigDirectory(modConfig.ModId));
        _configStore.Changed += OnConfigurationChanged;

        if (!TryGetUserDefinedParams(modLoader, out var userDefinedParams))
        {
            LogFailure("IUserDefinedParams is unavailable; original behavior retained.");
            return;
        }

        if (!TryVerifyBuild(userDefinedParams))
        {
            return;
        }

        if (!TryGetController(modLoader, out IReloadedHooks hooks))
        {
            LogFailure("IReloadedHooks is unavailable; original behavior retained.");
            return;
        }

        if (!TryGetController(modLoader, out IScanManager scanManager))
        {
            LogFailure("IScanManager is unavailable; original behavior retained.");
            return;
        }

        if (!TryGetController(modLoader, out IScannerFactory scannerFactory))
        {
            LogFailure("IScannerFactory is unavailable; original behavior retained.");
            return;
        }

        if (!TryCreateRuntimeSignatureScanner(
                scannerFactory,
                out var runtimeSignatureScanner,
                out var moduleRange))
        {
            return;
        }

        try
        {
            var signatureDirectory = Path.Combine(
                modLoader.GetDirectoryForModId(_modConfig.ModId),
                "Signatures");
            scanManager.InitializeScans(signatureDirectory, _modConfig.ModId);

            _gateHook = new FullAssistGateHook(
                _logger,
                _modConfig.ModId,
                _configStore.Current,
                hooks,
                new ReloadedRuntimeMemoryReader(),
                runtimeSignatureScanner,
                moduleRange);
            _gateHook.RegisterScans(scanManager);
        }
        catch (Exception ex)
        {
            if (_gateHook is not null)
            {
                _gateHook.Dispose();
                _gateHook = null;
            }
            else
            {
                runtimeSignatureScanner.Dispose();
            }

            LogFailure(
                $"Runtime signature registration failed " +
                $"({ex.GetType().Name}); original behavior retained.");
        }
    }

    private bool TryGetUserDefinedParams(IModLoader modLoader, out IUserDefinedParams userDefinedParams)
    {
        userDefinedParams = null!;
        var controller = modLoader.GetController<IUserDefinedParams>();
        if (controller is null || !controller.TryGetTarget(out var resolved) || resolved is null)
        {
            return false;
        }

        userDefinedParams = resolved;
        return true;
    }

    private static bool TryGetController<TController>(
        IModLoader modLoader,
        out TController controller)
        where TController : class
    {
        controller = null!;
        var reference = modLoader.GetController<TController>();
        return reference is not null &&
               reference.TryGetTarget(out var resolved) &&
               resolved is not null &&
               (controller = resolved) is not null;
    }

    private bool TryVerifyBuild(IUserDefinedParams userDefinedParams)
    {
        try
        {
            if (!userDefinedParams.IsEndlessRagnarok())
            {
                LogFailure("The running game is not Endless Ragnarok; original behavior retained.");
                return false;
            }

            if (userDefinedParams.GetGameVersion() !=
                GameVersion.RelinkEndlessRagnarok)
            {
                LogFailure("The running game is not the formal Endless Ragnarok release; original behavior retained.");
                return false;
            }

            var sha256 = TryComputeRunningExecutableSha256();
            var identity = new BuildIdentity(
                userDefinedParams.ApplicationVersion,
                sha256);
            var status = new BuildVerifier().Verify(identity);

            if (_configStore.Current.DiagnosticLogging)
            {
                _logger.WriteLine(
                    $"[{_modConfig.ModId}] " +
                    $"ApplicationVersion={identity.ApplicationVersion}; " +
                    $"SHA-256={identity.Sha256 ?? "unavailable"}");
            }

            if (status == BuildVerificationStatus.Unsupported)
            {
                LogFailure(
                    $"Unsupported ApplicationVersion " +
                    $"{identity.ApplicationVersion}; original behavior retained.");
                return false;
            }

            if (status == BuildVerificationStatus.Verified)
            {
                _logger.WriteLine(
                    $"[{_modConfig.ModId}] Verified Endless Ragnarok 2.0.2 executable.",
                    System.Drawing.Color.Green);
            }
            else
            {
                LogWarning(
                    "The executable SHA-256 does not match the verified " +
                    "build or could not be read. Runtime signature " +
                    "validation will determine compatibility.");
            }

            return true;
        }
        catch (Exception ex)
        {
            LogFailure($"Build validation failed ({ex.GetType().Name}); original behavior retained.");
            return false;
        }
    }

    private static string? TryComputeRunningExecutableSha256()
    {
        try
        {
            var executablePath = Environment.ProcessPath ??
                Process.GetCurrentProcess().MainModule?.FileName;
            return string.IsNullOrWhiteSpace(executablePath)
                ? null
                : BuildVerifier.ComputeSha256(executablePath);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private bool TryCreateRuntimeSignatureScanner(
        IScannerFactory scannerFactory,
        out RuntimeSignatureScanner scanner,
        out ModuleAddressRange moduleRange)
    {
        scanner = null!;
        moduleRange = default;

        try
        {
            using var process = Process.GetCurrentProcess();
            var mainModule = process.MainModule;
            if (mainModule is null ||
                mainModule.BaseAddress == 0 ||
                mainModule.ModuleMemorySize <= 0)
            {
                LogFailure(
                    "The main executable module is unavailable; original behavior retained.");
                return false;
            }

            moduleRange = new ModuleAddressRange(
                mainModule.BaseAddress,
                mainModule.ModuleMemorySize);
            var snapshotter = new RuntimeExecutableMemorySnapshotter(
                new WindowsRuntimeMemoryRegionSource());
            if (!snapshotter.TryCapture(
                    moduleRange,
                    out var snapshots,
                    out var failure))
            {
                LogFailure(
                    $"Executable memory snapshot failed: {failure}; " +
                    "original behavior retained.");
                return false;
            }

            scanner = new RuntimeSignatureScanner(
                scannerFactory,
                moduleRange,
                snapshots);
            return true;
        }
        catch (Exception ex)
        {
            LogFailure(
                $"Runtime signature scanner initialization failed " +
                $"({ex.GetType().Name}); original behavior retained.");
            return false;
        }
    }

    private void OnConfigurationChanged(Config config)
    {
        _gateHook?.UpdateConfiguration(config);
        if (config.DiagnosticLogging)
        {
            _logger.WriteLine(
                $"[{_modConfig.ModId}] Configuration updated: " +
                $"Enabled={config.Enabled}, " +
                $"EnableAssistMode={config.EnableAssistMode}, " +
                $"EnableOnlineSessions={config.EnableOnlineSessions}, " +
                "DiagnosticLogging=true.");
        }
    }

    private void LogFailure(string message) =>
        _logger.WriteLine($"[{_modConfig.ModId}] {message}", System.Drawing.Color.Red);

    private void LogWarning(string message) =>
        _logger.WriteLine(
            $"[{_modConfig.ModId}] {message}",
            System.Drawing.Color.Yellow);

    public void Dispose()
    {
        _gateHook?.Dispose();
        _gateHook = null;
        _configStore.Changed -= OnConfigurationChanged;
        _configStore.Dispose();
    }
}
