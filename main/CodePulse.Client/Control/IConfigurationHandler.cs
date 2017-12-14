using CodePulse.Client.Config;

namespace CodePulse.Client.Control
{
    public interface IConfigurationHandler
    {
        void OnConfig(RuntimeAgentConfiguration config);
    }
}
