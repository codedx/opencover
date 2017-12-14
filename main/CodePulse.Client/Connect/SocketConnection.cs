using System;
using System.IO;
using System.Net.Sockets;

namespace CodePulse.Client.Connect
{
    public class SocketConnection : IConnection
    {
        private readonly Socket _socket;

        public BinaryReader InputReader { get; }
        public BinaryWriter OutputWriter { get; }

        public SocketConnection(Socket socket)
        {
            _socket = socket ?? throw new ArgumentNullException(nameof(socket));

            var stream = new BufferedStream(new NetworkStream(socket));
            InputReader = new BinaryReader(stream);
            OutputWriter = new BinaryWriter(stream);
        }

        public void Close()
        {
            InputReader.Close();
            OutputWriter.Close();

            _socket.Close();
        }

        public void SetReceiveTimeout(int timeout)
        {
            _socket.ReceiveTimeout = timeout;
        }
    }
}
