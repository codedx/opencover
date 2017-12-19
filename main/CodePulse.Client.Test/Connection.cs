using System.IO;
using CodePulse.Client.Connect;

namespace CodePulse.Client.Test
{
    public class Connection : IConnection
    {
        public BinaryReader InputReader { get; }
        public BinaryWriter OutputWriter { get; }

        public Connection(BinaryReader inputReader, BinaryWriter outputWriter)
        {
            InputReader = inputReader;
            OutputWriter = outputWriter;
        }

        public void Close()
        {
            InputReader.Close();
            OutputWriter.Close();
        }
    }
}
