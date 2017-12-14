using Mono.Cecil;

namespace CodePulse.Client.Trace
{
    public interface ITraceDataCollector
    {
        int SequenceId { get; }

        void MethodEntry(int methodId);

        int MethodEntry(string className, string sourceFile,
            MethodAttributes attributes, string methodName, string methodSignature,
            int startLineNumber, int endLineNumber);

        void MethodExit(int methodId, ushort sourceLine);
    }
}
