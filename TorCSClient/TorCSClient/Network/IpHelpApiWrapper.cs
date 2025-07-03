using System.ComponentModel;
using System.Net.Sockets;
using System.Net;
using System.Runtime.InteropServices;
using System.Net.NetworkInformation;
using System.Buffers;

namespace TorCSClient.Network
{
    public sealed class IpHelpApiWrapper
    {

        public const string LibraryName = "iphlpapi.dll";


        #region Native functions

        [DllImport(LibraryName, CharSet = CharSet.Auto, ExactSpelling = true, SetLastError = true)]
        private static extern uint GetBestInterface(uint dwDestAddr, ref uint pdwBestIfIndex);


        //
        [DllImport(LibraryName, CharSet = CharSet.Auto, ExactSpelling = true, SetLastError = true)]
        private static extern uint GetBestInterfaceEx(ref byte dwDestAddr, ref uint pdwBestIfIndex);
        //


        [DllImport(LibraryName, CharSet = CharSet.Auto, ExactSpelling = true, SetLastError = true)]
        private static extern uint GetBestInterfaceEx(IntPtr dwDestAddr, ref uint pdwBestIfIndex);

        [DllImport(LibraryName, CharSet = CharSet.Auto, ExactSpelling = true, SetLastError = true)]
        private static extern uint SendARP(uint DestIP, uint SrcIP, byte[] pMacAddr, ref int PhyAddrLen);


        //
        [DllImport(LibraryName, CharSet = CharSet.Auto, ExactSpelling = true, SetLastError = true)]
        private static extern uint GetIpNetTable(ref byte pIpNetTable, ref int pdwSize, bool bOrder);
        //
        #endregion

        public const int DefaultBufferSize = 16 * 1024;

        #region GetBestInterfaceIndex()

        public static uint GetBestInterfaceIndex(uint address)
        {
            uint index = 0;
            uint errorCode = GetBestInterface(address, ref index);
            HandleErrorCode(errorCode);
            return index;
        }

        public static uint GetBestInterfaceIndex(IPAddress address)
        {
            return GetBestInterfaceIndex(GetIPV4AddressUint(address));
        }

        #endregion

        #region GetBestInterface4()

        public static NetworkInterface GetBestInterface4(IPAddress address, NetworkInterface[]? interfaces = null)
        {
            // Use GetBestInterfaceIndex instead of GetBestInterfaceEx because it's faster (BARELY!) and doesnt allocate memory on the heap
            uint bestIndex = GetBestInterfaceIndex(address);
            interfaces ??= NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface netInterface in interfaces)
            {
                if (netInterface.GetIPProperties().GetIPv4Properties().Index == bestIndex) return netInterface;
            }
            throw new InvalidOperationException("Unable to find the best interface");
        }

        #endregion

        #region GetBestInterfaceIndexEx()

        public static uint GetBestInterfaceIndexEx(IPAddress address)
        {
            Span<byte> byteSpan = stackalloc byte[26];

            byteSpan[0] = (byte)(((short)address.AddressFamily) & 255);
            byteSpan[1] = (byte)(((short)address.AddressFamily) >> 8);

            if (!address.TryWriteBytes(byteSpan[2..], out int _)) throw new InvalidDataException("Unable to get ip address bytes");

            uint index = 0;
            uint errorCode = GetBestInterfaceEx(ref byteSpan.GetPinnableReference(), ref index);
            HandleErrorCode(errorCode);
            return index;
        }

        #endregion

        #region GetBestInterface6()

        public static NetworkInterface GetBestInterface6(IPAddress address, NetworkInterface[]? interfaces = null)
        {
            uint bestIndex = GetBestInterfaceIndexEx(address);
            interfaces ??= NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface netInterface in interfaces)
            {
                if (netInterface.GetIPProperties().GetIPv6Properties().Index == bestIndex) return netInterface;
            }
            throw new InvalidOperationException("Unable to find the best interface");
        }

        #endregion

        #region GetBestInterface()

        public static NetworkInterface GetBestInterface(IPAddress address, NetworkInterface[]? interfaces = null)
        {
            return address.AddressFamily switch
            {
                AddressFamily.InterNetwork => GetBestInterface4(address, interfaces),
                AddressFamily.InterNetworkV6 => GetBestInterface6(address, interfaces),
                _ => throw new ArgumentException("AddressFamily should be InterNetwork or InterNetworkV6"),
            };
        }

        #endregion

        #region SendARP()

        public static byte[] SendARP(uint destionationAddress, uint sourceAddress = 0, int physicalAddressSize = 6)
        {
            byte[] physicalAddress = new byte[physicalAddressSize];
            int addressLength = physicalAddress.Length;
            uint errorCode = SendARP(destionationAddress, sourceAddress, physicalAddress, ref addressLength);
            HandleErrorCode(errorCode);
            return physicalAddress;
        }

        /// <summary>
        /// Send Address Resolution Protocol (ARP) packet to resolve the physical address of the device with the desired ipv4 address in the local network.
        /// If the ARP entry exists in the ARP table on the local device, returns the target address, otherwise sends resolution packet.
        /// IMPORTANT: Supports only IPv4
        /// </summary>
        /// <param name="destAddress">Ip address to be resolved</param>
        /// <param name="srcAddress">Network interface ip address, if set to null, would automatically resolve from the main interface</param>
        /// <returns>Resolved physical address</returns>
        public static PhysicalAddress SendARP(IPAddress destAddress, IPAddress? srcAddress = null, int physicalAddressSize = 6)
        {
            // Do not check if addresses are IPv4 since GetIPV4AddressUint would throw an exception if not IPv4 address was passed to it
            uint destionationAddress = GetIPV4AddressUint(destAddress);
            uint sourceAddress = ((srcAddress == null) ? 0 : GetIPV4AddressUint(srcAddress));

            return new PhysicalAddress(SendARP(destionationAddress, sourceAddress, physicalAddressSize));
        }

        #endregion

        /// <summary>
        /// Returns ipv4 address in uint format
        /// </summary>
        /// <param name="ipAddr">Address to convert</param>
        /// <returns>Address in uint format</returns>
        /// <exception cref="InvalidOperationException">Provided ip address is not ipv4</exception>
        /// <exception cref="InvalidDataException">Unable to write ip address bytes</exception>
        public static uint GetIPV4AddressUint(IPAddress ipAddr)
        {
            if (ipAddr.AddressFamily is not AddressFamily.InterNetwork) throw new InvalidOperationException("GetIPV4Adress supports only ipv4 addresses");
            Span<byte> addrBytes = stackalloc byte[4];
            if (!ipAddr.TryWriteBytes(addrBytes, out int _)) throw new InvalidDataException("Unable to get ip address bytes");
            return ((uint)addrBytes[3] << 24) + ((uint)addrBytes[2] << 16) + ((uint)addrBytes[1] << 8) + addrBytes[0];
        }

        private static void HandleErrorCode(uint errorCode)
        {
            if (errorCode == (uint)ErrorReturnCodes.ERROR_INSUFFICIENT_BUFFER) throw new OutOfMemoryException("Buffer is too small");
            if ((errorCode != (uint)ErrorReturnCodes.NO_ERROR) && (errorCode != (int)ErrorReturnCodes.ERROR_NO_DATA)) throw new Win32Exception((int)errorCode);
        }

        public static PhysicalAddressRecord[] GetIpNetTableRecords(bool sortedOrder = false, int bufferSize = DefaultBufferSize)
        {
            if ((bufferSize > (1023 * 1024)) || (bufferSize < 1)) throw new ArgumentOutOfRangeException(nameof(bufferSize), "bufferSize must be [1; 1047552]");

            Span<byte> buffer = stackalloc byte[bufferSize];

            uint errorCode = GetIpNetTable(ref buffer.GetPinnableReference(), ref bufferSize, sortedOrder);

            HandleErrorCode(errorCode);

            return CreatePhysicalAddressRecordArrayFromBuffer(bufferSize, buffer);
        }

        private static PhysicalAddressRecord[] CreatePhysicalAddressRecordArrayFromBuffer(int allocatedSize, Span<byte> buffer)
        {
            int singleSize = 24;
            int num = (int)BitConverter.ToUInt32(buffer);
            PhysicalAddressRecord[] records = new PhysicalAddressRecord[num];
            for (int k = 0, i = 4; k < num; k++)
            {
                records[k] = new
                    (
                        physicalAddress: buffer[(i + 8)..(i + 14)].ToArray(),
                        ipAddress: SpanBitConverter.ToUInt32(buffer, i + 16),
                        netType: (IpNetType)SpanBitConverter.ToUInt32(buffer, i + 20)
                    );
                i += singleSize;
            }
            return records;
        }
    }


    public enum ErrorReturnCodes : uint
    {
        NO_ERROR = 0,
        ERROR_INSUFFICIENT_BUFFER = 122,
        ERROR_INVALID_PARAMETER = 87,
        ERROR_NO_DATA = 238,
    }

    public enum IpNetType
    {
        Other = 1,
        Invalid = 2,
        Dynamic = 3,
        Static = 4,
    }
    public sealed class PhysicalAddressRecord
    {

        public readonly uint IpAddressInt;

        private IPAddress? _ipAddress;

        public IPAddress IpAddress
        {
            get
            {
                _ipAddress ??= new(IpAddressInt);
                return _ipAddress;
            }
        }

        public readonly byte[] PhysicalAddressBytes;

        private PhysicalAddress? _physicalAddress;

        public PhysicalAddress PhysicalAddress
        {
            get
            {
                _physicalAddress ??= new(PhysicalAddressBytes);
                return _physicalAddress;
            }
        }

        public readonly IpNetType NetType;

        public PhysicalAddressRecord(uint ipAddress, byte[] physicalAddress, IpNetType netType)
        {
            IpAddressInt = ipAddress;
            PhysicalAddressBytes = physicalAddress;
            NetType = netType;
        }
    }

    /// <summary>
    /// Provides span bit conversions with indexes (BitConverter doesnt support indexes)
    /// I am not sure how should it be done properly, maybe extension?
    /// </summary>
    internal sealed class SpanBitConverter
    {

        public static int ToInt32<TFrom>(Span<TFrom> span, int index = 0)
            where TFrom : struct
        {
            return MemoryMarshal.Cast<TFrom, int>(span.Slice(index, sizeof(Int32)))[0];//CastToSingle<TFrom, Int32>(span, sizeof(Int32), index);
        }

        public static long ToInt64<TFrom>(Span<TFrom> span, int index = 0)
            where TFrom : struct
        {
            return MemoryMarshal.Cast<TFrom, long>(span.Slice(index, sizeof(Int64)))[0];//CastToSingle<TFrom, Int64>(span, sizeof(Int64), index);
        }

        public static uint ToUInt32<TFrom>(Span<TFrom> span, int index = 0)
            where TFrom : struct
        {
            return MemoryMarshal.Cast<TFrom, uint>(span.Slice(index, sizeof(UInt32)))[0];//CastToSingle<TFrom, UInt32>(span, sizeof(UInt32), index);
        }

        public static TTo CastToSingle<TFrom, TTo>(Span<TFrom> span, int size, int index = 0)
            where TFrom : struct
            where TTo : struct
        {
            return MemoryMarshal.Cast<TFrom, TTo>(span.Slice(index, size))[0];
        }
    }
}
