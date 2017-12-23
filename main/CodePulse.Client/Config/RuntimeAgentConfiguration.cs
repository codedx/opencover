using System.Collections.Generic;
using Newtonsoft.Json;

namespace CodePulse.Client.Config
{
    public class RuntimeAgentConfiguration
    {
        public byte RunId { get; }

        public int HeartbeatInterval { get; }

        public List<string> Exclusions { get; }

        public List<string> Inclusions { get; }

        public int BufferMemoryBudget { get; }

        public int NumDataSenders { get; }

        [JsonConstructor]
        public RuntimeAgentConfiguration(byte runId,
            int heartbeatInterval,
            IEnumerable<string> exclusions,
            IEnumerable<string> inclusions,
            int bufferMemoryBudget,
            int numDataSenders)
        {
            RunId = runId;
            HeartbeatInterval = heartbeatInterval;
            Exclusions = new List<string>(exclusions);
            Inclusions = new List<string>(inclusions);
            BufferMemoryBudget = bufferMemoryBudget;
            NumDataSenders = numDataSenders;
        }
    }
}
