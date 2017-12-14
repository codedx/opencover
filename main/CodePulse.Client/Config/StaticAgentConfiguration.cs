using System;
using log4net;

namespace CodePulse.Client.Config
{
    public class StaticAgentConfiguration
    {
        public const int DefaultConnectTimeout = 30;

        public int HqPort { get; }

        public string HqHost { get; }

        public int ConnectTimeout { get; }

        public ILog Logger { get; }

        public StaticAgentConfiguration(int hqPort,
            string hqHost,
            int connectTimeout,
            ILog logger)
        {
            if (hqHost == null)
            {
                throw new ArgumentNullException(nameof(hqHost));
            }
            if (string.IsNullOrWhiteSpace(hqHost))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(hqHost));
            }

            HqPort = hqPort;
            HqHost = hqHost;
            ConnectTimeout = connectTimeout;
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
    }
}
