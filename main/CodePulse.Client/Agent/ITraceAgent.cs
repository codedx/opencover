using CodePulse.Client.Config;
using CodePulse.Client.Instrumentation.Id;
using CodePulse.Client.Trace;

namespace CodePulse.Client.Agent
{
    public interface ITraceAgent
    {
        StaticAgentConfiguration StaticAgentConfiguration { get; }

        RuntimeAgentConfiguration RuntimeAgentConfiguration { get; }

        ClassIdentifier ClassIdentifier { get; }

        MethodIdentifier MethodIdentifier { get; }

        ITraceDataCollector TraceDataCollector { get; }

        bool Connect();

        bool Prepare();

        void Start();

        void KillTrace(string errorMessage);

        void Shutdown();

        void ShutdownAndWait();

        void WaitForStart();
    }
}
