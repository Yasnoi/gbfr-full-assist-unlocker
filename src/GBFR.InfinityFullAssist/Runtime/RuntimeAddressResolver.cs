using System.Buffers.Binary;

namespace GBFR.InfinityFullAssist.Runtime;

internal static class RuntimeAddressResolver
{
    public static bool TryResolveRipRelativeAddress(
        IRuntimeMemoryReader memory,
        nint instructionAddress,
        int displacementOffset,
        int instructionLength,
        out nint target)
    {
        target = 0;
        Span<byte> displacementBytes = stackalloc byte[sizeof(int)];
        if (!memory.TryReadBytes(
                instructionAddress + displacementOffset,
                displacementBytes))
        {
            return false;
        }

        var displacement =
            BinaryPrimitives.ReadInt32LittleEndian(displacementBytes);
        target = instructionAddress + instructionLength + displacement;
        return true;
    }

    public static bool TryResolveRelativeCallAddress(
        IRuntimeMemoryReader memory,
        nint instructionAddress,
        out nint target)
    {
        target = 0;
        Span<byte> instruction = stackalloc byte[5];
        if (!memory.TryReadBytes(instructionAddress, instruction) ||
            instruction[0] != 0xE8)
        {
            return false;
        }

        var displacement =
            BinaryPrimitives.ReadInt32LittleEndian(instruction[1..]);
        target = instructionAddress + instruction.Length + displacement;
        return true;
    }
}
