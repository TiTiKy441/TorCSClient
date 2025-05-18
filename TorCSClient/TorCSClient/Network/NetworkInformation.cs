using System.Net.NetworkInformation;
using System.Net;

namespace TorCSClient.Network
{
    /// <summary>
    /// Contains useful network information
    /// </summary>
    public sealed class NetworkInformation
    {

        /// <summary>
        /// Finds main network interface through finding the best interface to the IPAddress.Any
        /// </summary>
        /// <returns>Best network interface</returns>
        public static NetworkInterface GetMainNetworkInterface()
        {
            // IPAddress.Any should always be ipv4?
            return IpHelpApiWrapper.GetBestInterface4(IPAddress.Any);
        }

        /// <summary>
        /// Find address of the gateway
        /// </summary>
        /// <returns>Gateway mac address</returns>
        /// <exception cref="AggregateException">Thrown if the gateway ip address was not found in table of all addresses and their associated physical addresses</exception>
        public static PhysicalAddress GetGatewayPhysicalAddress()
        {
            IPAddress targetAddress = GetGatewayIPAddress();
            PhysicalAddressRecord[] records = IpHelpApiWrapper.GetIpNetTableRecords(); // List of ip addresses and their physical addresses
            PhysicalAddress? found = Array.Find(records, x => x.IpAddress.Equals(targetAddress) && (x.NetType != IpNetType.Invalid))?.PhysicalAddress;
            if (found == null) throw new AggregateException("Gateway address was not found in the table");
            return found;
        }

        /// <summary>
        /// Gets main network interface ip address
        /// </summary>
        /// <returns>IP address of the main network interface</returns>
        public static IPAddress GetMainNetworkInterfaceIPAddress()
        {
            return GetMainNetworkInterface().GetIPProperties().UnicastAddresses.Last().Address;
        }

        /// <summary>
        /// Gets main network interface physical address
        /// </summary>
        /// <returns>Physical address of the main network interface</returns>
        public static PhysicalAddress GetMainNetworkInterfacePhysicalAddress()
        {
            return GetMainNetworkInterface().GetPhysicalAddress();
        }

        /// <summary>
        /// Gets device's gateway ip address
        /// </summary>
        /// <returns>Gateway ip address</returns>
        public static IPAddress GetGatewayIPAddress()
        {
            return GetMainNetworkInterface().GetIPProperties().GatewayAddresses.Last().Address;
        }

        /// <summary>
        /// Gets used DNS addresses
        /// </summary>
        /// <returns>DNS addresses</returns>
        public static IPAddress[] GetDNSAddresses()
        {
            return GetMainNetworkInterface().GetIPProperties().DnsAddresses.ToArray();
        }
    }
}
