using System.Text;
using System.Diagnostics;
using System.Text.RegularExpressions;
using TorCSClient.Proxy.Control;
using System.Net;
using TorCSClient.Relays;

namespace TorCSClient.Proxy
{
    internal sealed class TorService
    {

        private static TorService? _instance;

        public static TorService Instance { 
            get 
            {
                _instance ??= new TorService();
                return _instance;
            } 
        }

        public TorServicePort TorController;

        public static readonly Dictionary<string, string> Paths = new()
        {
            { "tor", Path.GetFullPath(Configuration.Instance.Get("TorDirectory").First()) },
            { "lyrebird.exe", Path.GetFullPath(Path.Combine(Configuration.Instance.Get("TorDirectory").First(), "lyrebird.exe")) },
            { "tor.exe", Path.GetFullPath(Path.Combine(Configuration.Instance.Get("TorDirectory").First(), "tor.exe")) },
            { "torrc", Path.GetFullPath(Path.Combine(Configuration.Instance.Get("TorDirectory").First(), "torrc")) },
        };

        public static readonly Dictionary<string, string> TorCache = new()
        {
            { "cached_certs", Path.GetFullPath(Path.Combine(Configuration.Instance.Get("TorDirectory").First(), "cached-certs")) },
            { "cached_descriptors", Path.GetFullPath(Path.Combine(Configuration.Instance.Get("TorDirectory").First(), "cached-descriptors")) },
            { "cached_descriptos_new", Path.GetFullPath(Path.Combine(Configuration.Instance.Get("TorDirectory").First(), "cached-descriptors.new")) },
        };

        private Dictionary<string, string[]> _torrcConfiguration = new()
        {
            { "ClientTransportPlugin", new string[] { "obfs4,webtunnel exec lyrebird.exe" } },
            { "SocksPort", new string[] { "9050" } },
            { "ControlPort", new string[] { "9051" } },
            { "UseBridges", new string[] { "1" } },
            { "DataDirectory", new string[] { AppContext.BaseDirectory.Replace(@"\", "/") + "/tor/" } },
            { "DNSPort", new string[] { "53" } },
        };

        private Process? _torProcess;

        private ProxyStatus _status = ProxyStatus.Disabled;

        public ProxyStatus Status
        {
            get
            {
                return _status;
            }
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnStatusChange?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private string _startupStatus = string.Empty;

        public string StartupStatus { 
            get
            {
                return _startupStatus;
            }
            set
            {
                _startupStatus = value;
                OnStartupStatusChange?.Invoke(this, EventArgs.Empty);
            }
        }

        public List<string> FilteredBridges { get; private set; } = new();
        public List<string> WorkingBridges { get; private set; } = new();
        public List<string> DeadBridges { get; private set; } = new();

        public bool BridgeProxyExhausted { get; private set; } = false;

        public event EventHandler? OnStartupStatusChange;
        public event EventHandler? OnStatusChange;
        public event EventHandler? OnBridgeProxyExhaustion;
        public event EventHandler<NewBridgeEventArgs>? OnNewWorkingBridge;
        public event EventHandler<NewBridgeEventArgs>? OnNewDeadBridge;
        public event EventHandler<NewMessageEventArgs>? OnNewMessage;

        public bool ProxyRunning 
        { 
            get 
            {
                try
                {
                    return _torProcess != null && !_torProcess.HasExited;
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
            }
        }

        private TorService()
        {

            _instance = this;
            ClearCache();
            if (File.Exists(Paths["torrc"]))
            {
                _torrcConfiguration.Clear();
                string trimmedLine;
                string[] splitLine;
                List<string> value;
                foreach (string line in File.ReadAllLines(Paths["torrc"]))
                {
                    trimmedLine = line.Trim();
                    if (trimmedLine.StartsWith("#") || trimmedLine.StartsWith("//") || !trimmedLine.Contains(' ') || string.IsNullOrWhiteSpace(trimmedLine)) continue;
                    splitLine = trimmedLine.Split(' ', 2);
                    if (!_torrcConfiguration.ContainsKey(splitLine[0])) _torrcConfiguration[splitLine[0]] = Array.Empty<string>();
                    value = _torrcConfiguration[splitLine[0]].ToList();
                    value.Add(splitLine[1]);
                    _torrcConfiguration[splitLine[0]] = value.ToArray();
                }
            }
            else
            {
                UpdateTorrc();
            }
            TorController = new TorServicePort(Convert.ToInt32(GetConfigurationValue("ControlPort").First()));
            OnStatusChange += TorService_OnStatusChange;
        }

        private void TorService_OnStatusChange(object? sender, EventArgs e)
        {
            switch (Status)
            {
                case ProxyStatus.Disabled:
                    Console.WriteLine("Tor status changed to disabled");
                    TorController.Disconnect();
                    break;

                case ProxyStatus.Starting:
                    Console.WriteLine("Tor status changed to starting");
                    TorController.Connect();
                    TorController.Authenticate();
                    break;

                case ProxyStatus.Running:
                    Console.WriteLine("Tor status changed to running");
                    break;
            }
        }

        public List<IPAddress> GetUsedAddresses(bool resolveDns = false, int timeout = 1000)
        {
            List<IPAddress> addresses = new();
            if (GetConfigurationValue("UseBridges").First() == "1")
            {
                foreach (string bridgeLine in GetConfigurationValue("Bridge"))
                {
                    if (bridgeLine.StartsWith("webtunnel", StringComparison.CurrentCultureIgnoreCase))
                    {
                        foreach (Match urlMatch in Utils.UrlSelector.Matches(bridgeLine))
                        {
                            if (urlMatch.Success)
                            {
                                Task.Factory.StartNew(() =>
                                {
                                    try
                                    {
                                        IPAddress[] resolved = Dns.GetHostAddresses(new Uri(urlMatch.Value).Host);
                                        addresses.AddRange(resolved);
                                    }
                                    catch (Exception)
                                    {
                                    }
                                }).Wait(timeout);
                            }
                        }
                    }
                    else
                    {
                        addresses.AddRange(Utils.GetAllIpAddressesFromString(bridgeLine));
                    }
                }
            }
            else
            {
                foreach (Relay relay in RelayDistributor.Instance.GuardRelays)
                {
                    addresses.AddRange(relay.GetAddresses());
                }
            }
            return addresses;
        }

        public void UpdateTorrc()
        {
            StringBuilder generatedConfiguration = new("#" + DateTime.Now.ToString("hh:mm:ss") + "\n");
            foreach (string par in _torrcConfiguration.Keys)
            {
                foreach (string val in _torrcConfiguration[par])
                {
                    if (val == null || string.IsNullOrEmpty(val)) continue;
                    generatedConfiguration.AppendLine(par + " " + val);
                }
            }
            File.WriteAllText(Paths["torrc"], generatedConfiguration.ToString());
        }

        public void ClearCache()
        {
            if (ProxyRunning) return;
            foreach (string cachedFile in TorCache.Values)
            {
                if (File.Exists(cachedFile)) File.Delete(cachedFile);
            }
        }

        public string[] GetConfigurationValue(string key)
        {
            if (!_torrcConfiguration.ContainsKey(key)) return Array.Empty<string>();
            return _torrcConfiguration[key];
        }

        public int GetSocksPort()
        {
            return Convert.ToInt32(GetConfigurationValue("SocksPort").First());
        }

        public IPEndPoint GetSocksEndPoint()
        {
            return new IPEndPoint(IPAddress.Loopback, GetSocksPort());
        }

        public int GetDNSPort()
        {
            return Convert.ToInt32(GetConfigurationValue("DNSPort").First());
        }

        public IPEndPoint GetDNSEndPoint()
        {
            return new IPEndPoint(IPAddress.Loopback, GetDNSPort());
        }

        public bool ExistsConfigurationValue(string key)
        {
            return _torrcConfiguration.ContainsKey(key);
        }

        public void SetConfigurationValue(string key, string value, bool appendToEnd = false)
        {
            if (appendToEnd) _torrcConfiguration[key] = _torrcConfiguration[key].Append(value).ToArray();
            else _torrcConfiguration[key] = new string[] { value, };
        }

        public void SetConfigurationValue(string key, string[] value, bool appendToEnd = false)
        {
            if (appendToEnd)
            {
                List<string> c = _torrcConfiguration[key].ToList();
                c.AddRange(value);
                _torrcConfiguration[key] = c.ToArray();
            }
            else _torrcConfiguration[key] = value;
        }

        public void StopTor(bool forced=false)
        {
            if (!ProxyRunning) return;
            if (TorController.IsUsable && !forced)
            {
                TorController.Shutdown();
            }
            else
            {
                //_TorCSClientProcess?.StandardInput.Close();
                //_TorCSClientProcess?.StandardOutput.Close();
                if (!_torProcess.CloseMainWindow())
                {
                    _torProcess?.Kill(entireProcessTree: true);
                }
                //_TorCSClientProcess?.WaitForExit();
            }
            WorkingBridges.Clear();
            FilteredBridges.Clear();
            DeadBridges.Clear();
        }

        public void WaitForEnd()
        {
            //_TorCSClientProcess?.WaitForExit();
            while (Status != ProxyStatus.Disabled)
            {
                Thread.Sleep(1);
            }
        }

        public static void KillTorProcess()
        {
            List<string> usedFilenames = Paths.Values.ToList().ConvertAll(x => Path.GetFileName(x)).Where((x, i) => Path.GetExtension(x) == ".exe").ToList();
            List<Process> processesToKill = new();
            foreach (Process p in Process.GetProcesses().Where((x, i) => usedFilenames.Contains(x.ProcessName + ".exe")))
            {
                try
                {
                    if (p.Id == Environment.ProcessId || p.Modules == null) continue;
                    foreach (ProcessModule module in p.Modules)
                    {
                        if (module.FileName != null && Paths.ContainsValue(module.FileName)) processesToKill.Add(p);
                    }
                }
                catch (Exception)
                {
                }
            }
            processesToKill.ForEach(x => x.Kill(true));
        }

        public void ReloadWithWorkingBridges(bool forcedStop = false)
        {
            string[] working = WorkingBridges.ToArray();
            StopTor(forcedStop);
            WaitForEnd();
            SetConfigurationValue("Bridge", working);
            StartTor();
        }

        public void StartTor()
        {
            if (ProxyRunning) return;
            if (_torrcConfiguration.ContainsKey("Bridge")) FilteredBridges = _torrcConfiguration["Bridge"].ToList();
            ClearCache();
            UpdateTorrc();
            _torProcess?.Close();
            _torProcess = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = Paths["tor.exe"],
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    Arguments = string.Format("-f \"{0}\"", Paths["torrc"]),
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                },
                EnableRaisingEvents = true,
            };
            _torProcess.Start();

            StartupStatus = "Process started";
            char logType;
            string? line;
            _torProcess.OutputDataReceived +=  (s, e) =>
            {
                try
                {
                    if (e.Data?.Length > 0)
                    {
                        line = e.Data.ToString().ToUpper();

                        logType = Utils.SquareBracketsSelector.Match(line).Value[0];

                        OnNewMessage?.Invoke(this, new NewMessageEventArgs(line));

                        if (line.Contains("BOOTSTRAPPED") || line.Contains("STARTING"))
                        {
                            StartupStatus = "Conn: " + Utils.PercentageSelector.Match(line).Value;
                            Status = ProxyStatus.Starting;
                        }
                        if (line.Contains("DONE") && line.Contains("100%"))
                        {
                            StartupStatus = "Done";
                            Status = ProxyStatus.Running;
                        }
                        if (line.Contains("INTERRUPT: EXITING CLEANLY"))
                        {
                            Status = ProxyStatus.Disabled;
                        }

                        string serveripv4;
                        string serveripv6;
                        string bridgeString;
                        if (logType == 'W' && line.Contains("MAKE SURE THAT THE PROXY SERVER IS UP AND RUNNING") && !BridgeProxyExhausted)
                        {
                            BridgeProxyExhausted = true;
                            OnBridgeProxyExhaustion?.Invoke(this, EventArgs.Empty);
                        }
                        if (logType == 'W' && line.Contains("UNABLE TO CONNECT"))
                        {
                            serveripv4 = Utils.Ipv4AddressSelector.Match(line).Value;
                            serveripv6 = Utils.Ipv6AddressSelector.Match(line).Value.ToLower();
                            bridgeString = _torrcConfiguration["Bridge"].ToList().FindAll(x => ((serveripv4 != string.Empty ? x.Contains(serveripv4) : false) || (serveripv6 != string.Empty ? x.ToLower().Contains(serveripv6) : false)))[0];
                            WorkingBridges.RemoveAll(x => ((serveripv4 != string.Empty ? x.Contains(serveripv4) : false) || (serveripv6 != string.Empty ? x.ToLower().Contains(serveripv6) : false)));
                            if (!DeadBridges.Contains(bridgeString)) DeadBridges.Add(bridgeString);
                            if (FilteredBridges.Any(x => ((serveripv4 != string.Empty ? x.Contains(serveripv4) : false) || (serveripv6 != string.Empty ? x.ToLower().Contains(serveripv6) : false))))
                            {
                                OnNewDeadBridge?.Invoke(this, new NewBridgeEventArgs(bridgeString));
                                FilteredBridges.RemoveAll(x => ((serveripv4 != string.Empty ? x.Contains(serveripv4) : false) || (serveripv6 != string.Empty ? x.ToLower().Contains(serveripv6) : false)));
                            }
                        }

                        if (logType == 'N' && line.Contains("NEW BRIDGE DESCRIPTOR"))
                        {
                            serveripv4 = Utils.Ipv4AddressSelector.Match(line).Value;
                            serveripv6 = Utils.Ipv6AddressSelector.Match(line).Value.ToLower();
                            bridgeString = _torrcConfiguration["Bridge"].ToList().FindAll(x => ((serveripv4 != string.Empty ? x.Contains(serveripv4) : false) || (serveripv6 != string.Empty ? x.ToLower().Contains(serveripv6) : false)))[0];
                            if (!FilteredBridges.Contains(bridgeString)) FilteredBridges.Add(bridgeString);
                            if (DeadBridges.Contains(bridgeString)) DeadBridges.Remove(bridgeString);
                            if (!WorkingBridges.Contains(bridgeString))
                            {
                                WorkingBridges.Add(bridgeString);
                                OnNewWorkingBridge?.Invoke(this, new NewBridgeEventArgs(bridgeString));
                            }
                        }
                        
                        if (logType == 'N' && line.Contains("INTERRUPT"))
                        {

                        }
                    };
                }catch(Exception)
                {
                }  
            };
            _torProcess.Exited += (s, e) =>
            {
                BridgeProxyExhausted = false;
                Status = ProxyStatus.Disabled;
                Console.WriteLine("Tor proxy exited [PID={0}]", _torProcess.Id);
            };
            _torProcess.ErrorDataReceived += (s, e) =>
            {
                BridgeProxyExhausted = false;
                Status = ProxyStatus.Disabled;
            };
            //_torProcess.WaitForExitAsync();
            _torProcess.BeginErrorReadLine();
            _torProcess.BeginOutputReadLine();
            Console.WriteLine("Tor proxy launched [PID={0}]", _torProcess.Id);
        }
    }

    public sealed class NewBridgeEventArgs : EventArgs
    {

        public readonly string Bridge;

        public NewBridgeEventArgs(string bridgeString) : base()
        {
            Bridge = bridgeString;
        }
    }

    public sealed class NewMessageEventArgs : EventArgs
    {

        public readonly string Message;

        public NewMessageEventArgs(string message) : base()
        {
            Message = message;
        }
    }
    public enum ProxyStatus
    {
        Disabled,
        Starting,
        Running,
    }
};