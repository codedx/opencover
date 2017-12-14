using System;
using System.Net.Sockets;

namespace CodePulse.Client.Util
{
    public class SocketFactory
    {
        private readonly int _port;
        private readonly int _retryDurationInMilliseconds;
        private readonly string _host;

        public SocketFactory(string host, int port, int retryDurationInMilliseconds)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            if (port <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(port));
            }
            if (retryDurationInMilliseconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(retryDurationInMilliseconds));
            }
            _port = port;
            _retryDurationInMilliseconds = retryDurationInMilliseconds;
        }

        public Socket Connect()
        {
            var now = DateTime.UtcNow;
            var timeoutExpires = now.AddMilliseconds(_retryDurationInMilliseconds);

            Socket socket;
            do
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    socket.Connect(_host, _port);
                }
                catch
                {
                    socket.Dispose();
                    socket = null;
                }
            }
            while (socket == null && DateTime.UtcNow < timeoutExpires);

            return socket;
        }
    }
}
