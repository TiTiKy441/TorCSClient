using Microsoft.Win32;
using System.Net;
using System.Runtime.InteropServices;
using TorCSClient.Network.ProxiFyre;
using TorCSClient.Proxy;
using TorCSClient.Network.WinpkFilter;
using TorCSClient.Network;
using NdisApi;
using System.Net.NetworkInformation;

namespace TorCSClient.Listener
{

    /**
     * This class is basically holds everything together
     * 
     * It listens to the status changes of tor and adjusts firewall and other parameters accordingly to the configuration
     **/
    internal sealed class MainListener
    {

        public static bool IsEnabled { get; private set; } = false;

        // Used to set socks proxy
        [DllImport("wininet.dll")]
        private static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);
        private const int INTERNET_OPTION_SETTINGS_CHANGED = 39;
        private const int INTERNET_OPTION_REFRESH = 37;

        private const string userRoot = "HKEY_CURRENT_USER";
        private const string subkey = "Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings";
        private const string keyName = userRoot + "\\" + subkey;


        public static void Initialize()
        {
            Hook();
            if (Configuration.Instance.GetFlag("ConstantFirewall")) Firewall.Apply();
        }

        public static void Hook()
        {
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            TorService.Instance.OnStatusChange += TorService_OnStatusChange;
            ProxiFyreService.Instance.OnExit += ProxiFyre_OnExit;
            NetworkChange.NetworkAddressChanged += NetworkChange_NetworkAddressChanged;
            NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;
        }

        public static void Unhook()
        {
            AppDomain.CurrentDomain.ProcessExit -= CurrentDomain_ProcessExit;
            TorService.Instance.OnStatusChange -= TorService_OnStatusChange;
            ProxiFyreService.Instance.OnExit -= ProxiFyre_OnExit;
            NetworkChange.NetworkAddressChanged -= NetworkChange_NetworkAddressChanged;
            NetworkChange.NetworkAvailabilityChanged -= NetworkChange_NetworkAvailabilityChanged;
        }

        private static void NetworkChange_NetworkAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e)
        {
            Console.WriteLine("Network availability changed");
            SetupFirewall();
            if (IsEnabled)
            {
                EnableTor(false);
                EnableTor(true);
            }
        }

        private static void NetworkChange_NetworkAddressChanged(object? sender, EventArgs e)
        {
            Console.WriteLine("Network address changed");
            SetupFirewall();
            if (IsEnabled)
            {
                EnableTor(false);
                EnableTor(true);
            }
        }

        private static void ProxiFyre_OnExit(object? sender, EventArgs e)
        {
            // Since ProxiFyre uses ndisapi, if it's terminated ungracefully, it wil leave the network adapter in Tunneling state
            // By resetting the main adapter, we are making sure that it's okay
            Firewall.ResetMainAdapter();
        }

        private static void CurrentDomain_ProcessExit(object? sender, EventArgs e)
        {
            EnableTor(false);
            Utils.SetDNS(Configuration.Instance.Get("DefaultDNS").First());
            TorService.Instance.StopTor();
            Firewall.ResetMainAdapter();
            Unhook();
        }

        private static void TorService_OnStatusChange(object? sender, EventArgs e)
        {
            switch (TorService.Instance.Status)
            {
                case ProxyStatus.Disabled:
                    EnableTor(false);
                    Utils.SetDNS(Configuration.Instance.Get("DefaultDNS").First());
                    Firewall.SetPassAddresses(Array.Empty<IPAddress>());
                    break;

                case ProxyStatus.Starting:
                    EnableTor(false);
                    SetupFirewall();
                    break;

                case ProxyStatus.Running:
                    if (Configuration.Instance.GetFlag("StartEnabled")) EnableTor(true);
                    break;
            }
        }

        private static void SetupFirewall()
        {
            // We allow DNS requests so that when we get Tor's addresses in use, we can resolve hostnames
            // This is needed for webtunnel bridges
            List<IPAddress> passed = new();

            IPAddress[] dnsAddrs = CachedNetworkInformation.Shared.MainNetworkInterfaceIPProperties.DnsAddresses.ToArray();

            passed.AddRange(dnsAddrs);
            Firewall.SetPassAddresses(dnsAddrs);

            IPAddress[] usedAddresses = TorService.Instance.GetUsedAddresses(true, Configuration.Instance.GetInt("WebtunnelDNSQueryTimeout")).ToArray();

            passed.AddRange(usedAddresses);

            Firewall.SetPassAddresses(passed);
            Console.WriteLine("Firewall setup done");
        }

        public static bool EnableTor(bool enable)
        {
            if (enable)
            {
                //Utils.ReinitHttpClient("socks5://127.0.0.1:" + TorService.Instance.GetConfigurationValue("SocksPort").First());
                Utils.ReinitHttpClient("socks5://" + TorService.Instance.GetSocksEndPoint().ToString());
                
                if (Configuration.Instance.GetFlag("UseTorDNS")) Utils.SetDNS(IPAddress.Loopback.ToString());
                
                switch ((ProxificationType)Configuration.Instance.GetInt("NetworkFilterType"))
                {
                    case ProxificationType.SystemProxy:
                        EnableProxy(true);
                        if (!Firewall.IsApplied)
                        {
                            if (!Firewall.Apply()) return false;
                        }
                        break;
                    case ProxificationType.SelectedApps:
                        EnableProxy(false);
                        ProxiFyreService.Instance.Start();
                        break;
                    case ProxificationType.ProxifyreAll:
                        EnableProxy(false);
                        if (!Firewall.IsApplied)
                        {
                            if (!Firewall.Apply(addBaseFilters: false)) return false; 
                        }
                        ProxiFyreService.Instance.Start(true);
                        break;
                }
                IsEnabled = true;
                return true;
            }
            else
            {
                EnableProxy(false);
                if (Configuration.Instance.GetFlag("UseTorDNS")) Utils.SetDNS(Configuration.Instance.Get("DefaultDNS").First());
                Utils.ReinitHttpClient(null);

                ProxiFyreService.Instance.Stop();
                if (Firewall.IsApplied && !Configuration.Instance.GetFlag("ConstantFirewall"))
                { 
                    if (!Firewall.Stop()) return false; 
                }
                IsEnabled = false;
                return true;
            }
        }

        public static void EnableProxy(bool enable)
        {
            if (enable)
            {
                //Registry.SetValue(keyName, "ProxyServer", "socks=127.0.0.1:" + TorService.Instance.GetConfigurationValue("SocksPort").First());
                Registry.SetValue(keyName, "ProxyServer", "socks=" + TorService.Instance.GetSocksEndPoint().ToString());
                Registry.SetValue(keyName, "ProxyEnable", 1, RegistryValueKind.DWord);
                Registry.SetValue(keyName, "ProxyOverride", "");
            }
            else
            {
                Registry.SetValue(keyName, "ProxyEnable", 0, RegistryValueKind.DWord);
            }

            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);
        }
    }
}
