using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace TorCSClient.Relays
{
    internal sealed class RelayScanner
    {

        public static bool Scanning
        {
            get
            {
                try
                {
                    return _relayScannerProcess != null && !_relayScannerProcess.HasExited;
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
            }
        }

        public static event EventHandler<EventArgs>? OnScanEnded;
        public static event EventHandler<OnNewWorkingRelayEventArgs>? OnNewWorkingRelay;

        private static Process? _relayScannerProcess;

        private readonly static Regex _vanillaBridgeRegex = new(@"(?<ip>\d{1,3}(?:\.\d{1,3}){3}:\d{1,5})\s(?<fingerpring>[A-F0-9]{40})");

        public static void StartScan(int timeout, int packetSize)
        {
            if (Scanning) return;

            _relayScannerProcess = new Process();

            ProcessStartInfo startInfo = new()
            {
                FileName = Path.Combine(Configuration.Instance.Get("RelayScannerDirectory").First(), "TorRelayScannerCS.exe"),
                Arguments = string.Format("-n {0} --timeout {1} {2} ", packetSize.ToString(), timeout.ToString(), Configuration.Instance.Get("RelayScannerArgs").First()),
                //UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
            };

            _relayScannerProcess = new()
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true,
            };

            _relayScannerProcess.OutputDataReceived += (s, e) =>
            {
                if (e.Data?.Length > 0)
                {
                    if (_vanillaBridgeRegex.IsMatch(e.Data.ToString()))
                    {
                        Match match = _vanillaBridgeRegex.Match(e.Data.ToString());
                        OnNewWorkingRelay?.Invoke(null, new OnNewWorkingRelayEventArgs(e.Data.ToString()));
                    }
                }
            };
            _relayScannerProcess.Exited += (s, e) =>
            {
                OnScanEnded?.Invoke(null, EventArgs.Empty);
                Console.WriteLine("Tor relay scanner exited [PID={0}]", _relayScannerProcess.Id);
            };

            _relayScannerProcess.Start();
            _relayScannerProcess.BeginOutputReadLine();
            //_relayScannerProcess.WaitForExitAsync();
            Console.WriteLine("Tor relay scanner launched [PID={0}]", _relayScannerProcess.Id);
        }

        public static void StopScan()
        {
            if (!Scanning) return;//throw new InvalidOperationException("Scan is not in progress");
            _relayScannerProcess?.Kill(true);
        }

        public static void WaitForEnd()
        {
            if (!Scanning) return;
            _relayScannerProcess?.WaitForExit();
        }
    }

    public class OnNewWorkingRelayEventArgs : EventArgs
    {

        public readonly string Relay;

        public OnNewWorkingRelayEventArgs(string relay) : base()
        {
            Relay = relay;
        }
    }
}

