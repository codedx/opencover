using System;
using System.Net.Sockets;

namespace CodePulse.Client.Util
{
    public class SocketFactory
    {
        public int Port { get; }

        public int RetryDurationInMilliseconds { get; }

        public string Host { get; }

        public SocketFactory(string host, int port, int retryDurationInMilliseconds)
        {
            Host = host ?? throw new ArgumentNullException(nameof(host));
            if (port <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(port));
            }
            if (retryDurationInMilliseconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(retryDurationInMilliseconds));
            }
            Port = port;
            RetryDurationInMilliseconds = retryDurationInMilliseconds;
        }

        public Socket Connect()
        {
            var now = DateTime.UtcNow;
            var timeoutExpires = now.AddMilliseconds(RetryDurationInMilliseconds);

            Socket socket;
            do
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    socket.Connect(Host, Port);
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
