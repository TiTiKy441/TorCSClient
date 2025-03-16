using System.Net.Sockets;
using System.Text;

namespace TorCSClient.Proxy.Control
{

    // Thats really awful
    internal sealed class TorServicePort
    {

        private TcpClient? _tcpClient;
        private NetworkStream? _stream;
        private StreamReader? _reader;
        private StreamWriter? _writer;

        public const string TorAddress = "127.0.0.1";

        public const string ResponseOK = "250 OK";
        public const int BufferSize = 4096;

        private int _port;

        public bool IsUsable { get; private set; } = false;

        public bool IsConnected { get
            {
                return _stream != null && _writer != null && _reader != null;
            } 
        }

        public TorServicePort(int port)
        {
            _port = port;
        }

        public void Connect()
        {
            if (IsConnected) return;
            _tcpClient = new TcpClient()
            {
                ReceiveTimeout = 10000,
                SendTimeout = 10000,
            };
            _tcpClient.Connect(TorAddress, _port);
            _stream = _tcpClient.GetStream();
            _reader = new StreamReader(_stream, Encoding.ASCII, false, BufferSize, true);
            _writer = new StreamWriter(_stream, Encoding.ASCII, BufferSize, true);
            Console.WriteLine("Connected to tor control port");
        }

        public void Disconnect()
        {
            _reader?.Close();
            _reader = null;
            _writer?.Close();
            _writer = null;
            _stream?.Close();
            _stream = null;
            _tcpClient?.Close();
            _tcpClient = null;
            IsUsable = false;
            Console.WriteLine("Disconnected from tor control port");
        }

        public void Shutdown()
        {
            if (!IsUsable) throw new AggregateException("Control port is not authed");
            SendLine("SIGNAL SHUTDOWN");
            Disconnect();
        }

        public bool Authenticate()
        {
            if (!IsConnected) throw new AggregateException("Control port is not connected");
            string resp = SendCommand("AUTHENTICATE");
            if (resp == ResponseOK)
            {
                IsUsable = true;
                return true;
            }
            return false;
        }

        public string ReadLine()
        {
            return _reader.ReadLine();
        }

        public void SendLine(string line)
        {
            if (_stream == null) return;
            _writer.WriteLine(line);
            _writer.Flush();
            Console.WriteLine("Line sent: " + line);
        }

        public string SendCommand(string command)
        {
            if (_stream == null) return string.Empty;
            _writer.WriteLine(command);
            _writer.Flush();
            Console.WriteLine("Command sent: " + command);
            return _reader.ReadLine();
        }
    }
}
