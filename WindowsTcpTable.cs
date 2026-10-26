using System.ComponentModel;
using System.Net;
using System.Runtime.InteropServices;

namespace OllamaTray;

internal static class WindowsTcpTable
{
    private const int AddressFamilyIpv4 = 2;
    private const int TcpTableOwnerPidListener = 3;
    private const uint ErrorInsufficientBuffer = 122;
    private const int TcpStateListen = 2;
    private const int TcpRowSize = 24;
    private const int MaximumBufferAttempts = 3;

    public static IReadOnlyList<int> GetListeningProcessIds(
        int port,
        params IPAddress[] localAddresses)
    {
        var table = ReadTable();
        try
        {
            var rowCount = Marshal.ReadInt32(table);
            var processIds = new HashSet<int>();

            for (var index = 0; index < rowCount; index++)
            {
                var row = IntPtr.Add(table, sizeof(int) + (index * TcpRowSize));
                var state = Marshal.ReadInt32(row);
                var localAddress = new IPAddress(BitConverter.GetBytes(Marshal.ReadInt32(row, 4)));
                var localPortValue = unchecked((ushort)Marshal.ReadInt32(row, 8));
                var localPort = unchecked((ushort)IPAddress.NetworkToHostOrder((short)localPortValue));

                if (state != TcpStateListen ||
                    localPort != port ||
                    !localAddresses.Contains(localAddress))
                {
                    continue;
                }

                processIds.Add(Marshal.ReadInt32(row, 20));
            }

            return processIds.ToArray();
        }
        finally
        {
            Marshal.FreeHGlobal(table);
        }
    }

    private static IntPtr ReadTable()
    {
        var bufferSize = 0;
        var result = GetExtendedTcpTable(
            IntPtr.Zero,
            ref bufferSize,
            true,
            AddressFamilyIpv4,
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
                AddressFamilyIpv4,
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
