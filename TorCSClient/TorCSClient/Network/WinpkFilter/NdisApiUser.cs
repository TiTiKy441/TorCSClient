using NdisApi;
using System.Net;
using System.Net.NetworkInformation;

namespace WinNetworkUtilsCS.Network.WinpkFilter
{
    public abstract class NdisApiUser : IDisposable
    {

        public abstract bool Capturing { get; }

        protected NdisApiDotNet _ndisapi = new(null);

        private static readonly NdisApiDotNet ndisapi = new(null);

        private readonly static Dictionary<IntPtr, List<NdisApiUser>> _adapterUsers = new();

        private static NetworkInterface? _cachedMainNetworkInterface;

        protected bool _disposed = false;

        public NdisApiUser()
        {
            if (!_ndisapi.IsDriverLoaded()) throw new InvalidOperationException("ndisapi driver is not loaded");
            if (!_ndisapi.GetTcpipBoundAdaptersInfo().Item1 || _ndisapi.GetTcpipBoundAdaptersInfo().Item2.Count == 0) throw new Exception("ndisapi was unable to query network devices");
        }

        // Registering user to adapter was made for the purpose of keeping track of the adapter and filters/sniffers/other stuff that uses ndisapi attached to it
        // If the other ndisapi user is already running on this adapter, we can keep track of that and say that adapter is occupied
        protected static void RegisterUserToAdapter(NetworkAdapter adapter, NdisApiUser user)
        {
            if (!_adapterUsers.ContainsKey(adapter.Handle)) _adapterUsers[adapter.Handle] = new();
            _adapterUsers[adapter.Handle].Add(user);
            Console.WriteLine("New user registered to adapter at {0}", adapter.Name.Split('{', '}')[1]);
        }

        protected static bool UnregisterUserFromAdapter(NetworkAdapter adapter, NdisApiUser user)
        {
            Console.WriteLine("User unregistered from adapter at {0}", adapter.Name.Split('{', '}')[1]);
            if (_adapterUsers.ContainsKey(adapter.Handle)) return _adapterUsers[adapter.Handle].Remove(user);
            return false;
        }

        protected static bool IsOccupied(NetworkAdapter adapter)
        {
            if (!_adapterUsers.ContainsKey(adapter.Handle)) return false;
            return _adapterUsers[adapter.Handle].Any(user => user.Capturing);
        }

        public static bool ResetMainAdapter(bool forced = false)
        {
            NetworkAdapter mainAdapter = GetActiveAdapter();
            return ((!IsOccupied(mainAdapter)) || forced) && (ndisapi.ResetPacketFilterTable() & ndisapi.SetAdapterMode(mainAdapter.Handle, 0) & ndisapi.SetPacketEvent(mainAdapter.Handle, null));
        }

        // TODO: REDO
        // I am not sure how would this perform in real world
        public static NetworkAdapter GetActiveAdapter()
        {
            foreach (NetworkAdapter i in ndisapi.GetTcpipBoundAdaptersInfo().Item2)
            {
                if (GetMainInterface().GetPhysicalAddress().ToString() == i.CurrentAddress.ToString())
                {
                    return i;
                }
            }
            return ndisapi.GetTcpipBoundAdaptersInfo().Item2.First();
        }

        // This is literally a copy of Utils.GetMainInterface()
        public static NetworkInterface GetMainInterface()
        {
            if (_cachedMainNetworkInterface != null) return _cachedMainNetworkInterface;
            _cachedMainNetworkInterface = NetworkInterface.GetAllNetworkInterfaces().Where(x => x.OperationalStatus == OperationalStatus.Up
                && !x.IsReceiveOnly
                && (x.NetworkInterfaceType == NetworkInterfaceType.Ethernet || x.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                && !x.Name.StartsWith("vEthernet")
                && !x.Description.StartsWith("VirtualBox"))
                .OrderByDescending(x => x.GetIPStatistics().BytesReceived)
                .First();
            return _cachedMainNetworkInterface;
        }

        public static IPAddress GetMainInterfaceAddress()
        {
            return GetMainInterface().GetIPProperties().UnicastAddresses.Last().Address;
        }

        public void Dispose()
        {
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        protected void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(GetType().FullName);
        }
    }
}
