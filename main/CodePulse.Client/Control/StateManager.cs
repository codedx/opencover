using System;
using System.Collections.Generic;
using CodePulse.Client.Errors;
using CodePulse.Client.Message;

namespace CodePulse.Client.Control
{
    public class StateManager
    {
        private readonly IErrorHandler _errorHandler;
        private readonly IList<IModeChangeListener> _listeners = new List<IModeChangeListener>();

        private readonly StateManagerControlMessageHandler _messageHandler;

        public AgentOperationMode CurrentMode { get; private set; }

        public IControlMessageHandler ControlMessageHandler => _messageHandler;

        public StateManager(IErrorHandler errorHandler)
        {
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            _messageHandler = new StateManagerControlMessageHandler(this);
        }

        public void AddListener(IModeChangeListener listener)
        {
            lock (_listeners)
            {
                _listeners.Add(listener);
            }
        }

        public void TriggerShutdown()
        {
            TriggerModeChange(AgentOperationMode.Shutdown);
        }

        private void TriggerModeChange(AgentOperationMode newMode)
        {
            if (CurrentMode == AgentOperationMode.Shutdown)
            {
                return;
            }

            var oldMode = CurrentMode;
            CurrentMode = newMode;

            lock (_listeners)
            {
                foreach (var listener in _listeners)
                {
                    listener.OnModeChange(oldMode, newMode);
                }
            }
        }

        private class StateManagerControlMessageHandler : IControlMessageHandler
        {
            private readonly StateManager _stateManager;
            private bool _initSuspended;

            public StateManagerControlMessageHandler(StateManager stateManager)
            {
                _stateManager = stateManager;
                _stateManager.CurrentMode = AgentOperationMode.Initializing;
            }

            public void OnStart()
            {
                _stateManager.TriggerModeChange(_initSuspended ? AgentOperationMode.Suspended : AgentOperationMode.Tracing);
            }

            public void OnStop()
            {
                _stateManager.TriggerModeChange(AgentOperationMode.Shutdown);
            }

            public void OnPause()
            {
                if (_stateManager.CurrentMode != AgentOperationMode.Tracing)
                { 
                    _stateManager._errorHandler.HandleError("Pause control message is only valid when tracing.", null);
                    return;
                }
                _stateManager.TriggerModeChange(AgentOperationMode.Paused);
            }

            public void OnUnpause()
            {
                if (_stateManager.CurrentMode == AgentOperationMode.Paused)
                {
                    _stateManager.TriggerModeChange(AgentOperationMode.Tracing);
                    return;
                }

                if (_stateManager.CurrentMode != AgentOperationMode.Shutdown)
                {
                    _stateManager._errorHandler.HandleError("Unpause control message is only valid when paused.", null);
                }
            }

            public void OnSuspend()
            {
                switch (_stateManager.CurrentMode)
                {
                    case AgentOperationMode.Initializing:
                        _initSuspended = true;
                        break;
                    case AgentOperationMode.Tracing:
                        _stateManager.TriggerModeChange(AgentOperationMode.Suspended);
                        break;
                    default:
                        _stateManager._errorHandler.HandleError("Suspend control message is only valid when tracing or initializing.", null);
                        break;
                }
            }

            public void OnUnsuspend()
            {
                switch (_stateManager.CurrentMode)
                {
                    case AgentOperationMode.Initializing:
                        _initSuspended = false;
                        return;
                    case AgentOperationMode.Suspended:
                        _stateManager.TriggerModeChange(AgentOperationMode.Tracing);
                        return;
                }

                if (_stateManager.CurrentMode != AgentOperationMode.Shutdown)
                {
                    _stateManager._errorHandler.HandleError("Unsuspend control message is only valid when suspended or initializing.", null);
                }
            }

            public void OnError(string error)
            {
                _stateManager._errorHandler.HandleError($"Error received from HQ ({error}", null);
            }
        }
    }
}
