using System;
using System.IO;
using System.Threading;

namespace CodePulse.Client.Message
{
    public abstract class BufferService
    {
        private readonly object _pauseObject = new object();

        private volatile bool _paused;
        private volatile bool _suspended;

        public bool IsPaused => _paused;

        public bool IsSuspended => _suspended;

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
    }
}
