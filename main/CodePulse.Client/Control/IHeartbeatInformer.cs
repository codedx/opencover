using CodePulse.Client.Message;

namespace CodePulse.Client.Control
{
    public interface IHeartbeatInformer
    {
        AgentOperationMode OperationMode { get; }

        int SendQueueSize { get; }
    }
}
