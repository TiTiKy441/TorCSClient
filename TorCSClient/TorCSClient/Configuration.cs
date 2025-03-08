using System.Text;

namespace TorCSClient
{
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
                    "obfs4 5.9.70.38:9002 F33AD3B1655C0D1FC13B455FCFC9704384086CE0 cert=4TO12+5smChHZG2sVBpDAI/rvYwLVai+A4ljNXLG5rlvrL/LoJ5vfXdVEkBjwJBzL1E6Og iat-mode=0",
                    "obfs4 212.21.66.21:40425 363E9725C18A67E86D4978EF6AE3B4C50DFC9B55 cert=L1wlC3DNi60m/+JxA/FRqTIpICp/2etDdZKDManMQhBDg/WS577C9K3I8gGNN6Uc5r9qVw iat-mode=0",
                    "obfs4 212.21.66.66:20621 986E06A61EC62DC0DD58E0A1BB6EE54463EF408A cert=ifbMX0EZ6eqTfCuyiiR0LDEZVX2UVGHEFvqbu5qb6wCZELi5WYhEEWIIBek5MSvyTQx3CA iat-mode=0",
                    "obfs4 212.21.66.73:35401 C36F8D3C481910ED7A34F5ECEBE1C7C9A258F4A8 cert=9IygPQi2UKJ6pUjYTHl8ltg1cuPDvcsE9Os9TPVSioR0qmXU/0uSvD3rsm3jskV1nupJAg iat-mode=0",
                    "obfs4 5.9.122.242:20132 A0329253C08BAA072F923BF00F2E9CEBBE0B0162 cert=nfmTZXNl2FDOG7xK1cFxB6NfOGiUQQBrrPgphV9afzXvj+s+b+e5uOBfIuUh5gWNbnp7Gg iat-mode=0",
                    "obfs4 5.9.140.196:54444 F3037FFB92C3E7F11D1D8C5DCD744C10C09AE4D6 cert=ymd54jE/rwsNkYm2bmGRqAzb/U2mrpitnR3CZsxKcw1Dm2CFzNa1q6UoYfizVvub+6vBFA iat-mode=0",
                    "webtunnel [2001:db8:8c19:9032:a7d3:bdb9:1795:1f63]:443 2A815E6A368C4B5B0AE0E31967DBCE186855F501 url=https://www.iplease.top/l4t795ZBZTh8iHbK5Aba70TT ver=0.0.1",
                    "webtunnel [2001:db8:4dce:3e6b:32a3:a054:788c:d7f0]:443 339281F7912780D8A7FB118A48A65FFBF92F043F url=https://mwumba.com/9f2jjnAR9LR1fInBy60tbS57 ver=0.0.1",
                    "webtunnel [2001:db8:3c2a:2d1c:6e06:549c:72ac:bfc0]:443 5F55B60FF2AEA996B5109BC338FC5F2BCA696F52 url=https://cloudstreamingcdn.website/Iej1bootoo9ef4A ver=0.0.1",
                    "webtunnel [2001:db8:1fc0:eebe:5e6e:d6ee:f53e:6889]:443 4A3859C089DF40A4FFADC10A79DFEBE4F8272535 url=https://verry.org/K2A2utQIMou4Ia2WjVseyDjV ver=0.0.1",
                    "webtunnel [2001:db8:8d54:8d31:43e8:80aa:bca:24ca]:443 447BBCEB9662D3FA3133199BF47A3D4CAB2D627C url=https://gitwizardry.pw/ThieLe5Ein0luofief7o ver=0.0.1",
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
                "UseTorAsSystemProxy", new string[]
                {
                    "1"
                }
            },

            {
                // Default DNS server, sets DNS to this server if the tor dns is not used
                "DefaultDNS", new string[]
                {
                    Utils.GetDnsAddress().ToString()
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
