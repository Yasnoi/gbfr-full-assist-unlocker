using System.Buffers.Binary;
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
    private const string SignatureGroup = "granblue_fantasy_relink_er_2_0_2";
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

    private static readonly BytePattern GatePattern = BytePattern.Parse(GateSignature);
    private static readonly BytePattern AssistDisableHandlerPattern =
        BytePattern.Parse(AssistDisableHandlerSignature);
    private static readonly BytePattern OnlineQuestModePattern =
        BytePattern.Parse(OnlineQuestModeSignature);

    private readonly ILogger _logger;
    private readonly string _modId;
    private readonly IReloadedHooks _hooks;
    private readonly IRuntimeMemoryReader _memory;
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
        IRuntimeMemoryReader memory)
    {
        _logger = logger;
        _modId = modId;
        _config = config;
        _hooks = hooks;
        _memory = memory;

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

        scanManager.AddScan(GateScanName, SignatureGroup, address =>
        {
            _gateAddress = address;
            TryInstall();
        });
        scanManager.AddScan(
            AssistDisableHandlerScanName,
            SignatureGroup,
            address =>
            {
                _assistDisableTermHandlerAddress = address;
                TryInstall();
            });
        scanManager.AddScan(
            OnlineQuestModeScanName,
            SignatureGroup,
            address =>
            {
                _onlineQuestModeAddress = address;
                TryInitializeOnlineQuestModeState();
            });
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

            if (!ValidateOpcode(_gateAddress, GatePattern, GateScanName) ||
                !ValidateOpcode(
                    _assistDisableTermHandlerAddress,
                    AssistDisableHandlerPattern,
                    AssistDisableHandlerScanName))
            {
                return;
            }

            if (!TryResolveRipRelativeAddress(
                    _gateAddress + 4,
                    displacementOffset: 3,
                    instructionLength: 7,
                    out var questStateGlobalPointer) ||
                !TryResolveRipRelativeAddress(
                    _assistDisableTermHandlerAddress + 0x17,
                    displacementOffset: 3,
                    instructionLength: 7,
                    out var assistSelectionGlobalPointer) ||
                !TryResolveRelativeCallAddress(
                    _gateAddress + 0x10,
                    out var questIdGetterAddress))
            {
                LogFailure(
                    "Failed to resolve a verified state pointer or Quest ID getter.");
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
                    $"[{_modId}] Infinity Assist gate hook installed for ER 2.0.2.",
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
            var bytes = new byte[OnlineQuestModePattern.Length];
            if (!_memory.TryReadBytes(_onlineQuestModeAddress, bytes) ||
                !OnlineQuestModePattern.Matches(bytes))
            {
                LogOnlineStateFailure(
                    "OnlineQuestMode opcode validation failed.");
                return;
            }

            if (!TryResolveRipRelativeAddress(
                    _onlineQuestModeAddress + 0x7,
                    displacementOffset: 3,
                    instructionLength: 7,
                    out var globalPointer))
            {
                LogOnlineStateFailure(
                    "Failed to resolve the verified online Quest mode state pointer.");
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

    private bool ValidateOpcode(nint address, BytePattern pattern, string name)
    {
        var bytes = new byte[pattern.Length];
        if (!_memory.TryReadBytes(address, bytes) || !pattern.Matches(bytes))
        {
            LogFailure($"{name} opcode validation failed; hook not installed.");
            return false;
        }

        return true;
    }

    private bool TryResolveRipRelativeAddress(
        nint instructionAddress,
        int displacementOffset,
        int instructionLength,
        out nint target)
    {
        target = 0;
        Span<byte> displacementBytes = stackalloc byte[sizeof(int)];
        if (!_memory.TryReadBytes(
                instructionAddress + displacementOffset,
                displacementBytes))
        {
            return false;
        }

        var displacement = BinaryPrimitives.ReadInt32LittleEndian(displacementBytes);
        target = instructionAddress + instructionLength + displacement;
        return ReloadedRuntimeMemoryReader.IsLikelyPointer(target);
    }

    private bool TryResolveRelativeCallAddress(
        nint instructionAddress,
        out nint target)
    {
        target = 0;
        Span<byte> instruction = stackalloc byte[5];
        if (!_memory.TryReadBytes(instructionAddress, instruction) ||
            instruction[0] != 0xE8)
        {
            return false;
        }

        var displacement =
            BinaryPrimitives.ReadInt32LittleEndian(instruction[1..]);
        target = instructionAddress + instruction.Length + displacement;
        return ReloadedRuntimeMemoryReader.IsLikelyPointer(target);
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

    public void Dispose()
    {
        lock (_installLock)
        {
            _gateHook?.Disable();
            _gateHook = null;
            _stateReader = null;
        }
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
