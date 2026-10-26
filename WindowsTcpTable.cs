using System.ComponentModel;
using System.Net;
using System.Runtime.InteropServices;

namespace OllamaTray;

internal static class WindowsTcpTable
{
    private const int AddressFamilyIpv4 = 2;
    private const int AddressFamilyIpv6 = 23;
    private const int TcpTableOwnerPidListener = 3;
    private const uint ErrorInsufficientBuffer = 122;
    private const int TcpStateListen = 2;
    private const int TcpRowIpv4Size = 24;
    private const int TcpRowIpv6Size = 56;
    private const int MaximumBufferAttempts = 3;

    public static IReadOnlyList<int> GetListeningProcessIds(
        int port,
        params IPAddress[] localAddresses)
    {
        var processIds = new HashSet<int>();
        ReadListeningProcessIds(
            AddressFamilyIpv4,
            TcpRowIpv4Size,
            port,
            localAddresses,
            processIds);
        ReadListeningProcessIds(
            AddressFamilyIpv6,
            TcpRowIpv6Size,
            port,
            localAddresses,
            processIds);
        return processIds.ToArray();
    }

    private static void ReadListeningProcessIds(
        int addressFamily,
        int rowSize,
        int port,
        IPAddress[] localAddresses,
        HashSet<int> processIds)
    {
        var table = ReadTable(addressFamily);
        try
        {
            var rowCount = Marshal.ReadInt32(table);
            for (var index = 0; index < rowCount; index++)
            {
                var row = IntPtr.Add(table, sizeof(int) + (index * rowSize));
                var isIpv4 = addressFamily == AddressFamilyIpv4;
                var localAddress = ReadLocalAddress(row, isIpv4);
                var localPortOffset = isIpv4 ? 8 : 20;
                var stateOffset = isIpv4 ? 0 : 48;
                var processIdOffset = isIpv4 ? 20 : 52;
                var state = Marshal.ReadInt32(row, stateOffset);
                var localPortValue = unchecked((ushort)Marshal.ReadInt32(row, localPortOffset));
                var localPort = unchecked((ushort)IPAddress.NetworkToHostOrder((short)localPortValue));
                var wildcardAddress = isIpv4 ? IPAddress.Any : IPAddress.IPv6Any;

                if (state == TcpStateListen &&
                    localPort == port &&
                    (localAddresses.Contains(wildcardAddress) ||
                     localAddresses.Contains(localAddress)))
                {
                    processIds.Add(Marshal.ReadInt32(row, processIdOffset));
                }
            }
        }
        finally
        {
            Marshal.FreeHGlobal(table);
        }
    }

    private static IPAddress ReadLocalAddress(IntPtr row, bool isIpv4)
    {
        if (isIpv4)
        {
            return new IPAddress(BitConverter.GetBytes(Marshal.ReadInt32(row, 4)));
        }

        var bytes = new byte[16];
        Marshal.Copy(row, bytes, 0, bytes.Length);
        return new IPAddress(bytes);
    }

    private static IntPtr ReadTable(int addressFamily)
    {
        var bufferSize = 0;
        var result = GetExtendedTcpTable(
            IntPtr.Zero,
            ref bufferSize,
            true,
            addressFamily,
            TcpTableOwnerPidListener,
            0);

        if (result != ErrorInsufficientBuffer)
        {
            throw new Win32Exception(
                (int)result,
                LocalizedText.Get("ErrorTcpTableSize"));
        }

        for (var attempt = 0; attempt < MaximumBufferAttempts; attempt++)
        {
            var table = Marshal.AllocHGlobal(bufferSize);
            result = GetExtendedTcpTable(
                table,
                ref bufferSize,
                true,
                addressFamily,
                TcpTableOwnerPidListener,
                0);

            if (result == 0)
            {
                return table;
            }

            Marshal.FreeHGlobal(table);
            if (result != ErrorInsufficientBuffer)
            {
                throw new Win32Exception(
                    (int)result,
                    LocalizedText.Get("ErrorTcpTableRead"));
            }
        }

        throw new InvalidOperationException(
            LocalizedText.Get("ErrorTcpTableChanged"));
    }

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern uint GetExtendedTcpTable(
        IntPtr tcpTable,
        ref int size,
        [MarshalAs(UnmanagedType.Bool)] bool order,
        int addressFamily,
        int tableClass,
        uint reserved);
}
