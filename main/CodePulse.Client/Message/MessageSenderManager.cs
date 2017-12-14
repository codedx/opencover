using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CodePulse.Client.Connect;
using CodePulse.Client.Errors;
using CodePulse.Client.Init;
using CodePulse.Client.Queue;
using CodePulse.Client.Util;
using log4net;

namespace CodePulse.Client.Message
{
    public class MessageSenderManager
    {
        private readonly SocketFactory _socketFactory;
        private readonly IDataConnectionHandshake _dataConnectionHandshake;
        private readonly BufferPool _bufferPool;
        private readonly byte _runId;
        private readonly ILog _logger;
        private readonly IErrorHandler _errorHandler;

        private readonly int _numSenders;
        private readonly IList<IConnection> _connections = new List<IConnection>();
        private readonly IList<PooledMessageSender> _senders = new List<PooledMessageSender>();

        private bool _started;

        public bool IsIdle
        {
            get
            {
                return !_started || _senders.All(x => x.IsShutdown || x.IsIdle);
            }
        }

        public MessageSenderManager(SocketFactory socketFactory,
            IDataConnectionHandshake dataConnectionHandshake,
            BufferPool bufferPool,
            int numSenders,
            byte runId,
            IErrorHandler errorHandler,
            ILog logger)
        {
            _bufferPool = bufferPool ?? throw new ArgumentNullException(nameof(bufferPool));
            _dataConnectionHandshake = dataConnectionHandshake ?? throw new ArgumentNullException(nameof(dataConnectionHandshake));
            _socketFactory = socketFactory ?? throw new ArgumentNullException(nameof(socketFactory));
            if (numSenders <= 0) throw new ArgumentOutOfRangeException(nameof(numSenders));
            _numSenders = numSenders;
            _runId = runId;
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool Start()
        {
            if (_started)
            {
                return false;
            }
            _started = true;

            try
            {
                for (var i = 0; i < _numSenders; i++)
                {
                    var socket = _socketFactory.Connect();
                    if (socket == null)
                    {
                        throw new ApplicationException("Failed to open HQ Data connection.");
                    }

                    var socketConnection = new SocketConnection(socket);

                    try
                    {
                        _dataConnectionHandshake.PerformHandshake(_runId, socketConnection);
                    }
                    catch (HandshakeException ex)
                    {
                        try
                        {
                            socketConnection.Close();
                        }
                        catch
                        {
                            // ignored
                        }

                        _errorHandler.HandleError("Unable to perform data connection handshake.", ex);
                        return false;
                    }

                    _connections.Add(socketConnection);
                    _senders.Add(new PooledMessageSender(_bufferPool, 
                        socketConnection.OutputWriter,
                        _errorHandler,
                        _logger));
                }
            }
            catch (Exception ex)
            {
                _errorHandler.HandleError("Failed to start the MessageSenderManager.", ex);
                return false;
            }

            if (_connections.Count == _numSenders)
            {
                foreach (var sender in _senders)
                {
                    sender.Start();
                }
                return true;
            }

            foreach (var connection in _connections)
            {
                connection.Close();
            }
            _connections.Clear();
            _senders.Clear();

            return false;
        }

        public void Shutdown()
        {
            foreach (var sender in _senders)
            {
                sender.Shutdown();
            }
            while (_senders.Any(x => !x.IsShutdown))
            {
                Thread.Sleep(50);
            }
            _senders.Clear();

            foreach (var connection in _connections)
            {
                connection.Close();
            }
            _connections.Clear();
        }
    }
}
