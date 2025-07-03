using System.Text;
using TorCSClient.Network;

namespace TorCSClient
{
    // Why did I write my own configuration? Cause I can? Yea, probably because of that, also, it sucks
    internal sealed class Configuration
    {

        public const string ConfigurationFile = "configuration";

        private static Configuration? _instance;

        public static Configuration Instance
        {
            get
            {
                _instance ??= new Configuration(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "configuration")));
                return _instance;
            }
        }

        private readonly string _configPath;

        private readonly Dictionary<string, string[]> _configuration = new()
        {
            {
                "DefaultBridges", new string[]
                {
                    "webtunnel [2001:db8:12ff:2d55:9130:36a7:c49b:d1f4]:443 933C998EC827D1C17CC93D1292BBC41735867CF8 url=https://x7t2qctb.xoomlia.com/qzxrtfmu/ ver=0.0.1",
                    "webtunnel [2001:db8:cb62:8f57:5a43:d9d0:a488:27e2]:443 16D0EF186DA080CE7A4968072920E08CA7729AED url=https://distributedcontent.website/ahxabaic5Opheir ver=0.0.1",
                    "obfs4 94.103.188.53:443 BFD5F83BE97EEAF8B3B8505B036ED7BB8A91E850 cert=Z0esGc0eTMaBA8c2ofxwuDP4sYF1U0Hd7IQA4XGv7Hyyic2hlFf0FXM2xowDA3HPUiixEg iat-mode=0",
                    "obfs4 163.172.69.160:9002 C5CC10D891A5333F28163C8840E6B2283E5C24A9 cert=zoL9pygPmz5TvZN23qiVbwSgW0tMvwSQ9Rqe5upmFad8HqJq+IX66TZmKAFpFUWf3lqcDw iat-mode=0",
                }
            },

            {
                "RelayFile", new string[1]
                {
                    Path.GetFullPath(AppContext.BaseDirectory + "details-full.json"),
                }
            },

            {
                "RelayMirrors", new string[]
                {
                    "https://github.com/ValdikSS/tor-onionoo-mirror/blob/master/details-full.json?raw=true",
                }
            },

            {
                "AdditionalTorrcConfiguration", new string[]
                {

                }
            },

            {
                "NetworkFilterType", new string[]
                {
                    "0"
                }
            },

            {
                "UseTorDNS", new string[]
                {
                    "1"
                }
            },

            {
                // Default DNS server, sets DNS to this server if the tor dns is not used
                "DefaultDNS", new string[]
                {
                    CachedNetworkInformation.Shared.MainNetworkInterface.GetIPProperties().DnsAddresses.First().ToString(),
                }
            },

            {
                "StartEnabled", new string[]
                {
                    "1"
                }
            },

            {   // Enables firewall for the system when application is running
                "ConstantFirewall", new string[]
                {
                    "0"
                }
            },

            {
                "ConnectOnStart", new string[]
                {
                    "0"
                }
            },

            {
                "HideConsole", new string[]
                {
                    "1"
                }
            },

            {
                "RelayScannerDirectory", new string[]
                {
                    Path.GetFullPath(AppContext.BaseDirectory + @"\TorRelayScannerCS\"),
                }
            },

            {
                "RelayScannerTimeout", new string[]
                {
                    "500"
                }
            },

            {
                "RelayScannerQueueSize", new string[]
                {
                    "50"
                }
            },

            {
                "RelayScannerArgs", new string[]
                {
                    "--not-install-bridges --not-start-browser -g 10000",
                }
            },

            {
                "MinBridgesCount", new string[]
                {
                    "2"
                }
            },

            {
                "FilterReloadTime", new string[]
                {
                    "20"
                }
            },

            {
                "ProxiFyreDirectory", new string[]
                {
                    Path.GetFullPath(AppContext.BaseDirectory + @"\proxifyre\"),
                }
            },

            {
                "TorDirectory", new string[]
                {
                    Path.GetFullPath(AppContext.BaseDirectory + @"\tor\"),
                }
            },

            {
                "WebtunnelDNSQueryTimeout", new string[]
                {
                    "3000" // milliseconds
                }
            }
            // AdditionalTorrcConfiguration
        };

        private Configuration(string configFile)
        {
            _configPath = configFile;
            if (!File.Exists(configFile)) return;
            string[] lines = File.ReadAllLines(configFile);
            string[] cmd;
            //_configuration.Clear();
            Dictionary<string, string[]> newConfiguration = new Dictionary<string, string[]>();
            foreach (string rawline in lines)
            {
                try
                {
                    string line = rawline.Trim();
                    if (string.IsNullOrEmpty(line) || line.StartsWith("#") || line.StartsWith("//")) continue;
                    cmd = line.Split("=", 2);
                    cmd = cmd.Select(x => x.Trim()).ToArray();
                    if (newConfiguration.ContainsKey(cmd[0]))
                    {
                        newConfiguration[cmd[0]] = newConfiguration[cmd[0]].ToList().Append(cmd[1]).ToArray();
                    }
                    else
                    {
                        newConfiguration[cmd[0]] = new string[] { cmd[1] };
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine("Unable to parse line: " + rawline);
                    //TDOD: Error handling
                }
            }
            foreach (string newKey in newConfiguration.Keys)
            {
                _configuration[newKey] = newConfiguration[newKey];
            }

            Console.WriteLine("New configuration loaded from: " + configFile);
        }

        public string[] Get(string key)
        {
            if (!_configuration.ContainsKey(key)) return Array.Empty<string>();
            return _configuration[key];
        }

        public bool GetFlag(string key)
        {
            return Convert.ToInt16(Get(key).First().Trim()) == 1;
        }

        public int GetInt(string key)
        {
            return Convert.ToInt32(Get(key).First().Trim());
        }

        public void SetInt(string key, int value)
        {
            Set(key, value.ToString());
        }

        public void SetFlag(string key, bool value)
        {
            Set(key, value ? "1" : "0");
        }

        public void Set(string key, string value)
        {
            Set(key, new string[1] { value });
        }

        public void Set(string key, string[] value, bool appendToEnd = false)
        {
            if (_configuration.ContainsKey(key) && appendToEnd)
            {
                List<string> c = _configuration[key].ToList();
                c.AddRange(value);
                _configuration[key] = c.ToArray();
            }
            else
            {
                _configuration[key] = value;
            }
        }

        public void Save()
        {
            StringBuilder generatedConfiguration = new("#" + DateTime.Now.ToString("hh:mm:ss") + "\n");
            foreach (string par in _configuration.Keys)
            {
                foreach (string val in _configuration[par])
                {
                    if (val == null || string.IsNullOrEmpty(val)) continue;
                    generatedConfiguration.AppendLine(par + "=" + val);
                }
            }
            File.WriteAllText(_configPath, generatedConfiguration.ToString());
        }
    }
}
