﻿using System.Diagnostics;
using System.Text.Json;
using TorCSClient.Proxy;

namespace TorCSClient.Network.ProxiFyre
{
    internal sealed class ProxiFyreService
    {

        public static readonly Dictionary<string, string> Paths = new()
        {
            { "proxifyre", Path.GetFullPath(Configuration.Instance.Get("ProxiFyreDirectory").First()) },
            { "proxifyre.exe", Path.GetFullPath(Path.Combine(Configuration.Instance.Get("ProxiFyreDirectory").First(), "ProxiFyre.exe")) },
            { "app-config", Path.GetFullPath(Path.Combine(Configuration.Instance.Get("ProxiFyreDirectory").First(), "app-config.json")) },
        };

        private static ProxiFyreService? _instance;

        public static ProxiFyreService Instance { 
            get 
            {
                _instance ??= new ProxiFyreService();
                return _instance;
            }
        }

        public readonly ProxiFyreConfig Config;

        private Process? _proxiFyreProcess;

        public bool IsRunning
        {
            get
            {
                try
                {
                    return (_proxiFyreProcess != null) && (!_proxiFyreProcess.HasExited);
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
            }
        }

        public event EventHandler? OnStart;

        public event EventHandler? OnExit;

        public event EventHandler? OnStartRequested;

        private ProxiFyreService()
        {
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            if (File.Exists(Paths["app-config"]))
            {
                using (FileStream stream = File.OpenRead(Paths["app-config"])) Config = JsonDocument.Parse(stream).Deserialize<ProxiFyreConfig>();
            }
            else
            {
                Config = new ProxiFyreConfig()
                {
                    LogLevel = "Info",
                    Proxies = new ProxiFyreProxyInformation[]
                    {
                        new ProxiFyreProxyInformation()
                        {
                            AppNames = Array.Empty<string>(),
                            ProxyEndpoint = "127.0.0.1:" + TorService.Instance.GetConfigurationValue("SocksPort").First(),
                            Protocols = new string[]
                            {
                                "TCP", "UDP" // I dont know what is the behaviour for UDP, so we will pass it too, even tho tor doesnt support it
                            },
                        },
                    },
                };
            }
        }

        private void CurrentDomain_ProcessExit(object? sender, EventArgs e)
        {
            UpdateConfig();
        }

        public bool Start(bool proxyfiAllApps = false)
        {
            OnStartRequested?.Invoke(this, EventArgs.Empty);
            bool success = false;
            try
            {
                if (IsRunning) return false;
                UpdateConfig(proxyfiAllApps);
                _proxiFyreProcess = new Process()
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        FileName = Paths["proxifyre.exe"],
                        RedirectStandardInput = false,
                        RedirectStandardOutput = false,
                        UseShellExecute = false,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        CreateNoWindow = true,
                    },
                    EnableRaisingEvents = true,
                };
                _proxiFyreProcess.Exited += (s, e) =>
                {
                    Console.WriteLine("ProxiFyre service exited [PID={0}]", _proxiFyreProcess.Id);
                    OnExit?.Invoke(this, EventArgs.Empty);
                };
                success = _proxiFyreProcess.Start();

                Console.WriteLine("ProxiFyre service launched [PID={0}]", _proxiFyreProcess.Id);
                OnStart?.Invoke(this, EventArgs.Empty);
                
                _proxiFyreProcess?.WaitForExitAsync();
            }
            catch(Exception e)
            {
                Console.WriteLine("ProxiFyre launch failed: {0}", e.Message);
                _proxiFyreProcess = null;
            }
            return success;
        }

        public void Stop()
        {
            if (!IsRunning) return;
            //_proxiFyreProcess.Kill(entireProcessTree: true);
            if (!_proxiFyreProcess.CloseMainWindow())
            {
                _proxiFyreProcess.Kill(true);
            }
        }

        public void UpdateConfig(bool proxyfiAllApps = false)
        {
            if (IsRunning) throw new InvalidOperationException("Cant update ProxiFyre config while ProxiFyre is running");
            if (proxyfiAllApps)
            {
                string[] apps = GetApps();
                SetApps(Array.Empty<string>());
                File.WriteAllText(Paths["app-config"], JsonSerializer.Serialize(Config));
                SetApps(apps);
            }
            else
            {
                File.WriteAllText(Paths["app-config"], JsonSerializer.Serialize(Config));
            }
        }

        public string[] GetApps()
        {
            return Config.Proxies.FirstOrDefault().AppNames;
        }

        public void SetApps(string[] apps)
        {
            Config.Proxies[0].AppNames = apps;
        }
    }
}
