using System.Runtime.InteropServices;
using GBFR.InfinityFullAssist.Configuration;
using GBFR.InfinityFullAssist.Core;
using NenTools.Reloaded.ScanManager.Interfaces;
using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;
using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace GBFR.InfinityFullAssist.Runtime;

internal sealed class FullAssistGateHook : IDisposable
{
    private const string SignatureGroup = "granblue_fantasy_relink_er_2_0_3";
    private const string GateScanName = "FullAssistGate";
    private const string AssistDisableHandlerScanName =
        "AssistDisableTermHandler";
    private const string OnlineQuestModeScanName = "OnlineQuestMode";

    internal const string GateSignature =
        "48 83 EC 28 48 8B 0D ?? ?? ?? ?? 48 8D 54 24 24 E8 ?? ?? ?? ?? " +
        "8B 4C 24 24 89 C8 C1 E8 14";

    internal const string AssistDisableHandlerSignature =
        "56 57 48 83 EC 28 48 8B 05 ?? ?? ?? ?? 0F B6 49 30 " +
        "88 88 4E 0E 00 00 48 8B 3D ?? ?? ?? ?? 31 F6 E8 ?? ?? ?? ?? " +
        "B9 00 00 00 00 84 C0 74 ?? 8B 47 10";

    internal const string OnlineQuestModeSignature =
        "48 83 79 10 00 74 ?? 48 8B 05 ?? ?? ?? ?? 8B 48 04 " +
        "B0 01 83 F9 03 74 ?? 31 C0 C3";

    internal const string GateValidationSignature =
        GateSignature +
        " 89 C2 80 E2 0F 80 FA 06 74 ?? 83 E0 0F 83 F8 03 74 ?? " +
        "83 F8 01 75 ??";

    internal const string AssistDisableHandlerValidationSignature =
        AssistDisableHandlerSignature +
        " 84 C0 0F 4E C6 3C 02 B9 02 00 00 00 0F 42 C8 80 F9 01 " +
        "74 ?? 0F B6 C1 83 F8 02 75 ?? B1 01 40 B6 01 EB ?? " +
        "31 C9 31 F6 48 8B 05 ?? ?? ?? ?? 86 48 11 " +
        "48 8B 05 ?? ?? ?? ?? 40 86 70 12 48 83 C4 28 5F 5E C3";

    internal const string OnlineQuestModeValidationSignature =
        OnlineQuestModeSignature;

    private static readonly BytePattern GateValidationPattern =
        BytePattern.Parse(GateValidationSignature);
    private static readonly BytePattern AssistDisableHandlerValidationPattern =
        BytePattern.Parse(AssistDisableHandlerValidationSignature);
    private static readonly BytePattern OnlineQuestModeValidationPattern =
        BytePattern.Parse(OnlineQuestModeValidationSignature);

    private readonly ILogger _logger;
    private readonly string _modId;
    private readonly IReloadedHooks _hooks;
    private readonly IRuntimeMemoryReader _memory;
    private readonly RuntimeSignatureScanner _signatureScanner;
    private readonly ModuleAddressRange _module;
    private readonly object _installLock = new();
    private readonly object _logLock = new();
    private readonly InfinityQuestClassifier _classifier;
    private readonly FullAssistGatePolicy _policy;
    private readonly PackedQuestIdDecoder _questIdDecoder;

    private Config _config;
    private nint _gateAddress;
    private nint _assistDisableTermHandlerAddress;
    private nint _onlineQuestModeAddress;
    private nint _onlineQuestModeGlobalPointer;
    private QuestRuntimeStateReader? _stateReader;
    private IHook<FullAssistGateDelegate>? _gateHook;
    private DecisionLogKey? _lastDecisionLog;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate byte FullAssistGateDelegate();

    public FullAssistGateHook(
        ILogger logger,
        string modId,
        Config config,
        IReloadedHooks hooks,
        IRuntimeMemoryReader memory,
        RuntimeSignatureScanner signatureScanner,
        ModuleAddressRange module)
    {
        _logger = logger;
        _modId = modId;
        _config = config;
        _hooks = hooks;
        _memory = memory;
        _signatureScanner = signatureScanner;
        _module = module;

        _classifier = new InfinityQuestClassifier(
            VerifiedInfinityData.FallbackQuestIds.ToArray());
        _policy = new FullAssistGatePolicy(
            _classifier,
            new AssistModeUnlockPolicy(
                VerifiedInfinityData.AssistMode,
                VerifiedInfinityData.FullAssistMode));
        _questIdDecoder = new PackedQuestIdDecoder(
            new QuestTypeResolver(
                (
                    VerifiedInfinityData.MultiQuestCategory,
                    VerifiedInfinityData.InfinitySubCategory
                ),
                [
                    (Category: 4, SubCategory: 8),
                    (Category: 4, SubCategory: 10)
                ]));
    }

    public bool IsInstalled => _gateHook?.IsHookEnabled == true;

    public void UpdateConfiguration(Config config) =>
        Volatile.Write(ref _config, config);

    public void RegisterScans(IScanManager scanManager)
    {
        ArgumentNullException.ThrowIfNull(scanManager);

        scanManager.AddScan(
            GateScanName,
            SignatureGroup,
            address =>
            {
                if (!TryValidateRuntimeSignature(
                        GateScanName,
                        GateSignature,
                        GateValidationPattern,
                        address,
                        required: true,
                        out var validatedAddress))
                {
                    return;
                }

                _gateAddress = validatedAddress;
                TryInstall();
            },
            () => LogSignatureFailure(
                GateScanName,
                "was not found",
                required: true));
        scanManager.AddScan(
            AssistDisableHandlerScanName,
            SignatureGroup,
            address =>
            {
                if (!TryValidateRuntimeSignature(
                        AssistDisableHandlerScanName,
                        AssistDisableHandlerSignature,
                        AssistDisableHandlerValidationPattern,
                        address,
                        required: true,
                        out var validatedAddress))
                {
                    return;
                }

                _assistDisableTermHandlerAddress = validatedAddress;
                TryInstall();
            },
            () => LogSignatureFailure(
                AssistDisableHandlerScanName,
                "was not found",
                required: true));
        scanManager.AddScan(
            OnlineQuestModeScanName,
            SignatureGroup,
            address =>
            {
                if (!TryValidateRuntimeSignature(
                        OnlineQuestModeScanName,
                        OnlineQuestModeSignature,
                        OnlineQuestModeValidationPattern,
                        address,
                        required: false,
                        out var validatedAddress))
                {
                    return;
                }

                _onlineQuestModeAddress = validatedAddress;
                TryInitializeOnlineQuestModeState();
            },
            () => LogSignatureFailure(
                OnlineQuestModeScanName,
                "was not found",
                required: false));
    }

    internal bool EvaluateForTests(
        bool originalResult,
        byte assistMode,
        QuestOnlineState onlineState,
        QuestSnapshot snapshot)
    {
        var config = Volatile.Read(ref _config);
        return _policy.Decide(
            originalResult,
            config.Enabled,
            config.EnableAssistMode,
            config.EnableOnlineSessions,
            assistMode,
            onlineState,
            snapshot);
    }

    private void TryInstall()
    {
        lock (_installLock)
        {
            if (_gateHook is not null ||
                _gateAddress == 0 ||
                _assistDisableTermHandlerAddress == 0)
            {
                return;
            }

            if (!RuntimeAddressResolver.TryResolveRipRelativeAddress(
                    _memory,
                    _gateAddress + 4,
                    displacementOffset: 3,
                    instructionLength: 7,
                    out var questStateGlobalPointer))
            {
                LogFailure(
                    "The Quest state RIP-relative instruction could not be resolved.");
                return;
            }

            if (!_module.Contains(questStateGlobalPointer, IntPtr.Size))
            {
                LogFailure(
                    "The resolved Quest state global target is outside the main module.");
                return;
            }

            if (!RuntimeAddressResolver.TryResolveRipRelativeAddress(
                    _memory,
                    _assistDisableTermHandlerAddress + 0x17,
                    displacementOffset: 3,
                    instructionLength: 7,
                    out var assistSelectionGlobalPointer))
            {
                LogFailure(
                    "The Assist selection RIP-relative instruction could not be resolved.");
                return;
            }

            if (!_module.Contains(assistSelectionGlobalPointer, IntPtr.Size))
            {
                LogFailure(
                    "The resolved Assist selection global target is outside the main module.");
                return;
            }

            if (!RuntimeAddressResolver.TryResolveRelativeCallAddress(
                    _memory,
                    _gateAddress + 0x10,
                    out var questIdGetterAddress))
            {
                LogFailure(
                    "The Quest ID getter call could not be resolved.");
                return;
            }

            if (!_module.Contains(questIdGetterAddress))
            {
                LogFailure(
                    "The resolved Quest ID getter target is outside the main module.");
                return;
            }

            try
            {
                var questIdGetter = _hooks.CreateWrapper<CurrentQuestIdGetter>(
                    questIdGetterAddress,
                    out _);
                _stateReader = new QuestRuntimeStateReader(
                    _memory,
                    questStateGlobalPointer,
                    assistSelectionGlobalPointer,
                    Volatile.Read(ref _onlineQuestModeGlobalPointer),
                    questIdGetter);

                var hook = _hooks.CreateHook<FullAssistGateDelegate>(
                    FullAssistGateDetour,
                    _gateAddress);
                _gateHook = hook;
                hook.Activate();

                _logger.WriteLine(
                    $"[{_modId}] Infinity Assist gate hook installed for ER 2.0.3.",
                    System.Drawing.Color.Green);
            }
            catch (Exception ex)
            {
                try
                {
                    _gateHook?.Disable();
                }
                catch (Exception)
                {
                    // Installation already failed. Keep unwinding fail-closed.
                }

                _gateHook = null;
                _stateReader = null;
                LogFailure(
                    $"Hook installation failed ({ex.GetType().Name}).");
            }
        }
    }

    private void TryInitializeOnlineQuestModeState()
    {
        lock (_installLock)
        {
            if (!RuntimeAddressResolver.TryResolveRipRelativeAddress(
                    _memory,
                    _onlineQuestModeAddress + 0x7,
                    displacementOffset: 3,
                    instructionLength: 7,
                    out var globalPointer))
            {
                LogOnlineStateFailure(
                    "Failed to resolve the verified online Quest mode state pointer.");
                return;
            }

            if (!_module.Contains(globalPointer, IntPtr.Size))
            {
                LogOnlineStateFailure(
                    "The resolved online Quest mode global target is outside the main module.");
                return;
            }

            Volatile.Write(ref _onlineQuestModeGlobalPointer, globalPointer);
            Volatile.Read(ref _stateReader)?
                .UpdateOnlineQuestModeGlobalPointer(globalPointer);
        }
    }

    private byte FullAssistGateDetour()
    {
        var hook = _gateHook;
        if (hook is null)
        {
            return 0;
        }

        var originalResult = hook.OriginalFunction();
        if (originalResult != 0)
        {
            LogDecisionIfRequested(
                default,
                QuestTypeResolution.Unavailable,
                originalResult,
                originalResult,
                stateReadable: false);
            return originalResult;
        }

        var config = Volatile.Read(ref _config);
        if (!config.Enabled && !config.DiagnosticLogging)
        {
            return originalResult;
        }

        try
        {
            var stateReader = _stateReader;
            if (stateReader is null || !stateReader.TryRead(out var state))
            {
                LogDecisionIfRequested(
                    default,
                    QuestTypeResolution.Unavailable,
                    originalResult,
                    originalResult,
                    stateReadable: false);
                return originalResult;
            }

            var quest = _questIdDecoder.Decode(state.QuestId);
            var result = _policy.Decide(
                originalResult: false,
                enabled: config.Enabled,
                enableAssistMode: config.EnableAssistMode,
                enableOnlineSessions: config.EnableOnlineSessions,
                assistMode: state.AssistMode,
                onlineState: state.OnlineState,
                quest);
            var finalResult = result ? (byte)1 : originalResult;

            LogDecisionIfRequested(
                state,
                quest.TypeResolution,
                originalResult,
                finalResult,
                stateReadable: true);
            return finalResult;
        }
        catch (Exception ex)
        {
            if (config.DiagnosticLogging)
            {
                _logger.WriteLine(
                    $"[{_modId}] Runtime state read failed ({ex.GetType().Name}); original result retained.",
                    System.Drawing.Color.Yellow);
            }

            return originalResult;
        }
    }

    private bool TryValidateRuntimeSignature(
        string name,
        string scanSignature,
        BytePattern validationPattern,
        nint scanManagerAddress,
        bool required,
        out nint validatedAddress)
    {
        validatedAddress = 0;
        SignatureMatchResult result;
        try
        {
            result = _signatureScanner.FindUnique(scanSignature);
        }
        catch (Exception ex)
        {
            LogSignatureFailure(
                name,
                $"could not be scanned ({ex.GetType().Name})",
                required);
            return false;
        }

        switch (result.Status)
        {
            case SignatureMatchStatus.Missing:
                LogSignatureFailure(name, "was not found", required);
                return false;
            case SignatureMatchStatus.Ambiguous:
                LogSignatureFailure(
                    name,
                    $"matched more than once " +
                    $"(0x{result.FirstOffset:X}, 0x{result.SecondOffset:X})",
                    required);
                return false;
            case SignatureMatchStatus.InvalidOffset:
                LogSignatureFailure(
                    name,
                    "returned an invalid module offset",
                    required);
                return false;
            case SignatureMatchStatus.Unique:
                break;
            default:
                LogSignatureFailure(
                    name,
                    "returned an unknown scan result",
                    required);
                return false;
        }

        if (result.Address != scanManagerAddress)
        {
            LogSignatureFailure(
                name,
                "did not agree with the managed scan result",
                required);
            return false;
        }

        if (!_module.Contains(result.Address, validationPattern.Length))
        {
            LogSignatureFailure(
                name,
                "validation window is outside the main module",
                required);
            return false;
        }

        var bytes = new byte[validationPattern.Length];
        if (!_memory.TryReadBytes(result.Address, bytes) ||
            !validationPattern.Matches(bytes))
        {
            LogSignatureFailure(
                name,
                "instruction structure validation failed",
                required);
            return false;
        }

        validatedAddress = result.Address;
        return true;
    }

    private void LogDecisionIfRequested(
        RuntimeAssistState state,
        QuestTypeResolution typeResolution,
        byte originalResult,
        byte finalResult,
        bool stateReadable)
    {
        if (!Volatile.Read(ref _config).DiagnosticLogging)
        {
            return;
        }

        var key = new DecisionLogKey(
            stateReadable,
            state.QuestId,
            state.AssistMode,
            state.DisableAssistTerm,
            state.OnlineState,
            typeResolution,
            originalResult,
            finalResult);
        lock (_logLock)
        {
            if (_lastDecisionLog == key)
            {
                return;
            }

            _lastDecisionLog = key;
            var assistMode = stateReadable
                ? state.AssistMode.ToString()
                : "unavailable";
            _logger.WriteLine(
                $"[{_modId}] gate=0x{_gateAddress:X}; " +
                $"state={(stateReadable ? "readable" : "unavailable")}; " +
                $"quest=0x{state.QuestId:X6}; type={typeResolution}; " +
                $"assistMode={assistMode}; " +
                $"session={state.OnlineState}; " +
                $"disableTerm={state.DisableAssistTerm?.ToString() ?? "unavailable"}; " +
                $"original={originalResult != 0}; final={finalResult != 0}");
        }
    }

    private void LogFailure(string message) =>
        _logger.WriteLine(
            $"[{_modId}] {message} Original behavior retained.",
            System.Drawing.Color.Red);

    private void LogOnlineStateFailure(string message) =>
        _logger.WriteLine(
            $"[{_modId}] {message} Online Quest state will be treated as unavailable.",
            System.Drawing.Color.Yellow);

    private void LogSignatureFailure(
        string name,
        string reason,
        bool required)
    {
        if (required)
        {
            LogFailure(
                $"{name} signature {reason}; hook not installed.");
            return;
        }

        LogOnlineStateFailure(
            $"{name} signature {reason}.");
    }

    public void Dispose()
    {
        lock (_installLock)
        {
            _gateHook?.Disable();
            _gateHook = null;
            _stateReader = null;
        }

        _signatureScanner.Dispose();
    }

    private readonly record struct DecisionLogKey(
        bool StateReadable,
        uint QuestId,
        byte AssistMode,
        bool? DisableAssistTerm,
        QuestOnlineState OnlineState,
        QuestTypeResolution TypeResolution,
        byte OriginalResult,
        byte FinalResult);
}
