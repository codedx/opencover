using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace CodePulse.Client.Test
{
    public class Server
    {
        public static Task CreateServer(int port, 
            ManualResetEventSlim listeningEvent,
            params Action<Socket>[] acceptActions)
        {
            return Task.Run(() =>
            {
                var localAddress = IPAddress.Parse("127.0.0.1");
                var localEndPoint = new IPEndPoint(localAddress, port);
                var listener = new Socket(localAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                try
                {
                    listener.Bind(localEndPoint);
                    listener.Listen(acceptActions.Length);

                    listeningEvent.Set();

                    var tasks = acceptActions.Select(
                        acceptAction => new Task(() =>
                        {
                            acceptAction(listener);

                        })).ToArray();

                    foreach (var task in tasks)
                    {
                        task.Start();
                    }

                    Task.WaitAll(tasks);
                }
                finally
                {
                    listener.Close();
                }
            });
        }
    }
}
