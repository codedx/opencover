﻿using System;
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

        public void WriteMapMethodSignature(BinaryWriter writer, int sigId, string signature)
        {
            writer.Write(MessageTypes.MapMethodSignature);
            writer.WriteBigEndian(sigId);
            writer.WriteUtfBigEndian(signature);
        }

        public void WriteMethodEntry(BinaryWriter writer, int relTime, int seq, int sigId, ushort threadId)
        {
            writer.Write(MessageTypes.MethodEntry);
            writer.WriteBigEndian(relTime);
            writer.WriteBigEndian(seq);
            writer.WriteBigEndian(sigId);
            writer.WriteBigEndian(threadId);
        }
    }
}
