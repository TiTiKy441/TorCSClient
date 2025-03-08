using NdisApi;
using System.Net;

namespace WinNetworkUtilsCS.Network.WinpkFilter
{
    public class NetworkIPFilter : NetworkPacketInterceptor
    {

        public IPAddress[] FilteredAddresses { get; private set; }

        private List<StaticFilter> _staticFilters;
        
        public static readonly new MSTCP_FLAGS Mode = MSTCP_FLAGS.MSTCP_FLAG_TUNNEL;

        public StaticFilter.FILTER_PACKET_ACTION FilterAction { get; private set; }

        public StaticFilter.FILTER_PACKET_ACTION GlobalAction { get; private set; }

        /**
         * NetworkIPFilters - reforwards all traffic going from or going to the specific ip
         * 
         * If filterAction set to FILTER_PACKET_REDIRECT, redirects all traffic to HandlePackets() or HandlePacket()
         * 
         * Default exceptions: ARP always passed
         **/
        public NetworkIPFilter(IPAddress[] filteredIP,
            StaticFilter.FILTER_PACKET_ACTION filterAction = StaticFilter.FILTER_PACKET_ACTION.FILTER_PACKET_REDIRECT,
            StaticFilter.FILTER_PACKET_ACTION globalAction = StaticFilter.FILTER_PACKET_ACTION.FILTER_PACKET_PASS,
            bool singlePacketHandle = false) : base(Mode, singlePacketHandle: singlePacketHandle)
        {
            FilteredAddresses = filteredIP;

            FilterAction = filterAction;
            GlobalAction = globalAction;

            List<StaticFilter> filters = new();

            foreach (IPAddress bps in FilteredAddresses)
            {
                filters.AddRange(CreateIpAddressStaticFilters(bps, filterAction));
            }
            filters.AddRange(CreateBaseStaticFilters(GlobalAction));
            _staticFilters = filters;
        }

        private List<StaticFilter> CreateBaseStaticFilters(StaticFilter.FILTER_PACKET_ACTION globalAction)
        {
            List<StaticFilter> filters = new(2)
            {

                // ARP PASS filter
                new(
                _adapter.Handle,
                PACKET_FLAG.PACKET_FLAG_ON_SEND_RECEIVE,
                StaticFilter.FILTER_PACKET_ACTION.FILTER_PACKET_PASS,
                StaticFilter.STATIC_FILTER_FIELDS.DATA_LINK_LAYER_VALID,
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
                _adapter.Handle,
                PACKET_FLAG.PACKET_FLAG_ON_SEND_RECEIVE,
                globalAction,
                0,
                null,
                null,
                null
                ),
            };

            //filters.AddRange(CreateIpAddressStaticFilters(IPAddress.Parse("0.0.0.0"), StaticFilter.FILTER_PACKET_ACTION.FILTER_PACKET_PASS));

            return filters;
        }

        private List<StaticFilter> CreateIpAddressStaticFilters(IPAddress destination, StaticFilter.FILTER_PACKET_ACTION action)
        {
            List<StaticFilter> staticFilters = new();
            IpNetRange destinationNetRange = new(IpNetRange.ADDRESS_TYPE.IP_RANGE_TYPE, destination, destination);

            staticFilters.Add
                (
                new
                    (
                    adapterHandle: _adapter.Handle,
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

            staticFilters.Add
                (
                new
                    (
                    adapterHandle: _adapter.Handle,
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

        public bool ChangeFilterParams(IPAddress[]? newAddresses = null, StaticFilter.FILTER_PACKET_ACTION? newFilterAction = null, StaticFilter.FILTER_PACKET_ACTION? newGlobalAction = null)
        {
            ThrowIfDisposed();
            FilteredAddresses = newAddresses ?? FilteredAddresses;
            FilterAction = newFilterAction ?? FilterAction;
            GlobalAction = newGlobalAction ?? GlobalAction;

            List<StaticFilter> filters = new();
            foreach (IPAddress addr in FilteredAddresses)
            {
                filters.AddRange(CreateIpAddressStaticFilters(addr, FilterAction));
            }
            filters.AddRange(CreateBaseStaticFilters(GlobalAction));
            _staticFilters = filters;

            if (!Capturing) return true;

            return _ndisapi.ResetPacketFilterTable() & _ndisapi.SetPacketFilterTable(_staticFilters);
        }

        public override bool Start()
        {
            ThrowIfDisposed();
            return _ndisapi.SetPacketFilterTable(_staticFilters) & base.Start();
        }

        public override bool Stop()
        {
            ThrowIfDisposed();
            return _ndisapi.ResetPacketFilterTable() & base.Stop();
        }

        public override void Dispose()
        {
            ThrowIfDisposed();
            _ndisapi.ResetPacketFilterTable();
            base.Dispose();
        }
    }
}
