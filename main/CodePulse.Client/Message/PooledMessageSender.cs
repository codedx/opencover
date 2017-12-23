using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CodePulse.Client.Errors;
using CodePulse.Client.Queue;
using CodePulse.Client.Util;
using log4net;

namespace CodePulse.Client.Message
{
    public class PooledMessageSender
    {
        private readonly ILog _logger;
        private readonly IErrorHandler _errorHandler;
        private readonly BinaryWriter _binaryWriter;
        private readonly BufferPool _bufferPool;

        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private volatile bool _isIdle;

        private TimeSpan ReadTimeout { get; }

        private readonly Task _task;

        public bool IsIdle => _isIdle;
        public bool IsShutdown => _task.IsCanceled || _task.IsCompleted || _task.IsFaulted;

        public PooledMessageSender(BufferPool bufferPool, 
            BinaryWriter binaryWriter, 
            IErrorHandler errorHandler,
            ILog logger)
        {
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            _bufferPool = bufferPool ?? throw new ArgumentNullException(nameof(bufferPool));
            _binaryWriter = binaryWriter ?? throw new ArgumentNullException(nameof(binaryWriter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _isIdle = true;

            ReadTimeout = TimeSpan.FromMilliseconds(1000);

            _task = Task.Run(() => SendMessages());
        }

        public void Shutdown()
        {
            try
            {
                _cancellationTokenSource.Cancel();
                _task.Wait();
            }
            catch (AggregateException aex)
            {
                aex.Handle(ex =>
                {
                    if (!(ex is TaskCanceledException))
                    {
                        return false;
                    }

                    _errorHandler.HandleError("Exception occurred when stopping pooled message sender.", ex);
                    return true;
                });
            }
        }

        private void SendMessages()
        {
            try
            {
                while (!_cancellationTokenSource.IsCancellationRequested)
                {
                    var buffer = GetBuffer();
                    if (buffer == null)
                    {
                        continue;
                    }

                    try
                    {
                        buffer.WriteTo(_binaryWriter.BaseStream);

                        _binaryWriter.FlushAndLog($"PooledMessageSender.Send {buffer.Length} byte(s)");

                        buffer.Reset();
                    }
                    catch (Exception ex)
                    {
                        _errorHandler.HandleError("Failed to write data buffer.", ex);
                    }
                    finally
                    {
                        _bufferPool.Release(buffer);
                    }
                }

                _logger.Info("Message sender received token cancellation request.");
            }
            catch (Exception ex)
            {
                _errorHandler.HandleError("PooledMessageSender failed and will no longer process messages.", ex);
            }
        }

        private MemoryStream GetBuffer()
        {
            _isIdle = true;

            var buffer = _bufferPool.AcquireForReading(ReadTimeout);
            _isIdle = buffer == null;

            return buffer;
        }
    }
}
