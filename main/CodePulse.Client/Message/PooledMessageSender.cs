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
        private volatile bool _isShutdown;

        private Task _task;

        public bool IsIdle => _isIdle;
        public bool IsShutdown => _isShutdown;

        public PooledMessageSender(BufferPool bufferPool, 
            BinaryWriter binaryWriter, 
            IErrorHandler errorHandler,
            ILog logger)
        {
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            _bufferPool = bufferPool ?? throw new ArgumentNullException(nameof(bufferPool));
            _binaryWriter = binaryWriter ?? throw new ArgumentNullException(nameof(binaryWriter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Start()
        {
            var token = _cancellationTokenSource.Token;
            _task = Task.Run(() =>
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        _isIdle = true;
                        var buffer = _bufferPool.AcquireForReading();
                        if (buffer == null)
                        {
                            // write may be disabled
                            continue;
                        }

                        _isIdle = false;

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
                finally
                {
                    _isShutdown = true;
                }
            }, token);
        }

        public void Shutdown()
        {
            if (_task == null)
            {
                return;
            }

            _cancellationTokenSource.Cancel();

            const int waitTime = 5000;
            if (!_task.Wait(waitTime))
            {
                _logger.Warn($"After waiting {waitTime} milliseconds, gave up waiting for message sender to stop.");
            }
        }
    }
}
