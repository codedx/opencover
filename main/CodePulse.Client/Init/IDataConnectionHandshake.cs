using CodePulse.Client.Connect;

namespace CodePulse.Client.Init
{
    public interface IDataConnectionHandshake
    {
        bool PerformHandshake(byte runId, IConnection connection);
    }
}
