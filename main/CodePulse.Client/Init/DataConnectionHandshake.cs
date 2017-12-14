using System;
using CodePulse.Client.Connect;
using CodePulse.Client.Message;
using CodePulse.Client.Util;

namespace CodePulse.Client.Init
{
    public class DataConnectionHandshake : IDataConnectionHandshake
    {
        private readonly IMessageProtocol _messageProtocol;

        public DataConnectionHandshake(IMessageProtocol messageProtocol)
        {
            _messageProtocol = messageProtocol ?? throw new ArgumentNullException(nameof(messageProtocol));
        }

        public bool PerformHandshake(byte runId, IConnection connection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            var outputWriter = connection.OutputWriter;

            _messageProtocol.WriteDataHello(outputWriter, runId);
            outputWriter.FlushAndLog("WriteDataHello");

            var inputReader = connection.InputReader;
            var reply = inputReader.ReadByte();

            switch (reply)
            {
                case MessageTypes.DataHelloReply:
                    return true;
                case MessageTypes.Error:
                    throw new HandshakeException(inputReader.ReadUtfBigEndian(), reply);
                default:
                    throw new HandshakeException($"Handshake operation failed with unexpected reply: {reply}", reply);
            }
        }
    }
}
