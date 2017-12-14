using CodePulse.Client.Message;

namespace CodePulse.Client.Control
{
    public interface IModeChangeListener
    {
        void OnModeChange(AgentOperationMode oldMode, AgentOperationMode newMode);
    }
}
