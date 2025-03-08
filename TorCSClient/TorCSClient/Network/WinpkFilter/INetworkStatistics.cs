namespace WinNetworkUtilsCS.Network.WinpkFilter
{
    public interface INetworkStatistics
    {

        public ulong PacketsReceived { get; }

        public ulong BytesReceived { get; }

        public ulong PacketsSent { get; }

        public ulong BytesSent { get; }

    }
}
