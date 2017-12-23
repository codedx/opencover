using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using CodePulse.Client.Agent;
using CodePulse.Client.Config;
using CodePulse.Client.Message;
using CodePulse.Client.Util;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CodePulse.Client.Test
{
    [TestClass]
    public class DefaultTraceAgentTests
    {
        [TestMethod]
        public void WhenConnectionUnavailableTimeoutOccurs()
        {
            // arrange
            var logger = new Mock<ILog>();

            var agent = new DefaultTraceAgent(new StaticAgentConfiguration(4999, "127.0.0.1", 5000, logger.Object));

            // act
            var connected = agent.Connect();

            // assert
            Assert.IsFalse(connected);
        }

        [TestMethod]
        public void WhenConnectionValidReturnsTrue()
        {
            // arrange
            var listeningEvent = new ManualResetEventSlim();
            var closeSocketEvent = new ManualResetEventSlim();

            var serverTask = Server.CreateServer(4999, listeningEvent, 
                listener =>
                {
                    var socket = listener.Accept();

                    // receive data hello and reply with incorrect message
                    var buffer = new byte[256];
                    var bytesReceived = socket.Receive(buffer);
                    if (bytesReceived == 2 && buffer[0] == MessageTypes.Hello)
                    {
                        socket.Send(new[] { MessageTypes.Configuration });

                        using (var memoryStream = new MemoryStream())
                        using (var binaryWriter = new BinaryWriter(memoryStream))
                        {
                            binaryWriter.WriteUtfBigEndian("{\"RunId\":1,\"HeartbeatInterval\":2,\"Exclusions\":[\"Exclusion\"],\"Inclusions\":[\"Inclusion\"],\"BufferMemoryBudget\":3,\"QueueRetryCount\":4,\"NumDataSenders\":5}");
                            binaryWriter.Flush();

                            socket.Send(memoryStream.ToArray());
                        }
                    }

                    closeSocketEvent.Wait();

                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                });

            if (!listeningEvent.Wait(5000))
            {
                Assert.Fail("Expected server to start listening");
            }

            var logger = new Mock<ILog>();

            var agent = new DefaultTraceAgent(new StaticAgentConfiguration(4999, "127.0.0.1", 5000, logger.Object));

            // act
            var connected = agent.Connect();

            // assert
            Assert.IsTrue(connected);

            closeSocketEvent.Set();

            agent.Shutdown();
            agent.WaitForShutdown();

            serverTask.Wait();
            serverTask.Dispose();
        }

        [TestMethod]
        public void WhenConnectionErrorsReturnsFalse()
        {
            // arrange
            var listeningEvent = new ManualResetEventSlim();
            var closeSocketEvent = new ManualResetEventSlim();

            var serverTask = Server.CreateServer(4999, listeningEvent,
                listener =>
                {
                    var socket = listener.Accept();

                    // receive data hello and reply with incorrect message
                    var buffer = new byte[256];
                    var bytesReceived = socket.Receive(buffer);
                    if (bytesReceived == 2 && buffer[0] == MessageTypes.Hello)
                    {
                        socket.Send(new[] { MessageTypes.Error });

                        using (var memoryStream = new MemoryStream())
                        using (var binaryWriter = new BinaryWriter(memoryStream))
                        {
                            binaryWriter.WriteUtfBigEndian("Error");
                            binaryWriter.Flush();

                            socket.Send(memoryStream.ToArray());
                        }
                    }

                    closeSocketEvent.Wait();

                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                });

            if (!listeningEvent.Wait(5000))
            {
                Assert.Fail("Expected server to start listening");
            }

            var logger = new Mock<ILog>();

            var agent = new DefaultTraceAgent(new StaticAgentConfiguration(4999, "127.0.0.1", 5000, logger.Object));

            // act
            var connected = agent.Connect();

            // assert
            Assert.IsFalse(connected);

            logger.Verify(x => x.Error("Unable to perform control connection handshake.: Error", It.IsAny<Exception>()));

            agent.Shutdown();
            agent.WaitForShutdown();

            closeSocketEvent.Set();
            serverTask.Wait();
            serverTask.Dispose();
        }

        [TestMethod]
        public void WhenConfigurationJsonInvalidReturnsFalse()
        {
            // arrange
            var listeningEvent = new ManualResetEventSlim();
            var closeSocketEvent = new ManualResetEventSlim();

            var serverTask = Server.CreateServer(4999, listeningEvent, 
                listener =>
                {
                    var socket = listener.Accept();

                    // receive data hello and reply with incorrect message
                    var buffer = new byte[256];
                    var bytesReceived = socket.Receive(buffer);
                    if (bytesReceived == 2 && buffer[0] == MessageTypes.Hello)
                    {
                        socket.Send(new[] { MessageTypes.Configuration });

                        using (var memoryStream = new MemoryStream())
                        using (var binaryWriter = new BinaryWriter(memoryStream))
                        {
                            binaryWriter.WriteUtfBigEndian("{\"RunId\":1,");
                            binaryWriter.Flush();

                            socket.Send(memoryStream.ToArray());
                        }
                    }

                    closeSocketEvent.Wait();

                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                });

            if (!listeningEvent.Wait(5000))
            {
                Assert.Fail("Expected server to start listening");
            }

            var logger = new Mock<ILog>();

            var agent = new DefaultTraceAgent(new StaticAgentConfiguration(4999, "127.0.0.1", 5000, logger.Object));

            // act
            var connected = agent.Connect();

            // assert
            Assert.IsFalse(connected);

            logger.Verify(x => x.Error("Failed to get configuration from HQ.: Unexpected end when deserializing object. Path 'RunId', line 1, position 11.", It.IsAny<Exception>()));

            agent.Shutdown();
            agent.WaitForShutdown();

            closeSocketEvent.Set();
            serverTask.Wait();
            serverTask.Dispose();
        }

        [TestMethod]
        public void WhenDataConnectionValidForOneSenderReturnsTrue()
        {
            // arrange
            var listeningEvent = new ManualResetEventSlim();
            var closeSocket1Event = new ManualResetEventSlim();
            var closeSocket2Event = new ManualResetEventSlim();

            void DoHelloReply(Socket socket, ManualResetEventSlim closeSocketEvent)
            {
                // receive data hello and reply with incorrect message
                var buffer = new byte[256];
                var bytesReceived = socket.Receive(buffer);
                if (bytesReceived == 2 && buffer[0] == MessageTypes.Hello)
                {
                    socket.Send(new[] { MessageTypes.Configuration });

                    using (var memoryStream = new MemoryStream())
                    using (var binaryWriter = new BinaryWriter(memoryStream))
                    {
                        binaryWriter.WriteUtfBigEndian("{\"RunId\":1,\"HeartbeatInterval\":2,\"Exclusions\":[\"Exclusion\"],\"Inclusions\":[\"Inclusion\"],\"BufferMemoryBudget\":2048,\"QueueRetryCount\":4,\"NumDataSenders\":1}");
                        binaryWriter.Flush();

                        socket.Send(memoryStream.ToArray());
                    }
                }

                do
                {
                    Array.Clear(buffer, 0, buffer.Length);
                    bytesReceived = socket.Receive(buffer);
                }
                while (!closeSocketEvent.Wait(TimeSpan.FromMilliseconds(1)) && bytesReceived >= 1 && buffer[0] == MessageTypes.Heartbeat);
            }

            void DoDataHelloReply(Socket socket)
            {
                // receive data hello and reply
                var buffer = new byte[256];
                var bytesReceived = socket.Receive(buffer);
                if (bytesReceived == 2 && buffer[0] == MessageTypes.DataHello)
                {
                    socket.Send(new[] { MessageTypes.DataHelloReply });
                }
            }

            var replyType = 0;
            var serverTask = Server.CreateServer(4999, listeningEvent, 
                listener =>
                {
                    var socket = listener.Accept();

                    Interlocked.Increment(ref replyType);
                    if (replyType == 1)
                    {
                        DoHelloReply(socket, closeSocket1Event);
                    }
                    else
                    {
                        DoDataHelloReply(socket);
                    }

                    closeSocket1Event.Wait();
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                }, 
                listener =>
                {
                    var socket = listener.Accept();

                    Interlocked.Increment(ref replyType);
                    if (replyType == 1)
                    {
                        DoHelloReply(socket, closeSocket2Event);
                    }
                    else
                    {
                        DoDataHelloReply(socket);
                    }

                    closeSocket2Event.Wait();

                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                });

            if (!listeningEvent.Wait(5000))
            {
                Assert.Fail("Expected server to start listening");
            }

            var logger = new Mock<ILog>();

            var agent = new DefaultTraceAgent(new StaticAgentConfiguration(4999, "127.0.0.1", 5000, logger.Object));

            // act
            var connected = agent.Connect();
            var prepared = agent.Prepare();

            // assert
            Assert.IsTrue(connected);
            Assert.IsTrue(prepared);

            agent.Shutdown();
            agent.WaitForShutdown();

            closeSocket1Event.Set();
            closeSocket2Event.Set();
            serverTask.Wait();
            serverTask.Dispose();
        }
    }
}
