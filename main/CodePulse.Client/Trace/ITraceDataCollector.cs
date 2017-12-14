namespace CodePulse.Client.Trace
{
    public interface ITraceDataCollector
    {
        int SequenceId { get; }

        int MethodEntry(string className, string sourceFile,
            string methodName, string methodSignature,
            int startLineNumber, int endLineNumber);
    }
}
