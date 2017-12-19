using System;
using System.Threading;
using System.Threading.Tasks;
using CodePulse.Client.Config;
using CodePulse.Client.Connect;
using CodePulse.Client.Control;
using CodePulse.Client.Data;
using CodePulse.Client.Errors;
using CodePulse.Client.Init;
using CodePulse.Client.Instrumentation.Id;
using CodePulse.Client.Message;
using CodePulse.Client.Protocol;
using CodePulse.Client.Queue;
using CodePulse.Client.Trace;
using CodePulse.Client.Util;

namespace CodePulse.Client.Agent
{
    public class DefaultTraceAgent : ITraceAgent
    {
        private SocketFactory _socketFactory;

        private readonly IMessageProtocol _messageProtocol;
        private readonly IProtocolVersion _protocolVersion;

        private readonly StateManager _stateManager;

        private BufferPool _bufferPool;
        private BufferService _bufferService;
        private Controller _controller;

        private readonly HeartbeatInformer _heartbeatInformer;
        private readonly ConfigurationHandler _configurationHandler;

        private readonly ManualResetEvent _startEvent = new ManualResetEvent(false);

        private readonly IErrorHandler _errorHandler = new ErrorHandler();

        private MessageSenderManager _messageSenderManager;

        public StaticAgentConfiguration StaticAgentConfiguration { get; }

        public RuntimeAgentConfiguration RuntimeAgentConfiguration { get; private set; }

        public ClassIdentifier ClassIdentifier { get; } = new ClassIdentifier();
        public MethodIdentifier MethodIdentifier { get; } = new MethodIdentifier();

        public ITraceDataCollector TraceDataCollector { get; private set; }

        public bool IsKilled { get; private set; }

        public DefaultTraceAgent(StaticAgentConfiguration staticAgentConfiguration)
        {
            StaticAgentConfiguration = staticAgentConfiguration ?? throw new ArgumentNullException(nameof(staticAgentConfiguration));

            _protocolVersion = new ProtocolVersion(_errorHandler);
            _messageProtocol = _protocolVersion.MessageProtocol;

            _heartbeatInformer = new HeartbeatInformer(this);
            _configurationHandler = new ConfigurationHandler(this);

            _stateManager = new StateManager(_errorHandler);
            _stateManager.AddListener(new ModeChangeListener(this));

            _errorHandler.ErrorOccurred += (sender, args) =>
            {
                StaticAgentConfiguration.Logger.Error(args.Item1);
            };
        }

        public bool Connect()
        {
            SocketConnection socketConnection;
            try
            {
                _socketFactory = new SocketFactory(StaticAgentConfiguration.HqHost,
                    StaticAgentConfiguration.HqPort,
                    StaticAgentConfiguration.ConnectTimeout);

                var socket = _socketFactory.Connect();
                if (socket == null)
                {
                    return false;
                }

                socketConnection = new SocketConnection(socket);
            }
            catch (Exception ex)
            {
                const string failedToConnectToHq = "Failed to connect to HQ.";

                _errorHandler.HandleError(failedToConnectToHq, ex);
                throw new ApplicationException($"Agent Connection Error: {failedToConnectToHq}", ex);
            }

            try
            {
                try
                {
                    RuntimeAgentConfiguration = _protocolVersion.ControlConnectionHandshake.PerformHandshake(socketConnection);
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
                    _errorHandler.HandleError("Unable to perform control connection handshake.", ex);

                    return false;
                }

                _controller = new Controller(socketConnection,
                    _protocolVersion,
                    RuntimeAgentConfiguration.HeartbeatInterval,
                    _stateManager.ControlMessageHandler,
                    _configurationHandler,
                    _heartbeatInformer,
                    _errorHandler,
                    StaticAgentConfiguration.Logger);
            }
            catch (Exception ex)
            {
                const string failedToGetConfigurationFromHq = "Failed to get configuration from HQ.";

                _errorHandler.HandleError(failedToGetConfigurationFromHq, ex);
                throw new ApplicationException($"Agent Connection Error: {failedToGetConfigurationFromHq}", ex);
            }

            try
            {
                _controller.Start();
            }
            catch (Exception ex)
            {
                const string failedToStartTheAgentController = "Failed to start the agent controller.";

                _errorHandler.HandleError(failedToStartTheAgentController, ex);
                throw new ApplicationException($"Agent Connection Error: {failedToStartTheAgentController}", ex);
            }

            _errorHandler.ErrorOccurred += (sender, args) => { KillTrace(args.Item1); };
            return true; 
        }

        public bool Prepare()
        {
            try
            {
                var memBudget = RuntimeAgentConfiguration.BufferMemoryBudget;
                var bufferLength = DecideBufferLength(memBudget);
                var numBuffers = memBudget / bufferLength;

                _bufferPool = new BufferPool(numBuffers, bufferLength);
                _bufferService = new PooledBufferService(_bufferPool, RuntimeAgentConfiguration.QueueRetryCount, StaticAgentConfiguration.Logger);
                TraceDataCollector = new TraceDataCollector(_messageProtocol, _bufferService, ClassIdentifier, MethodIdentifier, _errorHandler, StaticAgentConfiguration.Logger);

                _messageSenderManager = new MessageSenderManager(_socketFactory,
                    _protocolVersion.DataConnectionHandshake,
                    _bufferPool,
                    RuntimeAgentConfiguration.NumDataSenders,
                    RuntimeAgentConfiguration.RunId,
                    _errorHandler,
                    StaticAgentConfiguration.Logger);

                if (!_messageSenderManager.Start())
                {
                    return false;
                }
                _stateManager.AddListener(_bufferService.ModeChangeListener);

                return true;
            }
            catch (Exception e)
            {
                _errorHandler.HandleError("Error initializing trace agent and connecting to HQ.", e);

                throw new ApplicationException("Agent Initialization Error: Failed to set up message sending system", e);
            }
        }

        public void Start()
        {
            _startEvent.Set();
        }

        public void KillTrace(string errorMessage)
        {
            if (IsKilled)
            {
                return;
            }

            try
            {
                _controller.SendError(errorMessage);
            }
            catch (Exception ex)
            {
                _errorHandler.HandleError("Exception occurred while sending error message.", ex);
            }

            if (_bufferService != null)
            {
                _bufferService.SetSuspended(true);
                _bufferService.SetPaused(false);
            }

            Shutdown();

            IsKilled = true;
        }

        public void Shutdown()
        {
            _stateManager.TriggerShutdown();
        }

        public void ShutdownAndWait()
        {
            Shutdown();
            WaitForSenderManager();
        }

        public void CloseConnections()
        {
            _messageSenderManager.Shutdown();
            _controller.Shutdown();
        }

        public void WaitForStart()
        {
            _startEvent.WaitOne();
        }

        private void WaitForSenderManager()
        {
            var sleepInterval = RuntimeAgentConfiguration.HeartbeatInterval;
            do
            {
                try
                {
                    Thread.Sleep(sleepInterval);
                }
                catch (Exception ex)
                {
                    _errorHandler.HandleError("Exception occurred while waiting for send queue to empty.", ex);
                }
            }
            while (!IsKilled && (!_messageSenderManager.IsIdle || _bufferPool.ReadableBuffers > 0));
        }

        private int DecideBufferLength(int memBudget)
        {
            var len = 8192;
            var minBuffers = 10;
            while (memBudget / len < minBuffers)
            {
                if (len < 128)
                {
                    throw new ArgumentException("Agent's memory budget is too low to accomodate enough sending buffer space");
                }
                len /= 2;
            }
            return len;
        }

        private class HeartbeatInformer : IHeartbeatInformer
        {
            private readonly DefaultTraceAgent _agent;
            public AgentOperationMode OperationMode => _agent._stateManager.CurrentMode;

            public int SendQueueSize
            {
                get
                {
                    var agentBufferPool = _agent._bufferPool;
                    return agentBufferPool?.ReadableBuffers ?? 0;
                }
            }

            public HeartbeatInformer(DefaultTraceAgent agent)
            {
                _agent = agent;
            }
        }

        private class ConfigurationHandler : IConfigurationHandler
        {
            private readonly DefaultTraceAgent _agent;
            
            public ConfigurationHandler(DefaultTraceAgent agent)
            {
                _agent = agent;
            }

            public void OnConfig(RuntimeAgentConfiguration config)
            {
                if (_agent._stateManager.CurrentMode != AgentOperationMode.Initializing)
                {
                    throw new InvalidOperationException("Reconfiguration is only valid while agent is running");
                }

                _agent.RuntimeAgentConfiguration = config;
                _agent._controller.SetHeartbeatInterval(config.HeartbeatInterval);
            }
        }

        private class ModeChangeListener : IModeChangeListener
        {
            private readonly DefaultTraceAgent _agent;

            public ModeChangeListener(DefaultTraceAgent agent)
            {
                _agent = agent;
            }

            public void OnModeChange(AgentOperationMode oldMode, AgentOperationMode newMode)
            {
                if (oldMode == AgentOperationMode.Initializing && newMode != AgentOperationMode.Shutdown)
                {
                    _agent.Start();
                }
                else if (newMode == AgentOperationMode.Shutdown)
                {
                    Task.Run(() =>
                    {
                        _agent.WaitForSenderManager();
                        _agent.CloseConnections();
                    });
                    
                }
                else if (newMode == AgentOperationMode.Suspended)
                {
                    try
                    {
                        _agent._controller.SendDataBreak(_agent.TraceDataCollector.SequenceId);
                    }
                    catch (Exception ex)
                    {
                        _agent._errorHandler.HandleError("Error sending data break message.", ex);
                    }
                }
            }
        }
    }
}
