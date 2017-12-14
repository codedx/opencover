using System;
using System.IO;
using System.Threading;
using CodePulse.Client.Control;

namespace CodePulse.Client.Message
{
    public abstract class BufferService
    {
        private readonly object _pauseObject = new object();

        private volatile bool _paused;
        private volatile bool _suspended;

        public IModeChangeListener ModeChangeListener { get; }

        protected BufferService()
        {
            ModeChangeListener = new ModeListener(this);
        }

        public MemoryStream ObtainBuffer()
        {
            BlockWhilePaused();

            return _suspended ? null : OnObtainBuffer();
        }

        public void RelinquishBuffer(MemoryStream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            OnRelinquishBuffer(stream);
        }

        public void SetPaused(bool paused)
        {
            _paused = paused;

            if (_paused)
            {
                return;
            }

            lock (_pauseObject)
            {
                Monitor.PulseAll(_pauseObject);
            }
        }

        public virtual void SetSuspended(bool suspended)
        {
            _suspended = suspended;
        }

        protected abstract MemoryStream OnObtainBuffer();

        protected abstract void OnRelinquishBuffer(MemoryStream stream);

        private void BlockWhilePaused()
        {
            while (_paused)
            {
                lock (_pauseObject)
                {
                    Monitor.Wait(_pauseObject);
                }
            }
        }

        private class ModeListener : IModeChangeListener
        {
            private readonly BufferService _bufferService;

            public ModeListener(BufferService bufferService)
            {
                _bufferService = bufferService;
            }

            public void OnModeChange(AgentOperationMode oldMode, AgentOperationMode newMode)
            {
                switch (newMode)
                {
                    case AgentOperationMode.Paused:
                        _bufferService.SetPaused(true);
                        break;
                    case AgentOperationMode.Suspended:
                        _bufferService.SetSuspended(true);
                        break;
                    case AgentOperationMode.Tracing:
                        switch (oldMode)
                        {
                            case AgentOperationMode.Paused:
                                _bufferService.SetPaused(false);
                                break;
                            case AgentOperationMode.Suspended:
                                _bufferService.SetSuspended(false);
                                break;
                        }
                        break;
                    case AgentOperationMode.Shutdown:
                        if (_bufferService._paused)
                        {
                            _bufferService.SetPaused(false);
                        }
                        _bufferService.SetSuspended(true);
                        break;
                }
            }
        }
    }
}
