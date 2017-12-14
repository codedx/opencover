using System;
using System.IO;
using CodePulse.Client.Queue;
using log4net;

namespace CodePulse.Client.Message
{
    public class PooledBufferService : BufferService
    {
        private readonly BufferPool _bufferPool;
        private readonly int _maxObtainTries;
        private readonly ILog _logger;

        public PooledBufferService(BufferPool bufferPool, int maxObtainTries, ILog logger)
        {
            _bufferPool = bufferPool ?? throw new ArgumentNullException(nameof(bufferPool));
            if (maxObtainTries < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxObtainTries));
            }

            _maxObtainTries = maxObtainTries;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override void SetSuspended(bool suspended)
        {
            _bufferPool.SetWriteDisabled(suspended);
            base.SetSuspended(suspended);
        }

        protected override MemoryStream OnObtainBuffer()
        {
            for (var i = 0; i < _maxObtainTries; i++)
            {
                try
                {
                    return _bufferPool.AcquireForWriting();
                }
                catch (Exception ex)
                {
                    _logger.Warn($"Failed to acquire memory stream for writing from buffer pool: {ex.Message}");
                }
            }
            throw new FailedToObtainBufferException("Too many retries");
        }

        protected override void OnRelinquishBuffer(MemoryStream stream)
        {
            _bufferPool.Release(stream);
        }
    }

    public class FailedToObtainBufferException : Exception
    {
        public FailedToObtainBufferException()
        {
        }

        public FailedToObtainBufferException(string message) : base(message)
        {
        }

        public FailedToObtainBufferException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
