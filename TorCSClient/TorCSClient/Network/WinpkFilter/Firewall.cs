using NdisApi;
using System.Net;
using static NdisApi.StaticFilter;

namespace TorCSClient.Network.WinpkFilter
{
    public sealed class Firewall
    {

        public static bool IsApplied { get; private set; } = false;

        public static Tuple<IPAddress, FILTER_PACKET_ACTION>[] IPAddresses { get; private set; } = [];

        private static readonly NdisApiDotNet _ndisapi = new(null);

        private static NetworkAdapter _adapter = GetActiveAdapter();

        public static FILTER_PACKET_ACTION GlobalAction { get; private set; } = FILTER_PACKET_ACTION.FILTER_PACKET_DROP;

        public static void CloseAndDisposeNdisapi()
        {
            _ndisapi.Dispose();
        }

        public static void SetPassAddresses(IEnumerable<IPAddress> addresses)
        {
            IPAddresses = addresses.Select(x => new Tuple<IPAddress, FILTER_PACKET_ACTION>(x, FILTER_PACKET_ACTION.FILTER_PACKET_PASS)).ToArray();
            if (IsApplied)
            {
                Stop();
                Apply(GlobalAction);
            }
        }

        public static NetworkAdapter GetActiveAdapter()
        {
            Tuple<bool, List<NetworkAdapter>> adapters = _ndisapi.GetTcpipBoundAdaptersInfo();
            if (!adapters.Item1) throw new InvalidOperationException();
            foreach (NetworkAdapter i in adapters.Item2)
            {
                if (CachedNetworkInformation.Shared.MainNetworkInterfacePhysicalAddress.Equals(i.CurrentAddress))
                {
                    return i;
                }
            }
            throw new InvalidOperationException();
        }
        
        public static bool Apply(FILTER_PACKET_ACTION globalAction = FILTER_PACKET_ACTION.FILTER_PACKET_DROP, bool addBaseFilters = true)
        {
            GlobalAction = globalAction;
            _adapter = GetActiveAdapter();
            List<StaticFilter> filters = [];
            foreach (Tuple<IPAddress, FILTER_PACKET_ACTION> address in IPAddresses)
            {
                filters.AddRange(CreateIpAddressStaticFilters(_adapter.Handle, address.Item1, address.Item2));
            }
            if (addBaseFilters) filters.AddRange(CreateBaseStaticFilters(_adapter.Handle, globalAction));
            //_ndisapi.ResetPacketFilterTable();
            bool success = true;
            success &= _ndisapi.SetPacketFilterTable(filters);
            success &= _ndisapi.SetAdapterMode(_adapter.Handle, MSTCP_FLAGS.MSTCP_FLAG_TUNNEL);
            IsApplied = success;
            if (!IsApplied)
            {
                _ndisapi.ResetPacketFilterTable();
                _ndisapi.SetAdapterMode(_adapter.Handle, 0);
            }
            return IsApplied;
        }

        public static bool Stop()
        {
            IsApplied = !(_ndisapi.ResetPacketFilterTable() & _ndisapi.SetAdapterMode(_adapter.Handle, 0));
            return !IsApplied;
        }

        public static bool ResetMainAdapter()
        {
            NetworkAdapter mainAdapter = GetActiveAdapter();
            return (_ndisapi.ResetPacketFilterTable() & _ndisapi.SetAdapterMode(mainAdapter.Handle, 0) & _ndisapi.SetPacketEvent(mainAdapter.Handle, null));
        }

        /// <summary>
        /// Creates base static filters, e.g arp pass filter + global action for all other packets
        /// </summary>
        /// <param name="adapterHandle">Target adapter handle</param>
        /// <param name="globalAction">Action for all other packets</param>
        /// <returns>List of static filters with length 2</returns>
        private static List<StaticFilter> CreateBaseStaticFilters(IntPtr adapterHandle, FILTER_PACKET_ACTION globalAction)
        {
            return new(2)
            {
                // ARP pass filter
                new(
                    adapterHandle,
                    PACKET_FLAG.PACKET_FLAG_ON_SEND_RECEIVE,
                    FILTER_PACKET_ACTION.FILTER_PACKET_PASS,
                    STATIC_FILTER_FIELDS.DATA_LINK_LAYER_VALID,
                    new
                    (
                        Eth802dot3Filter.ETH_802_3_FLAGS.ETH_802_3_PROTOCOL,
                        null,
                        null,
                        0x0806
                    ),
                null,
                null
                ),

                // Action for all uncaptured packets
                new StaticFilter(
                    adapterHandle,
                    PACKET_FLAG.PACKET_FLAG_ON_SEND_RECEIVE,
                    globalAction,
                    0,
                    null,
                    null,
                    null
                ),
            };
        }

        private static List<StaticFilter> CreateIpAddressStaticFilters(IntPtr adapterHandle, IPAddress destination, StaticFilter.FILTER_PACKET_ACTION action, PACKET_FLAG directionFlags = PACKET_FLAG.PACKET_FLAG_ON_SEND | PACKET_FLAG.PACKET_FLAG_ON_RECEIVE)
        {
            List<StaticFilter> staticFilters = new();
            IpNetRange destinationNetRange = new(IpNetRange.ADDRESS_TYPE.IP_RANGE_TYPE, destination, destination);

            if (directionFlags.HasFlag(PACKET_FLAG.PACKET_FLAG_ON_SEND))
                staticFilters.Add
                (
                new
                    (
                    adapterHandle: adapterHandle,
                    directionFlags: PACKET_FLAG.PACKET_FLAG_ON_SEND,
                    filterAction: action,
                    validFields: StaticFilter.STATIC_FILTER_FIELDS.NETWORK_LAYER_VALID,
                    null,
                    new
                        (
                        addressFamily: destination.AddressFamily,
                        validFields: IpAddressFilter.IP_FILTER_FIELDS.IP_FILTER_DEST_ADDRESS,
                        sourceAddress: null,
                        destinationAddress: destinationNetRange,
                        nextProtocol: 0
                        ),
                    null
                    )
                );

            if (directionFlags.HasFlag(PACKET_FLAG.PACKET_FLAG_ON_RECEIVE))
                staticFilters.Add
                (
                new
                    (
                    adapterHandle: adapterHandle,
                    directionFlags: PACKET_FLAG.PACKET_FLAG_ON_RECEIVE,
                    filterAction: action,
                    validFields: StaticFilter.STATIC_FILTER_FIELDS.NETWORK_LAYER_VALID,
                    null,
                    new
                        (
                        addressFamily: destination.AddressFamily,
                        validFields: IpAddressFilter.IP_FILTER_FIELDS.IP_FILTER_SRC_ADDRESS,
                        sourceAddress: destinationNetRange,
                        destinationAddress: null,
                        nextProtocol: 0
                        ),
                    null
                    )
                );
            return staticFilters;
        }
    }
}
