using CodePulse.Client.Config;
using CodePulse.Client.Connect;

namespace CodePulse.Client.Init
{
    public interface IControlConnectionHandshake
    {
        RuntimeAgentConfiguration PerformHandshake(IConnection connection);
    }
}
