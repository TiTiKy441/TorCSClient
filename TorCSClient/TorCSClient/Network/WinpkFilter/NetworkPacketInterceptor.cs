using NdisApi;

namespace WinNetworkUtilsCS.Network.WinpkFilter
{
    public class NetworkPacketInterceptor : NdisApiUser, INetworkStatistics
    {

        public readonly MSTCP_FLAGS Mode;

        public override bool Capturing
        {
            get
            {
                return _cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested;
            }
        }

        protected ManualResetEvent _packetEvent = new(false);

        protected readonly NetworkAdapter _adapter;

        protected readonly NdisBufferResource _buffer;

        protected CancellationTokenSource _cancellationTokenSource = new();

        protected ulong _packetsReceived = 0;

        public ulong PacketsReceived
        {
            get
            {
                return _packetsReceived;
            }
        }

        protected ulong _bytesReceived = 0;

        public ulong BytesReceived
        {
            get
            {
                return _bytesReceived;
            }
        }

        protected ulong _packetsSent = 0;

        public ulong PacketsSent
        {
            get
            {
                return _packetsSent;
            }
        }

        protected ulong _bytesSent = 0;

        public ulong BytesSent
        {
            get
            {
                return _bytesSent;
            }
        }

        public const int BufferSize = 64;

        public readonly bool SinglePacketHandle = false;

        private Task? _interceptingTask;

        /**
         * NetworkPacketInterceptor - intercepts packets coming from or coming to the specified adapter
         * 
         * Reforwards packets to HandlePackets() or HandlePacket() depending on singlePacketHandle value, if true, HandlePacket()
         *
         * If the capture adapter is null, uses the autodetermined main adapter 
         **/
        public NetworkPacketInterceptor(MSTCP_FLAGS mode, NetworkAdapter? captureAdapter = null, bool singlePacketHandle = false) : base()
        {
            Mode = mode;
            SinglePacketHandle = singlePacketHandle;
            _adapter = captureAdapter ?? GetActiveAdapter();
            _buffer = new NdisBufferResource(BufferSize);
            _packetEvent = new ManualResetEvent(false);
            // We create a cancellation token source at the beginning and then immidiately cancel
            // it in the constructor so that the Capturing stays false at start
            _cancellationTokenSource.Cancel();
            RegisterUserToAdapter(_adapter, this);
        }

        protected virtual void FilterWork()
        {
            WaitHandle[] handles = new WaitHandle[] { _cancellationTokenSource.Token.WaitHandle, _packetEvent };
            Tuple<bool, List<RawPacket>> packetList;
            do
            {
                try
                {
                    WaitHandle.WaitAny(handles, 100);
                    packetList = _ndisapi.ReadPackets(_adapter.Handle, _buffer);

                    if (!packetList.Item1 || packetList.Item2.Count == 0) continue;
                    if (!SinglePacketHandle)
                    {
                        HandlePackets(packetList.Item2);
                    }
                    else
                    {
                        foreach (RawPacket packet in packetList.Item2)
                        {
                            HandlePacket(packet);
                        }
                    }
                }
                finally
                {
                    _packetEvent.Reset();
                }
            } while (Capturing);
        }

        protected virtual void HandlePackets(List<RawPacket> rawPackets)
        {
            List<RawPacket> toAdapter = rawPackets.Where(rawPacket => rawPacket.DeviceFlags == PACKET_FLAG.PACKET_FLAG_ON_SEND).ToList();
            List<RawPacket> toMstcp = rawPackets.Where(rawPacket => rawPacket.DeviceFlags == PACKET_FLAG.PACKET_FLAG_ON_RECEIVE).ToList();

            toAdapter.ForEach(rawPacket =>
            {
                Interlocked.Increment(ref _packetsSent);
                Interlocked.Add(ref _bytesSent, (ulong)rawPacket.Data.Length);
            });
            toMstcp.ForEach(rawPacket =>
            {
                Interlocked.Increment(ref _packetsReceived);
                Interlocked.Add(ref _bytesReceived, (ulong)rawPacket.Data.Length);
            });

            if (Mode == MSTCP_FLAGS.MSTCP_FLAG_TUNNEL || Mode == MSTCP_FLAGS.MSTCP_FLAG_SENT_TUNNEL)
                _ndisapi.SendPacketsToAdapter(_adapter.Handle, _buffer, toAdapter);

            if (Mode == MSTCP_FLAGS.MSTCP_FLAG_TUNNEL || Mode == MSTCP_FLAGS.MSTCP_FLAG_RECV_TUNNEL)
                _ndisapi.SendPacketsToMstcp(_adapter.Handle, _buffer, toMstcp);
        }

        protected virtual void HandlePacket(RawPacket rawPacket)
        {
            if (rawPacket.DeviceFlags == PACKET_FLAG.PACKET_FLAG_ON_RECEIVE)
            {
                if (Mode == MSTCP_FLAGS.MSTCP_FLAG_TUNNEL || Mode == MSTCP_FLAGS.MSTCP_FLAG_RECV_TUNNEL)
                    _ndisapi.SendPacketToMstcp(_adapter.Handle, rawPacket);

                Interlocked.Increment(ref _packetsReceived);
                Interlocked.Add(ref _bytesReceived, (ulong)rawPacket.Data.Length);
            }
            else
            {
                if (Mode == MSTCP_FLAGS.MSTCP_FLAG_TUNNEL || Mode == MSTCP_FLAGS.MSTCP_FLAG_SENT_TUNNEL)
                    _ndisapi.SendPacketToAdapter(_adapter.Handle, rawPacket);


                Interlocked.Increment(ref _packetsSent);
                Interlocked.Add(ref _bytesSent, (ulong)rawPacket.Data.Length);
            }
        }

        public virtual bool Start()
        {
            ThrowIfDisposed();
            if (Capturing) return false;
            if (IsOccupied(_adapter)) return false;

            ResetCancellationToken();

            bool success = true;
            success &= _ndisapi.SetPacketEvent(_adapter.Handle, _packetEvent);
            success &= _ndisapi.SetAdapterMode(_adapter.Handle, Mode);

            if (success) _interceptingTask = Task.Factory.StartNew(FilterWork, _cancellationTokenSource.Token).ContinueWith(t => { if (t.IsFaulted) Stop(); });
            else Stop();
            return success;
        }

        public void WaitForExit()
        {
            ThrowIfDisposed();
            _interceptingTask?.Wait();
        }

        public void WaitForExit(int msTimeout)
        {
            ThrowIfDisposed();
            _interceptingTask?.Wait(msTimeout);
        }

        public virtual bool Stop()
        {
            ThrowIfDisposed();
            if (!Capturing) return false;

            _cancellationTokenSource.Cancel();

            return _ndisapi.SetPacketEvent(_adapter.Handle, null) & _ndisapi.SetAdapterMode(_adapter.Handle, 0);
        }

        private void ResetCancellationToken()
        {
            if (Capturing) _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = new();
        }

        public new void Dispose()
        {
            ThrowIfDisposed();

            Stop();
            WaitForExit();
            _buffer.Dispose();
            _packetEvent.Dispose();
            _ndisapi.SetPacketEvent(_adapter.Handle, null);
            _ndisapi.SetAdapterMode(_adapter.Handle, 0);
            UnregisterUserFromAdapter(_adapter, this);
            base.Dispose();
        }
    }
}
