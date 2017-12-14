using System;
using System.IO;
using CodePulse.Client.Util;

namespace CodePulse.Client.Message
{
    public class MessageProtocol : IMessageProtocol
    {
        public byte ProtocolVersion { get; } = 2;

        public void WriteHello(BinaryWriter writer)
        {
            writer.Write(MessageTypes.Hello);
            writer.Write(ProtocolVersion);
        }

        public void WriteDataHello(BinaryWriter writer, byte runId)
        {
            writer.Write(MessageTypes.DataHello);
            writer.Write(runId);
        }

        public void WriteError(BinaryWriter writer, string error)
        {
            writer.Write(MessageTypes.Error);
            writer.WriteUtfBigEndian(error);
        }

        public void WriteConfiguration(BinaryWriter writer, string configJson)
        {
            writer.Write(MessageTypes.Configuration);
            writer.WriteUtfBigEndian(configJson);
        }

        public void WriteDataHelloReply(BinaryWriter writer)
        {
            writer.Write(MessageTypes.DataHelloReply);
        }

        public void WriteStart(BinaryWriter writer)
        {
            writer.Write(MessageTypes.Start);
        }

        public void WriteStop(BinaryWriter writer)
        {
            writer.Write(MessageTypes.Stop);
        }

        public void WritePause(BinaryWriter writer)
        {
            writer.Write(MessageTypes.Pause);
        }

        public void WriteUnpause(BinaryWriter writer)
        {
            writer.Write(MessageTypes.Unpause);
        }

        public void WriteSuspend(BinaryWriter writer)
        {
            writer.Write(MessageTypes.Suspend);
        }

        public void WriteUnsuspend(BinaryWriter writer)
        {
            writer.Write(MessageTypes.Unsuspend);
        }

        public void WriteHeartbeat(BinaryWriter writer, AgentOperationMode mode, ushort sendBufferSize)
        {
            writer.Write(MessageTypes.Heartbeat);
            switch (mode)
            {
                case AgentOperationMode.Initializing:
                    writer.Write((byte) 73);
                    break;
                case AgentOperationMode.Paused:
                    writer.Write((byte) 80);
                    break;
                case AgentOperationMode.Suspended:
                    writer.Write((byte) 83);
                    break;
                case AgentOperationMode.Tracing:
                    writer.Write((byte) 84);
                    break;
                case AgentOperationMode.Shutdown:
                    writer.Write((byte) 88);
                    break;
                default:
                    throw new InvalidOperationException($"Unable to write heatbeat for unknown AgentOperationMode: {mode}.");
            }
            writer.WriteBigEndian(sendBufferSize);
        }

        public void WriteDataBreak(BinaryWriter writer, int sequenceId)
        {
            writer.Write(MessageTypes.DataBreak);
            writer.WriteBigEndian(sequenceId);
        }

        public void WriteClassTransformed(BinaryWriter writer, string className)
        {
            writer.Write(MessageTypes.ClassTransformed);
            writer.WriteUtfBigEndian(className);
        }

        public void WriteClassTransformFailed(BinaryWriter writer, string className)
        {
            writer.Write(MessageTypes.ClassTransformFailed);
            writer.WriteUtfBigEndian(className);
        }

        public void WriteClassIgnored(BinaryWriter writer, string className)
        {
            writer.Write(MessageTypes.ClassIgnored);
            writer.WriteUtfBigEndian(className);
        }

        public void WriteMapThreadName(BinaryWriter writer, ushort threadId, int relTime, string threadName)
        {
            writer.Write(MessageTypes.MapThreadName);
            writer.WriteBigEndian(threadId);
            writer.WriteBigEndian(relTime);
            writer.WriteUtfBigEndian(threadName);
        }

        public void WriteMapMethodSignature(BinaryWriter writer, int sigId, string signature)
        {
            writer.Write(MessageTypes.MapMethodSignature);
            writer.WriteBigEndian(sigId);
            writer.WriteUtfBigEndian(signature);
        }

        public void WriteMapException(BinaryWriter writer, int excId, string exception)
        {
            writer.Write(MessageTypes.MapException);
            writer.WriteBigEndian(excId);
            writer.WriteUtfBigEndian(exception);
        }

        public void WriteMethodEntry(BinaryWriter writer, int relTime, int seq, int sigId, ushort threadId)
        {
            writer.Write(MessageTypes.MethodEntry);
            writer.WriteBigEndian(relTime);
            writer.WriteBigEndian(seq);
            writer.WriteBigEndian(sigId);
            writer.WriteBigEndian(threadId);
        }

        public void WriteMethodExit(BinaryWriter writer, int relTime, int seq, int sigId, ushort lineNum, ushort threadId)
        {
            writer.Write(MessageTypes.MethodExit);
            writer.WriteBigEndian(relTime);
            writer.WriteBigEndian(seq);
            writer.WriteBigEndian(sigId);
            writer.WriteBigEndian(lineNum);
            writer.WriteBigEndian(threadId);
        }

        public void WriteException(BinaryWriter writer, int relTime, int seq, int sigId, int excId, ushort lineNum, ushort threadId)
        {
            writer.Write(MessageTypes.Exception);
            writer.WriteBigEndian(relTime);
            writer.WriteBigEndian(seq);
            writer.WriteBigEndian(sigId);
            writer.WriteBigEndian(excId);
            writer.WriteBigEndian(lineNum);
            writer.WriteBigEndian(threadId);
        }

        public void WriteExceptionBubble(BinaryWriter writer, int relTime, int seq, int sigId, int excId, ushort threadId)
        {
            writer.Write(MessageTypes.ExceptionBubble);
            writer.WriteBigEndian(relTime);
            writer.WriteBigEndian(seq);
            writer.WriteBigEndian(sigId);
            writer.WriteBigEndian(excId);
            writer.WriteBigEndian(threadId);
        }

        public void WriteMarker(BinaryWriter writer, string key, string value, int relTime, int seq)
        {
            writer.Write(MessageTypes.Marker);
            writer.WriteUtfBigEndian(key);
            writer.WriteUtfBigEndian(value);
            writer.WriteBigEndian(relTime);
            writer.WriteBigEndian(seq);
        }
    }
}
