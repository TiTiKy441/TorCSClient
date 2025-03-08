using Microsoft.Win32;
using System.Net;
using System.Runtime.InteropServices;
using TorCSClient.Network;
using TorCSClient.Network.ProxiFyre;
using TorCSClient.Proxy;
using TorCSClient.Proxy.Control;
using TorCSClient.Relays;
using WinNetworkUtilsCS.Network.WinpkFilter;
using NdisApi;

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

        private static NetworkPacketInterceptor _firewall;

        public static void Initialize()
        {
            Hook();
            _firewall = new NetworkIPFilter(Array.Empty<IPAddress>(), StaticFilter.FILTER_PACKET_ACTION.FILTER_PACKET_PASS, StaticFilter.FILTER_PACKET_ACTION.FILTER_PACKET_DROP);
            if (Configuration.Instance.GetFlag("ConstantFirewall")) _firewall.Start();
        }

        public static void Hook()
        {
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            TorService.Instance.OnStatusChange += TorService_OnStatusChange;
        }

        public static void Unhook()
        {
            AppDomain.CurrentDomain.ProcessExit -= CurrentDomain_ProcessExit;
            TorService.Instance.OnStatusChange -= TorService_OnStatusChange;
        }

        private static void CurrentDomain_ProcessExit(object? sender, EventArgs e)
        {
            EnableTor(false);
            ProxiFyreService.Instance.UpdateConfig();
            Utils.SetDNS(Configuration.Instance.Get("DefaultDNS").First());
            TorService.Instance.StopTor();
            TorService.Instance.WaitForEnd();
            TorService.KillTorProcess();
            TorService.Instance.OnStatusChange -= TorService_OnStatusChange;
            _firewall?.Dispose(); // ? just in case program exits before TorServiceListener is initialized
        }

        private static void TorService_OnStatusChange(object? sender, EventArgs e)
        {
            NetworkIPFilter ipFilter = (NetworkIPFilter)_firewall;
            switch (TorService.Instance.Status)
            {
                case ProxyStatus.Disabled:
                    EnableTor(false);
                    break;

                case ProxyStatus.Starting:
                    EnableTor(false);
                    // We allow DNS requests so that when we get Tor's addresses in use, we can resolve hostnames
                    // This is needed for webtunnel bridges
                    ipFilter.ChangeFilterParams(new IPAddress[] { Utils.GetDnsAddress() });
                    ipFilter.ChangeFilterParams(TorService.Instance.GetUsedAddresses(true).ToArray()); 
                    break;

                case ProxyStatus.Running:
                    ipFilter.ChangeFilterParams(TorService.Instance.GetUsedAddresses(true).ToArray());
                    if (Configuration.Instance.GetFlag("StartEnabled")) EnableTor(true);
                    break;
            }
        }

        public static bool EnableTor(bool enable)
        {
            if (enable)
            {
                if (Configuration.Instance.GetFlag("UseTorAsSystemProxy")) EnableProxy(true);
                if (Configuration.Instance.GetFlag("UseTorDNS")) Utils.SetDNS("127.0.0.1");
                Utils.ReinitHttpClient("socks5://127.0.0.1:" + TorService.Instance.GetConfigurationValue("SocksPort").First());

                if (Configuration.Instance.GetInt("NetworkFilterType") == 1) ProxiFyreService.Instance.Start();
                if (Configuration.Instance.GetInt("NetworkFilterType") == 0 && !_firewall.Capturing)
                {
                    if (!_firewall.Start()) return false;
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
                if (_firewall != null && _firewall.Capturing && !Configuration.Instance.GetFlag("ConstantFirewall") && !_firewall.Stop()) return false;
                IsEnabled = false;
                return true;
            }
        }


        public static void EnableProxy(bool enabled)
        {
            if (enabled)
            {
                Registry.SetValue(keyName, "ProxyServer", "socks=127.0.0.1:" + TorService.Instance.GetConfigurationValue("SocksPort").First());
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
