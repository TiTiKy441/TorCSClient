namespace WinNetworkUtilsCS.Network.WinpkFilter
{
    public class NetworkSpeedMonitor : IDisposable
    {

        public readonly INetworkStatistics NetworkStatistics;

        private readonly System.Timers.Timer _timer;

        public bool IsRunning
        {
            get
            {
                return _timer.Enabled;
            }
        }

        private ulong _oldPacketsReceived = 0;
        private ulong _oldBytesReceived = 0;

        private ulong _oldPacketsSent = 0;
        private ulong _oldBytesSent = 0;

        private DateTime _lastUpdate;

        private double DeltaTime
        {
            get
            {
                return (DateTime.Now - _lastUpdate).TotalSeconds;
            }
        }

        private bool _disposed = false;

        public double PacketsReceiveSpeed { get; private set; }

        public double BytesReceiveSpeed { get; private set; }

        public double PacketsSentSpeed { get; private set; }

        public double BytesSentSpeed { get; private set; }

        public event EventHandler? OnMonitorUpdate;

        public NetworkSpeedMonitor(INetworkStatistics stats, int updateInterval = 500)
        {
            NetworkStatistics = stats;
            _timer = new System.Timers.Timer()
            {
                Interval = updateInterval,
                AutoReset = true,
            };
            _timer.Elapsed += Timer_Elapsed;
            _oldPacketsReceived = NetworkStatistics.PacketsReceived;
            _oldBytesReceived = NetworkStatistics.BytesReceived;
            _oldPacketsSent = NetworkStatistics.PacketsSent;
            _oldBytesSent = NetworkStatistics.BytesSent;
            _lastUpdate = DateTime.Now;
        }

        private void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            PacketsReceiveSpeed = (NetworkStatistics.PacketsReceived - _oldPacketsReceived) / DeltaTime;
            BytesReceiveSpeed = (NetworkStatistics.BytesReceived - _oldBytesReceived) / DeltaTime;
            PacketsSentSpeed = (NetworkStatistics.PacketsSent - _oldPacketsSent) / DeltaTime;
            BytesSentSpeed = (NetworkStatistics.BytesSent - _oldBytesSent) / DeltaTime;

            _lastUpdate = DateTime.Now;
            _oldPacketsReceived = NetworkStatistics.PacketsReceived;
            _oldBytesReceived = NetworkStatistics.BytesReceived;
            _oldPacketsSent = NetworkStatistics.PacketsSent;
            _oldBytesSent = NetworkStatistics.BytesSent;
            OnMonitorUpdate?.Invoke(this, EventArgs.Empty);
        }

        public void Start()
        {
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _timer.Stop();
            _timer.Elapsed -= Timer_Elapsed;
            _timer.Dispose();
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
