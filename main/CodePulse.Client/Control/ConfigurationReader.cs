using System.IO;
using CodePulse.Client.Config;
using CodePulse.Client.Util;
using Newtonsoft.Json;

namespace CodePulse.Client.Control
{
    public class ConfigurationReader : IConfigurationReader
    {
        public RuntimeAgentConfiguration ReadConfiguration(BinaryReader reader)
        {
            var configJson = reader.ReadUtfBigEndian();
            return JsonConvert.DeserializeObject<RuntimeAgentConfiguration>(configJson);
        }
    }
}
