using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CodePulse.Client.Errors;
using CodePulse.Client.Instrumentation.Id;
using CodePulse.Client.Message;
using CodePulse.Client.Trace;
using log4net;

namespace CodePulse.Client.Data
{
    public class TraceDataCollector : ITraceDataCollector
    {
        private readonly ILog _logger;
        private readonly IErrorHandler _errorHandler;
        private readonly IMessageProtocol _messageProtocol;
        private readonly BufferService _bufferService;
        private readonly ClassIdentifier _classIdentifier;
        private readonly MethodIdentifier _methodIdentifier;

        private readonly MethodIdAdapter _methodIdAdapter;

        private readonly DateTime _startTime = DateTime.UtcNow;

        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly BlockingCollection<ITraceMessage> _traceMessages = new BlockingCollection<ITraceMessage>(new ConcurrentQueue<ITraceMessage>());
        private readonly Task _task;

        private int _sequenceId;

        public int SequenceId => _sequenceId;

        public TraceDataCollector(IMessageProtocol messageProtocol,
            BufferService bufferService,
            ClassIdentifier classIdentifier,
            MethodIdentifier methodIdentifier,
            IErrorHandler errorHandler,
            ILog logger)
        {
            _messageProtocol = messageProtocol ?? throw new ArgumentNullException(nameof(messageProtocol));
            _bufferService = bufferService ?? throw new ArgumentNullException(nameof(bufferService));
            _classIdentifier = classIdentifier ?? throw new ArgumentNullException(nameof(classIdentifier));
            _methodIdentifier = methodIdentifier ?? throw new ArgumentNullException(nameof(methodIdentifier));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _methodIdAdapter = new MethodIdAdapter(this);

            _task = Task.Run(() => ReadTraceMessages());
        }

        public void AddMethodVisit(string className, string sourceFile, string methodName, string methodSignature, int startLineNumber,
            int endLineNumber)
        {
            if (_task.Status != TaskStatus.Running)
            {
                _logger.Warn($"Ignoring method visit for {className}.{methodName} ({methodSignature}) because trace data collector is not running.");
                return;
            }

            _traceMessages.Add(new MethodVisitTraceMessage(
                className,
                sourceFile,
                methodName,
                methodSignature,
                startLineNumber,
                endLineNumber));
        }

        public void Shutdown()
        {
            _traceMessages.CompleteAdding();

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

                    _errorHandler.HandleError("Exception occurred when stopping trace data collector.", ex);
                    return true;
                });
            }
            finally
            {
                _traceMessages.Dispose();
            }
        }

        private void ReadTraceMessages()
        {
            while (!_traceMessages.IsCompleted && !_cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    var traceMessage = _traceMessages.Take(_cancellationTokenSource.Token);
                    if (!(traceMessage is MethodVisitTraceMessage))
                    {
                        throw new InvalidOperationException($"Detected unknown trace message of type {traceMessage.GetType().FullName}.");
                    }

                    var methodVisitTraceMessage = (MethodVisitTraceMessage) traceMessage;
                    MethodEntry(methodVisitTraceMessage.ClassName,
                        methodVisitTraceMessage.SourceFile,
                        methodVisitTraceMessage.MethodName,
                        methodVisitTraceMessage.MethodSignature,
                        methodVisitTraceMessage.StartLineNumber,
                        methodVisitTraceMessage.EndLineNumber);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    _errorHandler.HandleError("Unexpected error occurred while reading from trace messages queue.", ex);
                }
            }
        }

        private void MethodEntry(string className, string sourceFile, string methodName, string methodSignature,
            int startLineNumber, int endLineNumber)
        {
            var classId = _classIdentifier.Record(className, sourceFile);
            var methodId = _methodIdentifier.Record(classId, methodName, methodSignature, startLineNumber, endLineNumber);

            _logger.Debug($"MethodEntry: {methodSignature} ({methodId})");

            try
            {
                MethodActivity(methodId, null, (writer, timestamp, nextSequenceId, methodIdentifier, threadId, sourceLineNumber) =>
                {
                    if (sourceLineNumber.HasValue)
                    {
                        throw new InvalidOperationException();
                    }
                    _messageProtocol.WriteMethodEntry(writer, timestamp, nextSequenceId, methodIdentifier, threadId);
                });
            }
            catch (Exception ex)
            {
                _errorHandler.HandleError("Error sending method entry.", ex);
            }
        }

        private void SendMapMethodSignature(string signature, int id)
        {
            var buffer = _bufferService.ObtainBuffer();
            if (buffer == null)
            {
                return;
            }
            var writer = new BinaryWriter(buffer);
            var wrote = false;
            var bufferStartPosition = buffer.Position;
            try
            {
                _logger.Debug($"SendMapMethodSignature: {signature} ({id})");
                _messageProtocol.WriteMapMethodSignature(writer, id, signature);
                wrote = true;
            }
            finally
            {
                if (!wrote)
                {
                    buffer.Position = bufferStartPosition;
                }
                _bufferService.RelinquishBuffer(buffer);
            }
        }

        private void MethodActivity(int methodId, ushort? sourceLine, Action<BinaryWriter, int, int, int, ushort, ushort?> methodAction)
        {
            var buffer = _bufferService.ObtainBuffer();
            if (buffer == null)
            {
                return;
            }
            var writer = new BinaryWriter(buffer);
            var wrote = false;
            var bufferStartPosition = buffer.Position;
            try
            {
                var nextSequenceId = GetNextSequenceId();
                var timestamp = GetTimeOffsetInMilliseconds();
                const ushort threadId = 1;

                _methodIdAdapter.Mark(methodId);
                methodAction(writer, timestamp, nextSequenceId, methodId, threadId, sourceLine);
                wrote = true;
            }
            finally
            {
                if (!wrote)
                {
                    buffer.Position = bufferStartPosition;
                }
                _bufferService.RelinquishBuffer(buffer);
            }
        }

        private int GetNextSequenceId()
        {
            return _sequenceId++;
        }

        private int GetTimeOffsetInMilliseconds()
        {
            return (int)DateTime.UtcNow.Subtract(_startTime).TotalMilliseconds;
        }

        private class MethodIdAdapter
        {
            private readonly TraceDataCollector _traceDataCollector;
            private readonly ConcurrentDictionary<int, bool> _observedIds = new ConcurrentDictionary<int, bool>();

            public MethodIdAdapter(TraceDataCollector traceDataCollector)
            {
                _traceDataCollector = traceDataCollector;
            }

            public void Mark(int methodId)
            {
                var added = _observedIds.TryAdd(methodId, true);
                if (!added)
                {
                    return;
                }

                var methodInformation = _traceDataCollector._methodIdentifier.Lookup(methodId);
                if (methodInformation == null)
                {
                    throw new ArgumentException($"Unable to find method information for method ID {methodId}.", nameof(methodId));
                }

                _traceDataCollector.SendMapMethodSignature(methodInformation.Signature, methodId);
            }
        }
    }
}
