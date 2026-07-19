using System.Buffers.Binary;
using GBFR.InfinityFullAssist.Runtime;

namespace GBFR.InfinityFullAssist.Tests;

public sealed class RuntimeAddressResolverTests
{
    [Fact]
    public void ResolvesRipRelativeTarget()
    {
        var memory = new ByteMemoryReader();
        var instruction = (nint)0x10001000;
        memory.WithInt32(instruction + 3, 0x250);

        Assert.True(RuntimeAddressResolver.TryResolveRipRelativeAddress(
            memory,
            instruction,
            displacementOffset: 3,
            instructionLength: 7,
            out var target));
        Assert.Equal(instruction + 7 + 0x250, target);
    }

    [Fact]
    public void ResolvesRelativeCallTarget()
    {
        var memory = new ByteMemoryReader();
        var instruction = (nint)0x10001000;
        memory.WithBytes(instruction, [0xE8, 0x20, 0x00, 0x00, 0x00]);

        Assert.True(RuntimeAddressResolver.TryResolveRelativeCallAddress(
            memory,
            instruction,
            out var target));
        Assert.Equal(instruction + 5 + 0x20, target);
    }

    [Fact]
    public void RejectsNonCallOpcode()
    {
        var memory = new ByteMemoryReader();
        var instruction = (nint)0x10001000;
        memory.WithBytes(instruction, [0xE9, 0x20, 0x00, 0x00, 0x00]);

        Assert.False(RuntimeAddressResolver.TryResolveRelativeCallAddress(
            memory,
            instruction,
            out _));
    }

    [Fact]
    public void ResolvedTargetOutsideModuleIsRejectedByRange()
    {
        var memory = new ByteMemoryReader();
        var module = new ModuleAddressRange(0x10000000, 0x1000);
        var instruction = (nint)0x10000800;
        memory.WithBytes(instruction, [0xE8, 0x00, 0x10, 0x00, 0x00]);

        Assert.True(RuntimeAddressResolver.TryResolveRelativeCallAddress(
            memory,
            instruction,
            out var target));
        Assert.False(module.Contains(target));
    }

    private sealed class ByteMemoryReader : IRuntimeMemoryReader
    {
        private readonly Dictionary<nint, byte> _bytes = [];

        public ByteMemoryReader WithInt32(nint address, int value)
        {
            Span<byte> bytes = stackalloc byte[sizeof(int)];
            BinaryPrimitives.WriteInt32LittleEndian(bytes, value);
            return WithBytes(address, bytes);
        }

        public ByteMemoryReader WithBytes(
            nint address,
            ReadOnlySpan<byte> bytes)
        {
            for (var index = 0; index < bytes.Length; index++)
            {
                _bytes[address + index] = bytes[index];
            }

            return this;
        }

        public bool TryReadPointer(nint address, out nint value)
        {
            value = 0;
            return false;
        }

        public bool TryReadUInt32(nint address, out uint value)
        {
            value = 0;
            return false;
        }

        public bool TryReadByte(nint address, out byte value) =>
            _bytes.TryGetValue(address, out value);

        public bool TryReadBytes(nint address, Span<byte> destination)
        {
            for (var index = 0; index < destination.Length; index++)
            {
                if (!_bytes.TryGetValue(address + index, out destination[index]))
                {
                    destination.Clear();
                    return false;
                }
            }

            return true;
        }
    }
}
