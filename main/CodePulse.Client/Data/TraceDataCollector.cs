using System;
using System.Collections.Concurrent;
using System.IO;
using CodePulse.Client.Errors;
using CodePulse.Client.Instrumentation;
using CodePulse.Client.Instrumentation.Id;
using CodePulse.Client.Message;
using CodePulse.Client.Trace;
using Mono.Cecil;

namespace CodePulse.Client.Data
{
    public class TraceDataCollector : ITraceDataCollector
    {
        private readonly IErrorHandler _errorHandler;
        private readonly IMessageProtocol _messageProtocol;
        private readonly BufferService _bufferService;
        private readonly ClassIdentifier _classIdentifier;
        private readonly MethodIdentifier _methodIdentifier;

        private readonly MethodIdAdapter _methodIdAdapter;

        private readonly DateTime _startTime = DateTime.UtcNow;

        private int _sequenceId;

        public int SequenceId => _sequenceId;

        public TraceDataCollector(IMessageProtocol messageProtocol,
            BufferService bufferService,
            ClassIdentifier classIdentifier,
            MethodIdentifier methodIdentifier,
            IErrorHandler errorHandler)
        {
            _messageProtocol = messageProtocol ?? throw new ArgumentNullException(nameof(messageProtocol));
            _bufferService = bufferService ?? throw new ArgumentNullException(nameof(bufferService));
            _classIdentifier = classIdentifier ?? throw new ArgumentNullException(nameof(classIdentifier));
            _methodIdentifier = methodIdentifier ?? throw new ArgumentNullException(nameof(methodIdentifier));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));

            _methodIdAdapter = new MethodIdAdapter(this);
        }

        public void MethodEntry(int methodId)
        {
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

        public int MethodEntry(string className, string sourceFile,
            MethodAttributes attributes, string methodName, string methodSignature,
            int startLineNumber, int endLineNumber)
        {
            var classId = _classIdentifier.Record(className, sourceFile);
            var methodId = _methodIdentifier.Record(classId, CreateMethodAccess(attributes), methodName, methodSignature, startLineNumber, endLineNumber);

            MethodEntry(methodId);

            return methodId;
        }

        public void MethodExit(int methodId, ushort sourceLine)
        {
            try
            {
                MethodActivity(methodId, sourceLine, (writer, timestamp, nextSequenceId, methodIdentifier, threadId, sourceLineNumber) =>
                {
                    if (!sourceLineNumber.HasValue)
                    {
                        throw new InvalidOperationException();
                    }
                    _messageProtocol.WriteMethodExit(writer, timestamp, nextSequenceId, methodIdentifier, threadId, sourceLineNumber.Value);
                });
            }
            catch (Exception ex)
            {
                _errorHandler.HandleError("Error sending method exit.", ex);
            }
        }

        public void SendMapMethodSignature(string signature, int id)
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

        private static MethodAccess CreateMethodAccess(MethodAttributes methodAttributes)
        {
            var methodAccess = Merge(MethodAccess.Default, methodAttributes, MethodAttributes.Public, MethodAccess.AccPublic);

            methodAccess = Merge(methodAccess, methodAttributes, MethodAttributes.Public, MethodAccess.AccPublic);
            methodAccess = Merge(methodAccess, methodAttributes, MethodAttributes.Private, MethodAccess.AccPrivate);
            methodAccess = Merge(methodAccess, methodAttributes, MethodAttributes.Family, MethodAccess.AccProtected);
            methodAccess = Merge(methodAccess, methodAttributes, MethodAttributes.Static, MethodAccess.AccStatic);
            methodAccess = Merge(methodAccess, methodAttributes, MethodAttributes.Final, MethodAccess.AccFinal);
            methodAccess = Merge(methodAccess, methodAttributes, MethodAttributes.Abstract, MethodAccess.AccAbstract);

            return methodAccess;
        }

        private static MethodAccess Merge(MethodAccess access, MethodAttributes attributes, MethodAttributes attributeToTest, MethodAccess accessToTest)
        {
            if ((attributes & attributeToTest) == attributeToTest)
            {
                access |= accessToTest;
            }
            return access;
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
