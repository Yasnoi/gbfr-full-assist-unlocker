using System.Runtime.InteropServices;

namespace GBFR.InfinityFullAssist.Runtime;

internal sealed class WindowsRuntimeMemoryRegionSource :
    IRuntimeMemoryRegionSource
{
    private static readonly nint CurrentProcessPseudoHandle = (nint)(-1);

    public bool TryQuery(nint address, out VirtualMemoryRegion region)
    {
        region = default;
        var result = VirtualQuery(
            address,
            out var information,
            (nuint)Marshal.SizeOf<MemoryBasicInformation>());
        if (result == 0)
        {
            return false;
        }

        region = new VirtualMemoryRegion(
            information.BaseAddress,
            information.RegionSize,
            information.State,
            information.Protect);
        return true;
    }

    public bool TryRead(nint address, byte[] destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        return ReadProcessMemory(
                   CurrentProcessPseudoHandle,
                   address,
                   destination,
                   (nuint)destination.Length,
                   out var bytesRead) &&
               bytesRead == (nuint)destination.Length;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nuint VirtualQuery(
        nint address,
        out MemoryBasicInformation buffer,
        nuint length);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ReadProcessMemory(
        nint process,
        nint baseAddress,
        [Out] byte[] buffer,
        nuint size,
        out nuint bytesRead);

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryBasicInformation
    {
        public nint BaseAddress;
        public nint AllocationBase;
        public uint AllocationProtect;
        public ushort PartitionId;
        public nuint RegionSize;
        public uint State;
        public uint Protect;
        public uint Type;
    }
}
