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
                    "webtunnel [2001:db8:345b:f823:7fe0:dec9:ee74:7525]:443 98A44ED60781F69A41B3CB4DCBA5ECE70D0AF247 url=https://b.img-cdn.net/3DV2SZvE8CrQ/ ver=0.0.1",
                    "webtunnel [2001:db8:2e6a:63f2:456a:1cc0:690e:d78d]:443 C05C827E5A85ACAE4CD73A8A5C0FA1E8EDFA4FAD url=https://arinalee.amelia.ec/apache ver=0.0.1",
                    "obfs4 150.230.148.45:9056 8A7782444203DA59602A121F975C016C015A3018 cert=swvtQzIePhZsjSc1Dq8dYNspVgs6Mfedeq+/+lssvhNN/LUcwp4y4WsQJJTh35BJj3ErVw iat-mode=0",
                    "obfs4 23.92.19.8:999 9C181BFF7D3FA7C5BCE2D1E7031F8334DDC08FC7 cert=e2gza7BCBelieFMEBRp8Et0urJJwki73SikBNfw830cxgVxEUOdOegYuXH2LjVB3M6ZSKA iat-mode=0",
                }
            },

            {
                "RelayFile", new string[1]
                {
                    Path.GetFullPath(AppContext.BaseDirectory + "details-full.json"),
                }
            },

            {
                "RelayMirrors", new string[1]
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
