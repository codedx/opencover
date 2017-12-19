using System.IO;

namespace CodePulse.Client.Message
{
    public interface IMessageProtocol
    {
        byte ProtocolVersion { get; }

        void WriteHello(BinaryWriter writer);
        void WriteDataHello(BinaryWriter writer, byte runId);
        void WriteError(BinaryWriter writer, string error);
        void WriteHeartbeat(BinaryWriter writer, AgentOperationMode mode, ushort sendBufferSize);
        void WriteDataBreak(BinaryWriter writer, int sequenceId);
        void WriteMapMethodSignature(BinaryWriter writer, int sigId, string signature);
        void WriteMethodEntry(BinaryWriter writer, int relTime, int seq, int sigId, ushort threadId);
    }
}
