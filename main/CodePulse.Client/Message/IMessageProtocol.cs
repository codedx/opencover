using System.IO;

namespace CodePulse.Client.Message
{
    public interface IMessageProtocol
    {
        byte ProtocolVersion { get; }

        void WriteHello(BinaryWriter writer);
        void WriteDataHello(BinaryWriter writer, byte runId);
        void WriteError(BinaryWriter writer, string error);
        void WriteConfiguration(BinaryWriter writer, string configJson);
        void WriteDataHelloReply(BinaryWriter writer);
        void WriteStart(BinaryWriter writer);
        void WriteStop(BinaryWriter writer);
        void WritePause(BinaryWriter writer);
        void WriteUnpause(BinaryWriter writer);
        void WriteSuspend(BinaryWriter writer);
        void WriteUnsuspend(BinaryWriter writer);
        void WriteHeartbeat(BinaryWriter writer, AgentOperationMode mode, ushort sendBufferSize);
        void WriteDataBreak(BinaryWriter writer, int sequenceId);
        void WriteClassTransformed(BinaryWriter writer, string className);
        void WriteClassTransformFailed(BinaryWriter writer, string className);
        void WriteClassIgnored(BinaryWriter writer, string className);
        void WriteMapThreadName(BinaryWriter writer, ushort threadId, int relTime, string threadName);
        void WriteMapMethodSignature(BinaryWriter writer, int sigId, string signature);
        void WriteMapException(BinaryWriter writer, int excId, string exception);
        void WriteMethodEntry(BinaryWriter writer, int relTime, int seq, int sigId, ushort threadId);
        void WriteMethodExit(BinaryWriter writer, int relTime, int seq, int sigId, ushort lineNum, ushort threadId);
        void WriteException(BinaryWriter writer, int relTime, int seq, int sigId, int excId, ushort lineNum, ushort threadId);
        void WriteExceptionBubble(BinaryWriter writer, int relTime, int seq, int sigId, int excId, ushort threadId);
        void WriteMarker(BinaryWriter writer, string key, string value, int relTime, int seq);
    }
}
