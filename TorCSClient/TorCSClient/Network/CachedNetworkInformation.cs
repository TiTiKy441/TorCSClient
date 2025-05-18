using System.Net.NetworkInformation;
using System.Net;

namespace TorCSClient.Network
{
    public sealed class CachedNetworkInformation
    {

        private bool _changeCachedMainNetworkInterface = false;

        private NetworkInterface _cachedMainNetworkInterface = NetworkInformation.GetMainNetworkInterface();

        public NetworkInterface MainNetworkInterface
        {
            get
            {
                if (_changeCachedMainNetworkInterface)
                {
                    _cachedMainNetworkInterface = NetworkInformation.GetMainNetworkInterface();
                    _changeCachedMainNetworkInterface = false;
                }
                return _cachedMainNetworkInterface;
            }
        }

        private bool _changeCachedGatewayPhysicalAddress = false;

        private PhysicalAddress? _cachedGatewayPhysicalAddress = null;

        public PhysicalAddress GatewayPhysicalAddress
        {
            get
            {
                if (_changeCachedGatewayPhysicalAddress || (_cachedGatewayPhysicalAddress == null))
                {
                    _cachedGatewayPhysicalAddress = NetworkInformation.GetGatewayPhysicalAddress();
                    _changeCachedGatewayPhysicalAddress = false;
                }
                return _cachedGatewayPhysicalAddress;
            }
        }

        private bool _changeCachedMainNetworkInterfaceIPProperties = false;

        private IPInterfaceProperties? _cachedMainNetworkInterfaceIPProperties = null;

        public IPInterfaceProperties MainNetworkInterfaceIPProperties
        {
            get
            {
                if (_changeCachedMainNetworkInterfaceIPProperties || (_cachedMainNetworkInterfaceIPProperties == null))
                {
                    _cachedMainNetworkInterfaceIPProperties = MainNetworkInterface.GetIPProperties();
                    _changeCachedMainNetworkInterfaceIPProperties = false;
                }
                return _cachedMainNetworkInterfaceIPProperties;
            }
        }


        private bool _changeCachedMainNetworkInterfaceIPAddress = false;

        private IPAddress? _cachedMainNetworkInterfaceIPAddress = null;

        public IPAddress MainNetworkInterfaceIPAddress
        {
            get
            {
                if (_changeCachedMainNetworkInterfaceIPAddress || (_cachedMainNetworkInterfaceIPAddress == null))
                {
                    _cachedMainNetworkInterfaceIPAddress = MainNetworkInterface.GetIPProperties().UnicastAddresses.Last().Address;
                    _changeCachedMainNetworkInterfaceIPAddress = false;
                }
                return _cachedMainNetworkInterfaceIPAddress;
            }
        }

        private bool _changeCachedMainNetworkInterfacePhysicalAddress = false;

        private PhysicalAddress? _cachedMainNetworkInterfacePhysicalAddress = null;

        public PhysicalAddress MainNetworkInterfacePhysicalAddress
        {
            get
            {
                if (_changeCachedMainNetworkInterfacePhysicalAddress || (_cachedMainNetworkInterfacePhysicalAddress == null))
                {
                    _cachedMainNetworkInterfacePhysicalAddress = MainNetworkInterface.GetPhysicalAddress();
                    _changeCachedMainNetworkInterfacePhysicalAddress = false;
                }
                return _cachedMainNetworkInterfacePhysicalAddress;
            }
        }

        public bool _changeCachedGatewayIPAddress = false;

        private IPAddress? _cachedGatewayIPAddress = null;

        public IPAddress GatewayIPAddress
        {
            get
            {
                if (_changeCachedGatewayIPAddress || (_cachedGatewayIPAddress == null))
                {
                    _cachedGatewayIPAddress = MainNetworkInterface.GetIPProperties().GatewayAddresses.Last().Address;
                    _changeCachedGatewayIPAddress = false;
                }
                return _cachedGatewayIPAddress;
            }
        }

        /// <summary>
        /// Shared instance of cached network information
        /// </summary>
        public static readonly CachedNetworkInformation Shared = new();

        public CachedNetworkInformation()
        {
            NetworkChange.NetworkAddressChanged += NetworkChange_NetworkAddressChanged;
            NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;
        }

        private void NetworkChange_NetworkAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e)
        {
            _changeCachedGatewayIPAddress = true;
            _changeCachedGatewayPhysicalAddress = true;
            _changeCachedMainNetworkInterface = true;
            _changeCachedMainNetworkInterfaceIPAddress = true;
            _changeCachedMainNetworkInterfacePhysicalAddress = true;
            _changeCachedMainNetworkInterfaceIPProperties = true;
        }

        private void NetworkChange_NetworkAddressChanged(object? sender, EventArgs e)
        {
            _changeCachedMainNetworkInterface = true;
            _changeCachedMainNetworkInterfaceIPAddress = true;
            _changeCachedMainNetworkInterfacePhysicalAddress = true;
            _changeCachedMainNetworkInterfaceIPProperties = true;
        }

        ~CachedNetworkInformation()
        {
            NetworkChange.NetworkAddressChanged -= NetworkChange_NetworkAddressChanged;
            NetworkChange.NetworkAvailabilityChanged -= NetworkChange_NetworkAvailabilityChanged;
        }
    }
}
