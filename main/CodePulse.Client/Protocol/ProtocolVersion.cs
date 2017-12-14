using System;
using CodePulse.Client.Control;
using CodePulse.Client.Errors;
using CodePulse.Client.Init;
using CodePulse.Client.Message;

namespace CodePulse.Client.Protocol
{
    public class ProtocolVersion : IProtocolVersion
    {
        private readonly IErrorHandler _errorHandler;
        public IMessageProtocol MessageProtocol { get; }

        public IConfigurationReader ConfigurationReader { get; }

        public IControlConnectionHandshake ControlConnectionHandshake { get; }
        public IDataConnectionHandshake DataConnectionHandshake { get; }

        public ProtocolVersion(IErrorHandler errorHandler)
        {
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            MessageProtocol = new MessageProtocol();
            ConfigurationReader = new ConfigurationReader();
            ControlConnectionHandshake = new ControlConnectionHandshake(MessageProtocol, new ConfigurationReader());
            DataConnectionHandshake = new DataConnectionHandshake(MessageProtocol);
        }

        public IControlMessageProcessor GetControlMessageProcessor(IControlMessageHandler controlMessageHandler,
            IConfigurationHandler configurationHandler)
        {
            return new ControlMessageProcessor(ConfigurationReader, controlMessageHandler, configurationHandler, _errorHandler);
        }
    }
}
