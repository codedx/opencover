using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using CodePulse.Client.Config;
using CodePulse.Client.Connect;
using CodePulse.Client.Control;
using CodePulse.Client.Errors;
using CodePulse.Client.Message;
using CodePulse.Client.Protocol;
using CodePulse.Client.Util;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CodePulse.Client.Test
{
    class ConfigurationHandler : IConfigurationHandler
    {
        private readonly ManualResetEventSlim _onConfigHappened;

        public ConfigurationHandler(ManualResetEventSlim onConfigHappened)
        {
            _onConfigHappened = onConfigHappened;
        }
        public void OnConfig(RuntimeAgentConfiguration config)
        {
            _onConfigHappened.Set();
        }
    }

    class ControlMessageHandler : IControlMessageHandler
    {
        private readonly ManualResetEventSlim _onStartHappened;

        public ControlMessageHandler(ManualResetEventSlim onStartHappened)
        {
            _onStartHappened = onStartHappened;
        }

        public void OnStart()
        {
            _onStartHappened.Set();
        }

        public void OnStop()
        {
            throw new NotImplementedException();
        }

        public void OnPause()
        {
            throw new NotImplementedException();
        }

        public void OnUnpause()
        {
            throw new NotImplementedException();
        }

        public void OnSuspend()
        {
            throw new NotImplementedException();
        }

        public void OnUnsuspend()
        {
            throw new NotImplementedException();
        }

        public void OnError(string error)
        {
            throw new NotImplementedException();
        }
    }

    class HeartbeatInformer : IHeartbeatInformer
    {
        public AgentOperationMode OperationMode { get; set; }
        public int SendQueueSize { get; set; }
    }

    [TestClass]
    public class ControllerTests
    {
        [TestMethod]
        public void WhenControllerStartsItProcessesMessages()
        {
            // arrange
            var okayToWriteEvent = new ManualResetEventSlim();
            var closeSocketEvent = new ManualResetEventSlim();
            var onStartHappened = new ManualResetEventSlim();

            var listeningEvent = new ManualResetEventSlim();
            var serverTask = Server.CreateServer(4998, listeningEvent, 
                listener =>
                {
                    var socket = listener.Accept();

                    okayToWriteEvent.Wait();
                    socket.Send(new[] { MessageTypes.Start });
                    closeSocketEvent.Wait();

                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                });

            if (!listeningEvent.Wait(5000))
            {
                Assert.Fail("Expected server to start listening");
            }

            var socketFactory = new SocketFactory("127.0.0.1", 4998, 1);
            var errorHandler = new ErrorHandler();

            // act
            var controller = new Controller(
                new SocketConnection(socketFactory.Connect()),
                new ProtocolVersion(errorHandler),
                10,
                new ControlMessageHandler(onStartHappened),
                new Mock<IConfigurationHandler>().Object,
                new HeartbeatInformer(),
                errorHandler,
                new Mock<ILog>().Object);

            okayToWriteEvent.Set();
            onStartHappened.Wait();

            // assert
            Assert.IsTrue(controller.IsRunning);

            controller.Shutdown();

            closeSocketEvent.Set();
            serverTask.Wait(TimeSpan.FromMilliseconds(5000));
            serverTask.Dispose();
        }

        [TestMethod]
        public void WhenControllerHeartbeatIntervalChangesHeartbeatChanges()
        {
            // arrange
            var closeSocketEvent = new ManualResetEventSlim();
            var listeningEvent = new ManualResetEventSlim();

            var heartbeatTimes = new List<DateTime>();
            var serverTask = Server.CreateServer(4998, listeningEvent, 
                listener =>
                {
                    var socket = listener.Accept();
                    socket.ReceiveTimeout = 1;

                    while (!closeSocketEvent.Wait(TimeSpan.FromMilliseconds(1)))
                    {
                        var buffer = new byte[50];
                        try
                        {
                            var bytesRead = socket.Receive(buffer);
                            if (bytesRead > 0)
                            {
                                if (buffer[0] == MessageTypes.Heartbeat)
                                {
                                    heartbeatTimes.Add(DateTime.UtcNow);
                                }
                                Array.Clear(buffer, 0, buffer.Length);
                            }
                        }
                        catch
                        {
                            // ignored
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

            var socketFactory = new SocketFactory("127.0.0.1", 4998, 1);
            var errorHandler = new ErrorHandler();

            // act
            var controller = new Controller(
                new SocketConnection(socketFactory.Connect()),
                new ProtocolVersion(errorHandler),
                3000,
                new Mock<IControlMessageHandler>().Object,
                new Mock<IConfigurationHandler>().Object,
                new HeartbeatInformer(),
                errorHandler,
                new Mock<ILog>().Object);

            Thread.Sleep(10000);
            controller.SetHeartbeatInterval(200);
            Thread.Sleep(10000);

            controller.Shutdown();

            // assert
            Assert.IsTrue(heartbeatTimes.Count > 4);

            var slowHeartbeatTimeFrequency = heartbeatTimes[2].Subtract(heartbeatTimes[1]).TotalMilliseconds;
            var fastHeartbeatTimeFrequency = heartbeatTimes[heartbeatTimes.Count-1].Subtract(heartbeatTimes[heartbeatTimes.Count-2]).TotalMilliseconds;

            Assert.IsTrue(slowHeartbeatTimeFrequency > 2000 && slowHeartbeatTimeFrequency < 5000);
            Assert.IsTrue(fastHeartbeatTimeFrequency > 200 && fastHeartbeatTimeFrequency < 500);

            closeSocketEvent.Set();
            serverTask.Wait();
            serverTask.Dispose();
        }
    }
}
