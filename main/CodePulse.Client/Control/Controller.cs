﻿using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using CodePulse.Client.Connect;
using CodePulse.Client.Errors;
using CodePulse.Client.Protocol;
using CodePulse.Client.Util;
using log4net;

namespace CodePulse.Client.Control
{
    public class Controller
    {
        private readonly SocketConnection _socketConnection;
        private readonly IProtocolVersion _protocolVersion;
        private int _heartbeatInterval;
        private readonly ILog _logger;
        private readonly IErrorHandler _errorHandler;
        private readonly IControlMessageProcessor _messageProcessor;
        private readonly IHeartbeatInformer _heartbeatInformer;

        private readonly BinaryReader _inputReader;
        private readonly BinaryWriter _outputWriter;

        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private readonly object _sendDataObj = new object();

        private Task _task;

        public bool IsRunning { get; private set; }

        public Controller(SocketConnection socketConnection,
            IProtocolVersion protocolVersion,
            int heartbeatInterval,
            IControlMessageHandler messageHandler,
            IConfigurationHandler configurationHandler,
            IHeartbeatInformer heartbeatInformer,
            IErrorHandler errorHandler,
            ILog logger)
        {
            if (messageHandler == null) throw new ArgumentNullException(nameof(messageHandler));
            if (heartbeatInterval <= 0) throw new ArgumentOutOfRangeException(nameof(heartbeatInterval));

            _socketConnection = socketConnection ?? throw new ArgumentNullException(nameof(socketConnection));
            _protocolVersion = protocolVersion ?? throw new ArgumentNullException(nameof(protocolVersion));
            _heartbeatInterval = heartbeatInterval;
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _heartbeatInformer = heartbeatInformer ?? throw new ArgumentNullException(nameof(heartbeatInformer));
            _messageProcessor = _protocolVersion.GetControlMessageProcessor(messageHandler, configurationHandler);

            _inputReader = _socketConnection.InputReader;
            _outputWriter = _socketConnection.OutputWriter;
        }

        public void Shutdown()
        {
            IsRunning = false;

            if (_task == null)
            {
                return;
            }

            try
            {
                _cancellationTokenSource.Cancel();

                if (!_task.Wait(2000))
                {
                    _errorHandler.HandleError("Gave up waiting for controller to stop.", null);
                }

                _socketConnection.Close();
            }
            catch (Exception ex)
            {
                _errorHandler.HandleError("Exception occurred when stopping agent controller.", ex);
            }
        }

        public void SendError(string errorMessage)
        {
            lock (_sendDataObj)
            {
                _protocolVersion.MessageProtocol.WriteError(_outputWriter, errorMessage);
                _outputWriter.FlushAndLog("SendError");
            }
        }

        public void SendClassTransformed(string className)
        {
            lock (_sendDataObj)
            {
                _protocolVersion.MessageProtocol.WriteClassTransformed(_outputWriter, className);
                _outputWriter.FlushAndLog("SendClassTransformed");
            }
        }

        public void SendClassTransformFailed(string className)
        {
            lock (_sendDataObj)
            {
                _protocolVersion.MessageProtocol.WriteClassTransformFailed(_outputWriter, className);
                _outputWriter.FlushAndLog("SendClassTransformFailed");
            }
        }

        public void SendClassIgnored(string className)
        {
            lock (_sendDataObj)
            {
                _protocolVersion.MessageProtocol.WriteClassIgnored(_outputWriter, className);
                _outputWriter.FlushAndLog("SendClassIgnored");
            }
        }

        public void SendDataBreak(int sequence)
        {
            lock (_sendDataObj)
            {
                _protocolVersion.MessageProtocol.WriteDataBreak(_outputWriter, sequence);
                _outputWriter.FlushAndLog("SendDataBreak");
            }
        }

        public void Start()
        {
            var token = _cancellationTokenSource.Token;
            _task = Task.Run(() =>
            {
                IsRunning = true;
                try
                {
                    var nextHeartbeat = DateTime.UtcNow;
                    while (!token.IsCancellationRequested)
                    {
                        // drain incoming messages before attempting to write
                        do
                        {
                        }
                        while (ProcessIncomingMessage(1));
                        
                        if (DateTime.UtcNow > nextHeartbeat)
                        {
                            SendHeartbeat();
                            nextHeartbeat = DateTime.UtcNow.AddMilliseconds(_heartbeatInterval);
                        }

                        var timeout = Math.Max(nextHeartbeat.Subtract(DateTime.UtcNow).TotalMilliseconds, 1);
                        ProcessIncomingMessage((int) timeout);
                    }

                    _logger.Info("Controller received token cancellation request.");
                }
                catch (Exception ex)
                {
                    if (IsRunning)
                    { 
                        _errorHandler.HandleError("Controller failed and will no longer process messages.", ex);
                    }
                }
                finally
                {
                    IsRunning = false;
                }
            }, token);
        }

        public void SetHeartbeatInterval(int heartbeatInterval)
        {
            if (heartbeatInterval <= 0) throw new ArgumentOutOfRangeException(nameof(heartbeatInterval));
            _heartbeatInterval = heartbeatInterval;
        }

        private void SendHeartbeat()
        {
            var mode = _heartbeatInformer.OperationMode;
            var sendQueueSize = (ushort)Math.Min(_heartbeatInformer.SendQueueSize, UInt16.MaxValue);

            lock (_sendDataObj)
            {
                _protocolVersion.MessageProtocol.WriteHeartbeat(_outputWriter, mode, sendQueueSize);
                _outputWriter.FlushAndLog("SendHeartbeat");
            }
        }

        private bool ProcessIncomingMessage(int timeout)
        {
            if (timeout <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
            }

            _socketConnection.SetReceiveTimeout(timeout);
            try
            {
                _messageProcessor.ProcessIncomingMessage(_inputReader);
                return true;
            }
            catch (IOException e)
            {
                var socketException = e.InnerException as SocketException;
                if (socketException?.SocketErrorCode == SocketError.TimedOut)
                {
                    return false;
                }
                throw;
            }
        }
    }
}
