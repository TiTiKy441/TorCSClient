using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.RegularExpressions;
using TorCSClient.Network;

namespace TorCSClient
{
    internal sealed class Utils
    {

        private static HttpClient _httpClient = new HttpClient();

        public readonly static Regex Ipv4AddressSelector = new("(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)");
        public readonly static Regex Ipv6AddressSelector = new("(([0-9a-fA-F]{1,4}:){7,7}[0-9a-fA-F]{1,4}|([0-9a-fA-F]{1,4}:){1,7}:|([0-9a-fA-F]{1,4}:){1,6}:[0-9a-fA-F]{1,4}|([0-9a-fA-F]{1,4}:){1,5}(:[0-9a-fA-F]{1,4}){1,2}|([0-9a-fA-F]{1,4}:){1,4}(:[0-9a-fA-F]{1,4}){1,3}|([0-9a-fA-F]{1,4}:){1,3}(:[0-9a-fA-F]{1,4}){1,4}|([0-9a-fA-F]{1,4}:){1,2}(:[0-9a-fA-F]{1,4}){1,5}|[0-9a-fA-F]{1,4}:((:[0-9a-fA-F]{1,4}){1,6})|:((:[0-9a-fA-F]{1,4}){1,7}|:)|fe80:(:[0-9a-fA-F]{0,4}){0,4}%[0-9a-zA-Z]{1,}|::(ffff(:0{1,4}){0,1}:){0,1}((25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9])\\.){3,3}(25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9])|([0-9a-fA-F]{1,4}:){1,4}:((25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9])\\.){3,3}(25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9]))");
        public readonly static Regex SquareBracketsSelector = new(@"(?<=\[)[^][]*(?=])");
        public readonly static Regex UrlSelector = new(@"(www.+|http.+)([\s]|$)");
        public readonly static Regex PercentageSelector = new("(\\d+(\\.\\d+)?%)");

        public readonly static Random Random = new(Convert.ToInt32(DateTime.Now.ToString("FFFFFFF")));

        private static NetworkInterface? _cachedMainNetworkInterface;
        private static ManagementObject? _cachedDnsMO;

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;

        public static void HideConsole()
        {
            ShowWindow(GetConsoleWindow(), SW_HIDE);
        }

        public static void ShowConsole()
        {
            ShowWindow(GetConsoleWindow(), SW_SHOW);
        }


        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AllocConsole();

        public static string Download(string url)
        {
            return _httpClient.Send(new HttpRequestMessage(HttpMethod.Get, url)).Content.ReadAsStringAsync().Result;
        }

        public static void ReinitHttpClient(string? proxy = null)
        {
            _httpClient.Dispose();
            if (proxy == null)
            {
                _httpClient = new HttpClient()
                {
                    Timeout = _httpClient.Timeout,
                };
                return;
            }
            Uri uri = new(proxy);
            string[] creds = uri.UserInfo.Split(':', 2);
            WebProxy webProxy = new(uri);
            if (proxy.Contains('@'))
            {
                webProxy.Credentials = new NetworkCredential(creds[0], creds[1]);
                webProxy.UseDefaultCredentials = false;
            }
            HttpClientHandler handler = new()
            {
                Proxy = webProxy,
                UseProxy = true,
            };
            _httpClient = new HttpClient(handler)
            {
                Timeout = _httpClient.Timeout,
            };
        }

        public static void DownloadToFile(string url, string fileName)
        {
            using (Stream s = _httpClient.GetStreamAsync(url).Result)
            {
                using (FileStream fs = new(fileName, FileMode.OpenOrCreate))
                {
                    s.CopyTo(fs);
                }
            }
        }

        /**
        public async static Task DownloadToFileAsync(string url, string fileName)
        {
            using Stream s = await _httpClient.GetStreamAsync(url);
            using FileStream fs = new(fileName, FileMode.OpenOrCreate);
            await s.CopyToAsync(fs);
        }
        **/

        public static void SetDNS(string newDNSServer)
        {
            string[] Dns = { newDNSServer, newDNSServer, newDNSServer };
            NetworkInterface CurrentInterface = CachedNetworkInformation.Shared.MainNetworkInterface;
            if (CurrentInterface == null) return;

            ManagementBaseObject objdns;

            if (_cachedDnsMO != null)
            {
                objdns = _cachedDnsMO.GetMethodParameters("SetDNSServerSearchOrder");
                objdns["DNSServerSearchOrder"] = Dns;
                _cachedDnsMO.InvokeMethod("SetDNSServerSearchOrder", objdns, null);
                return;
            }

            ManagementClass objMC = new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection objMOC = objMC.GetInstances();
            foreach (ManagementObject objMO in objMOC)
            {
                if ((bool)objMO["IPEnabled"])
                {
                    if (objMO["Description"].ToString().Equals(CurrentInterface.Description))
                    {
                        objdns = objMO.GetMethodParameters("SetDNSServerSearchOrder");
                        if (objdns != null)
                        {
                            objdns["DNSServerSearchOrder"] = Dns;
                            objMO.InvokeMethod("SetDNSServerSearchOrder", objdns, null);
                            _cachedDnsMO = objMO;
                        }
                    }
                }
            }
        }

        public static bool IsAdministrator()
        {
            return (new WindowsPrincipal(WindowsIdentity.GetCurrent()))
                      .IsInRole(WindowsBuiltInRole.Administrator);
        }

        // Gets all addresses from a string, resolves URLs too
        public static IPAddress[] GetAllIpAddressesFromString(string str, bool resolveUrl=true)
        {
            List<IPAddress> result = new();
            foreach(string ipv4match in Ipv4AddressSelector.Matches(str).ToList().ConvertAll(x => x.Value))
            {
                if (IPAddress.TryParse(ipv4match, out IPAddress? ipv4addr))
                {
                    result.Add(ipv4addr);
                }
            }
            foreach (string ipv4match in Ipv6AddressSelector.Matches(str).ToList().ConvertAll(x => x.Value))
            {
                if (IPAddress.TryParse(ipv4match, out IPAddress? ipv6addr))
                {
                    result.Add(ipv6addr);
                }
            }
            if (!resolveUrl) return result.ToArray();
            string[] hostnames = UrlSelector.Matches(str).ToList().ConvertAll(x => x.Value).ToArray();
            foreach (string host in hostnames)
            {
                try
                {
                    result.AddRange(Dns.GetHostAddresses(new Uri(host).Host));
                }
                catch (Exception) { }
            }
            return result.ToArray();
        }
    }
}
