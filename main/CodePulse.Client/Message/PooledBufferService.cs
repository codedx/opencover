using System;
using System.IO;
using CodePulse.Client.Queue;

namespace CodePulse.Client.Message
{
    public class PooledBufferService : BufferService
    {
        private readonly BufferPool _bufferPool;

        public PooledBufferService(BufferPool bufferPool)
        {
            _bufferPool = bufferPool ?? throw new ArgumentNullException(nameof(bufferPool));
        }

        public override void SetSuspended(bool suspended)
        {
            _bufferPool.SetWriteDisabled(suspended);
            base.SetSuspended(suspended);
        }

        protected override MemoryStream OnObtainBuffer()
        {
            return _bufferPool.AcquireForWriting();
        }

        protected override void OnRelinquishBuffer(MemoryStream stream)
        {
            _bufferPool.Release(stream);
        }
    }
}
