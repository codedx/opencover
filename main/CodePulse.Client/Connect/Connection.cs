using System.IO;

namespace CodePulse.Client.Connect
{
    public interface IConnection
    {
        void Close();

        BinaryReader InputReader { get; }

        BinaryWriter OutputWriter { get; }
    }
}
