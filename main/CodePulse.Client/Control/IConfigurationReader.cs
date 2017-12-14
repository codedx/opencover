using System.IO;
using CodePulse.Client.Config;

namespace CodePulse.Client.Control
{
    public interface IConfigurationReader
    {
        RuntimeAgentConfiguration ReadConfiguration(BinaryReader reader);
    }
}
