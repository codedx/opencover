using System;
using System.IO;
using CodePulse.Client.Errors;
using CodePulse.Client.Message;
using CodePulse.Client.Util;

namespace CodePulse.Client.Control
{
    public class ControlMessageProcessor : IControlMessageProcessor
    {
        private readonly IErrorHandler _errorHandler;
        private readonly IConfigurationReader _configurationReader;
        private readonly IControlMessageHandler _messageHandler;
        private readonly IConfigurationHandler _configurationHandler;

        public ControlMessageProcessor(IConfigurationReader configurationReader,
            IControlMessageHandler  messageHandler, 
            IConfigurationHandler configurationHandler,
            IErrorHandler errorHandler)
        {
            _configurationReader = configurationReader ?? throw new ArgumentNullException(nameof(configurationReader));
            _messageHandler = messageHandler ?? throw new ArgumentNullException(nameof(messageHandler));
            _configurationHandler = configurationHandler ?? throw new ArgumentNullException(nameof(configurationHandler));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        }

        public void ProcessIncomingMessage(BinaryReader inputReader)
        {
            byte messageType = inputReader.ReadByte();

            switch (messageType)
            {
                case MessageTypes.Start:
                    _messageHandler.OnStart();
                    break;
                case MessageTypes.Stop:
                    _messageHandler.OnStop();
                    break;
                case MessageTypes.Pause:
                    _messageHandler.OnPause();
                    break;
                case MessageTypes.Unpause:
                    _messageHandler.OnUnpause();
                    break;
                case MessageTypes.Suspend:
                    _messageHandler.OnSuspend();
                    break;
                case MessageTypes.Unsuspend:
                    _messageHandler.OnUnsuspend();
                    break;
                case MessageTypes.Configuration:
                    _configurationHandler.OnConfig(_configurationReader.ReadConfiguration(inputReader));
                    break;
                case MessageTypes.Error:
                    _messageHandler.OnError(inputReader.ReadUtfBigEndian());
                    break;
                default:
                    _errorHandler.HandleError("Unrecognized control message in ProcessIncomingMessage.", null);
                    break;
            }
        }
    }
}
