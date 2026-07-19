using GBFR.InfinityFullAssist.Runtime;
using GBFR.InfinityFullAssist.Core;

namespace GBFR.InfinityFullAssist.Tests;

public sealed class QuestRuntimeStateReaderTests
{
    private static readonly nint QuestGlobal = 0x100000;
    private static readonly nint QuestState = 0x200000;
    private static readonly nint AssistSelectionGlobal = 0x300000;
    private static readonly nint AssistSelection = 0x400000;
    private static readonly nint OnlineQuestModeGlobal = 0x500000;
    private static readonly nint OnlineQuestModeState = 0x600000;

    [Fact]
    public void ReadsQuestThroughGameGetterAndAssistModeFromCallerState()
    {
        var memory = new FakeRuntimeMemoryReader()
            .WithPointer(QuestGlobal, QuestState)
            .WithPointer(AssistSelectionGlobal, AssistSelection)
            .WithByte(
                AssistSelection + QuestRuntimeStateReader.AssistModeOffset,
                VerifiedInfinityData.FullAssistMode)
            .WithByte(
                QuestState + QuestRuntimeStateReader.DisableAssistTermOffset,
                1)
            .WithPointer(OnlineQuestModeGlobal, OnlineQuestModeState)
            .WithUInt32(
                OnlineQuestModeState +
                QuestRuntimeStateReader.OnlineQuestModeOffset,
                VerifiedInfinityData.OnlineQuestMode);
        var reader = new QuestRuntimeStateReader(
            memory,
            QuestGlobal,
            AssistSelectionGlobal,
            OnlineQuestModeGlobal,
            (nint state, out uint questId) =>
            {
                Assert.Equal(QuestState, state);
                questId = 0x40B301;
            });

        Assert.True(reader.TryRead(out var result));
        Assert.Equal(0x40B301u, result.QuestId);
        Assert.Equal(VerifiedInfinityData.FullAssistMode, result.AssistMode);
        Assert.True(result.DisableAssistTerm);
        Assert.Equal(QuestOnlineState.Online, result.OnlineState);
    }

    [Fact]
    public void MissingQuestContextFailsClosed()
    {
        var reader = new QuestRuntimeStateReader(
            new FakeRuntimeMemoryReader(),
            QuestGlobal,
            AssistSelectionGlobal,
            OnlineQuestModeGlobal,
            (nint _, out uint questId) => questId = 0x40B301);

        Assert.False(reader.TryRead(out _));
    }

    [Fact]
    public void DiagnosticDisableTermMayBeUnavailableWithoutInvalidatingGateState()
    {
        var memory = new FakeRuntimeMemoryReader()
            .WithPointer(QuestGlobal, QuestState)
            .WithPointer(AssistSelectionGlobal, AssistSelection)
            .WithByte(
                AssistSelection + QuestRuntimeStateReader.AssistModeOffset,
                VerifiedInfinityData.FullAssistMode);
        var reader = new QuestRuntimeStateReader(
            memory,
            QuestGlobal,
            AssistSelectionGlobal,
            OnlineQuestModeGlobal,
            (nint _, out uint questId) => questId = 0x40B316);

        Assert.True(reader.TryRead(out var result));
        Assert.Null(result.DisableAssistTerm);
        Assert.Equal(QuestOnlineState.Unknown, result.OnlineState);
    }

    [Fact]
    public void QuestGetterFailureFailsClosed()
    {
        var memory = new FakeRuntimeMemoryReader()
            .WithPointer(QuestGlobal, QuestState)
            .WithPointer(AssistSelectionGlobal, AssistSelection)
            .WithByte(
                AssistSelection + QuestRuntimeStateReader.AssistModeOffset,
                VerifiedInfinityData.FullAssistMode);
        var reader = new QuestRuntimeStateReader(
            memory,
            QuestGlobal,
            AssistSelectionGlobal,
            OnlineQuestModeGlobal,
            (nint _, out uint questId) =>
            {
                questId = 0;
                throw new InvalidOperationException();
            });

        Assert.False(reader.TryRead(out _));
    }

    [Fact]
    public void ReadableNonOnlineQuestModeIsOffline()
    {
        var memory = new FakeRuntimeMemoryReader()
            .WithPointer(QuestGlobal, QuestState)
            .WithPointer(AssistSelectionGlobal, AssistSelection)
            .WithByte(
                AssistSelection + QuestRuntimeStateReader.AssistModeOffset,
                VerifiedInfinityData.FullAssistMode)
            .WithPointer(OnlineQuestModeGlobal, OnlineQuestModeState)
            .WithUInt32(
                OnlineQuestModeState +
                QuestRuntimeStateReader.OnlineQuestModeOffset,
                0);
        var reader = new QuestRuntimeStateReader(
            memory,
            QuestGlobal,
            AssistSelectionGlobal,
            OnlineQuestModeGlobal,
            (nint _, out uint questId) => questId = 0x40B301);

        Assert.True(reader.TryRead(out var result));
        Assert.Equal(QuestOnlineState.Offline, result.OnlineState);
    }

    [Fact]
    public void OnlineQuestModePointerCanBeProvidedAfterConstruction()
    {
        var memory = new FakeRuntimeMemoryReader()
            .WithPointer(QuestGlobal, QuestState)
            .WithPointer(AssistSelectionGlobal, AssistSelection)
            .WithByte(
                AssistSelection + QuestRuntimeStateReader.AssistModeOffset,
                VerifiedInfinityData.FullAssistMode)
            .WithPointer(OnlineQuestModeGlobal, OnlineQuestModeState)
            .WithUInt32(
                OnlineQuestModeState +
                QuestRuntimeStateReader.OnlineQuestModeOffset,
                VerifiedInfinityData.OnlineQuestMode);
        var reader = new QuestRuntimeStateReader(
            memory,
            QuestGlobal,
            AssistSelectionGlobal,
            onlineQuestModeGlobalPointer: 0,
            (nint _, out uint questId) => questId = 0x40B301);

        Assert.True(reader.TryRead(out var beforeUpdate));
        Assert.Equal(QuestOnlineState.Unknown, beforeUpdate.OnlineState);

        reader.UpdateOnlineQuestModeGlobalPointer(OnlineQuestModeGlobal);

        Assert.True(reader.TryRead(out var afterUpdate));
        Assert.Equal(QuestOnlineState.Online, afterUpdate.OnlineState);
    }

    private sealed class FakeRuntimeMemoryReader : IRuntimeMemoryReader
    {
        private readonly Dictionary<nint, object> _values = [];

        public FakeRuntimeMemoryReader WithPointer(nint address, nint value)
        {
            _values[address] = value;
            return this;
        }

        public FakeRuntimeMemoryReader WithUInt32(nint address, uint value)
        {
            _values[address] = value;
            return this;
        }

        public FakeRuntimeMemoryReader WithByte(nint address, byte value)
        {
            _values[address] = value;
            return this;
        }

        public bool TryReadPointer(nint address, out nint value) =>
            TryRead(address, out value);

        public bool TryReadUInt32(nint address, out uint value) =>
            TryRead(address, out value);

        public bool TryReadByte(nint address, out byte value) =>
            TryRead(address, out value);

        public bool TryReadBytes(nint address, Span<byte> destination)
        {
            destination.Clear();
            return false;
        }

        private bool TryRead<T>(nint address, out T value)
            where T : struct
        {
            if (_values.TryGetValue(address, out var stored) && stored is T typed)
            {
                value = typed;
                return true;
            }

            value = default;
            return false;
        }
    }
}
