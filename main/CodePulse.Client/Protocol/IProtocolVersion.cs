using CodePulse.Client.Control;
using CodePulse.Client.Init;
using CodePulse.Client.Message;

namespace CodePulse.Client.Protocol
{
    public interface IProtocolVersion
    {
        IMessageProtocol MessageProtocol { get; }

        IControlConnectionHandshake ControlConnectionHandshake { get; }

        IDataConnectionHandshake DataConnectionHandshake { get; }

        IControlMessageProcessor GetControlMessageProcessor(IControlMessageHandler controlMessageHandler,
            IConfigurationHandler configurationHandler);
    }
}
