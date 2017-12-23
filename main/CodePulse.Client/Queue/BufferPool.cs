using System;
using System.Collections.Concurrent;
using System.IO;

namespace CodePulse.Client.Queue
{
    public class BufferPool
    {
        private readonly BlockingCollection<MemoryStream> _emptyBuffers;
        private readonly BlockingCollection<MemoryStream> _partialBuffers;
        private readonly BlockingCollection<MemoryStream> _fullBuffers;

        private readonly int _fullThreshold;
        private volatile bool _writeDisabled;

        public int ReadableBuffers => _fullBuffers.Count + _partialBuffers.Count;

        public bool IsEmpty => _emptyBuffers.Count == _emptyBuffers.BoundedCapacity &&
                               _partialBuffers.Count == 0 && _fullBuffers.Count == 0;

        public BufferPool(int numBuffers, int initialBufferCapacity)
        {
            _fullThreshold = (int) (initialBufferCapacity * 0.9);

            _emptyBuffers = new BlockingCollection<MemoryStream>(numBuffers);
            _partialBuffers = new BlockingCollection<MemoryStream>(numBuffers);
            _fullBuffers = new BlockingCollection<MemoryStream>(numBuffers);

            for (var i = 0; i < numBuffers; i++)
            {
                _emptyBuffers.Add(new MemoryStream(initialBufferCapacity));
            }
        }

        public MemoryStream AcquireForWriting()
        {
            while (true)
            {
                if (_writeDisabled)
                {
                    return null;
                }

                if (_partialBuffers.TryTake(out MemoryStream partialStream))
                {
                    return partialStream;
                }

                if (_emptyBuffers.TryTake(out MemoryStream emptyStream, 1))
                {
                    return emptyStream;
                }
            }
        }

        public MemoryStream AcquireForReading(TimeSpan timeout)
        {
            var now = DateTime.UtcNow;
            while (timeout == TimeSpan.MaxValue || DateTime.UtcNow.Subtract(now).TotalMilliseconds < timeout.TotalMilliseconds)
            {
                if (_fullBuffers.TryTake(out MemoryStream fullStream))
                {
                    return fullStream;
                }

                if (_partialBuffers.TryTake(out MemoryStream partialStream, 1))
                {
                    return partialStream;
                }
            }
            return null;
        }

        public void Release(MemoryStream stream)
        {
            var bufferSize = stream.Length;
            if (bufferSize == 0)
            {
                _emptyBuffers.Add(stream);
            }
            else if (bufferSize < _fullThreshold)
            {
                _partialBuffers.Add(stream);
            }
            else
            {
                _fullBuffers.Add(stream);
            }
        }

        public void SetWriteDisabled(bool writeDisabled)
        {
            _writeDisabled = writeDisabled;
        }
    }
}
