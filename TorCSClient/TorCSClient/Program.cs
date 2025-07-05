using TorCSClient.Proxy;
using TorCSClient.Relays;
using TorCSClient.GUI;
using TorCSClient.Listener;
using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using NdisApi;
using TorCSClient.Network;
using System.Runtime.CompilerServices;

namespace TorCSClient
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Utils.AllocConsole();

            if (!Utils.IsAdministrator())
            {
                Console.WriteLine("this application requires to be run as administrator");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                return;
            }

            if (Configuration.Instance.GetFlag("HideConsole")) 
            { 
                Utils.HideConsole(); 
            }
            else
            {
                Utils.ShowConsole();
            }

            ApplicationConfiguration.Initialize();
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            TorService.KillTorProcess();

            CheckWinPkFilter();
            CheckProxiFyre();
            CheckTorRelayScanner();

            MainListener.Initialize();
            MainListener.EnableTor(false);

            while (RelayDistributor.Instance == null)
            {
                try
                {
                    RelayDistributor.Initialize(Configuration.Instance.Get("RelayFile").First());
                }
                catch (Exception)
                {
                    Utils.ShowConsole();
                    Console.WriteLine("Relay file is corrupted or doesnt exist");
                    File.Delete(Configuration.Instance.Get("RelayFile").First());
                    DownloadRelays();
                }
            }

            string[] cmd;
            Dictionary<string, string[]> overrides = new();
            foreach (string overrideParameter in Configuration.Instance.Get("OverrideTorrcConfiguration"))
            {
                cmd = overrideParameter.Split(" ", 2);
                if (overrides.ContainsKey(cmd[0]))
                {
                    overrides[cmd[0]] = overrides[cmd[0]].ToList().Append(cmd[1]).ToArray();
                }
                else
                {
                    overrides.Add(cmd[0], new string[] { cmd[1] });
                }
            }

            foreach (string overrideParameter in overrides.Keys)
            {
                TorService.Instance.SetConfigurationValue(overrideParameter, overrides[overrideParameter], false);
            }

            foreach (string additionalParameter in Configuration.Instance.Get("AdditionalTorrcConfiguration"))
            {
                cmd = additionalParameter.Split(" ", 2);
                TorService.Instance.SetConfigurationValue(cmd[0], cmd[1], true);
                Console.WriteLine("Overriding torrc parameter: " + cmd[0]);
            }

            IconUserInterface iconInterface = new();
            iconInterface.Show();

            TorControl torControlWindow = new();
            torControlWindow.Show();

            Settings settingsWindow = new();
            settingsWindow.Show();

            if (Configuration.Instance.GetFlag("ConnectOnStart")) torControlWindow.connect_button.AccessibilityObject.DoDefaultAction();

            Console.WriteLine("Application running");
            Application.Run(iconInterface);
        }

        private static void CurrentDomain_ProcessExit(object? sender, EventArgs e)
        {
            Configuration.Instance.Save();
        }

        private static void DownloadRelays()
        {
            if (!File.Exists(Configuration.Instance.Get("RelayFile")[0]))
            {
                foreach (string mirror in Configuration.Instance.Get("RelayMirrors"))
                {
                    try
                    {
                        Console.WriteLine("Trying to download relay file from: " + mirror);
                        Utils.DownloadToFile(mirror, Configuration.Instance.Get("RelayFile")[0]);
                        break;
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Unable to download relay mirror from: " + mirror);
                        //TODO: Error handling
                    }
                }
            }
        }

        private static void CheckTorRelayScanner()
        {
            string relayScannerDirectory = Path.GetFullPath(Configuration.Instance.Get("RelayScannerDirectory").First());
            if (Directory.Exists(relayScannerDirectory)) return;

            Utils.ShowConsole();

            Console.WriteLine("TorRelayScannerCS direcotory was not found");
            Console.WriteLine("Trying to downlaod TorRelayScannerCS");

            string url = @"https://github.com/TiTiKy441/TorRelayScannerCS/releases/latest/download/TorRelayScannerCS_win" + RuntimeInformation.ProcessArchitecture.ToString().Substring(1) + ".zip";
            string archive = Path.GetFullPath(AppContext.BaseDirectory + @"\TorRelayScannerCS.zip");

            Console.WriteLine("Download URL: " + url);
            Console.WriteLine("Installation path: " + relayScannerDirectory);
            Console.WriteLine("Installation archive: " + archive);

            try
            {
                Utils.DownloadToFile(url, archive);
                Directory.CreateDirectory(relayScannerDirectory);
                ZipFile.ExtractToDirectory(archive, relayScannerDirectory);
                Console.WriteLine("TorRelayScannerCS downloaded!");
                File.Delete(archive);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to download TorRelayScannerCS!");
                Console.WriteLine(ex);
                Console.WriteLine("Not critical");
            }
            if (Configuration.Instance.GetFlag("HideConsole")) Utils.HideConsole();
        }

        private static void CheckProxiFyre()
        {
            string proxiFyreDirectory = Path.GetFullPath(Configuration.Instance.Get("ProxiFyreDirectory").First());
            if (Directory.Exists(proxiFyreDirectory)) return;

            Utils.ShowConsole();

            Console.WriteLine("ProxiFyre directory was not found!");
            Console.WriteLine("Trying to download proxifyre");

            string url = @"https://github.com/wiresock/proxifyre/releases/download/v1.0.22/ProxiFyre-v1.0.22-" + RuntimeInformation.ProcessArchitecture + "-signed.zip";
            string archive = Path.GetFullPath(AppContext.BaseDirectory + @"\proxifyre.zip");

            Console.WriteLine("Download URL: " + url);
            Console.WriteLine("Installation path: " + proxiFyreDirectory);
            Console.WriteLine("Installation archive: " + archive);

            try
            {
                Utils.DownloadToFile(url, archive);
                Directory.CreateDirectory(proxiFyreDirectory);
                ZipFile.ExtractToDirectory(archive, proxiFyreDirectory);
                Console.WriteLine("ProxiFyre downloaded!");
                File.Delete(archive);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to download proxifyre!");
                Console.WriteLine(ex);
                Console.WriteLine("Not critical");
            }
            if (Configuration.Instance.GetFlag("HideConsole")) Utils.HideConsole();
        }

        private static void CheckWinPkFilter()
        {
            using (NdisApiDotNet ndisapi = new(null))
            {
                if (ndisapi.IsDriverLoaded())
                {
                    Console.WriteLine("WinpkFilter found! (version - {0})", ndisapi.GetVersion());
                    return; 
                }

                Utils.ShowConsole();

                Console.WriteLine("WinpkFilter driver is not found!");

                Console.WriteLine("Windows packet filter driver is not loaded!");
                Console.WriteLine("Trying to download and install windows packet filter ");

                string url = @"https://github.com/wiresock/ndisapi/releases/download/v3.6.1/Windows.Packet.Filter.3.6.1.1." + System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture + ".msi";
                string installerPath = Path.GetFullPath(AppContext.BaseDirectory + @"\WinpkFilter.msi");
                Console.WriteLine("Download URL: " + url);
                Console.WriteLine("Installer path: " + installerPath);
                try
                {
                    Utils.DownloadToFile(url, installerPath);
                    Process installerProcess = new();
                    ProcessStartInfo processInfo = new();
                    processInfo.Arguments = string.Format("/i \"{0}\" /q", installerPath);
                    processInfo.FileName = "msiexec";
                    installerProcess.StartInfo = processInfo;
                    installerProcess.Start();
                    installerProcess.WaitForExit();
                    using (NdisApiDotNet tempNdisApi = new(null))
                    {
                        if (tempNdisApi.IsDriverLoaded()) Console.WriteLine("WinpkFilter installed");
                        else throw new Exception("WinpkFilter installation failed");
                    }
                    File.Delete(installerPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Unable to install Windows packet filter driver!");
                    Console.WriteLine(ex);
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey();
                    Environment.Exit(255);
                }
                if (Configuration.Instance.GetFlag("HideConsole")) Utils.HideConsole();
            }
        }
    }
}