using System.IO;

namespace CodePulse.Client.Control
{
    public interface IControlMessageProcessor
    {
        void ProcessIncomingMessage(BinaryReader inputReader);
    }
}
