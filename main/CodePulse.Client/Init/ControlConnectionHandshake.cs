using System;
using CodePulse.Client.Config;
using CodePulse.Client.Connect;
using CodePulse.Client.Control;
using CodePulse.Client.Message;
using CodePulse.Client.Util;

namespace CodePulse.Client.Init
{
    public class ControlConnectionHandshake : IControlConnectionHandshake
    {
        private readonly IMessageProtocol _messageProtocol;
        private readonly IConfigurationReader _configurationReader;

        public ControlConnectionHandshake(IMessageProtocol messageProtocol, IConfigurationReader configurationReader)
        {
            _messageProtocol = messageProtocol ?? throw new ArgumentNullException(nameof(messageProtocol));
            _configurationReader = configurationReader ?? throw new ArgumentNullException(nameof(configurationReader));
        }

        public RuntimeAgentConfiguration PerformHandshake(IConnection connection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            var outputWriter = connection.OutputWriter;

            _messageProtocol.WriteHello(outputWriter);
            outputWriter.FlushAndLog("WriteHello");

            var inputReader = connection.InputReader;
            var reply = inputReader.ReadByte();
            
            switch (reply)
            {
                case MessageTypes.Configuration:
                    return _configurationReader.ReadConfiguration(inputReader);
                case MessageTypes.Error:
                    throw new HandshakeException(inputReader.ReadUtfBigEndian(), reply);
                default:
                    throw new HandshakeException($"Handshake operation failed with unexpected reply: {reply}", reply);
            }
        }
    }
}
