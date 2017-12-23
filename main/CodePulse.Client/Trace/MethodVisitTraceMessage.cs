namespace CodePulse.Client.Trace
{
    public class MethodVisitTraceMessage : ITraceMessage
    {
        public string ClassName { get; }
        public string SourceFile { get; }
        public string MethodName { get; }
        public string MethodSignature { get; }
        public int StartLineNumber { get; }
        public int EndLineNumber { get; }

        public MethodVisitTraceMessage(string className, string sourceFile, string methodName, string methodSignature,
            int startLineNumber, int endLineNumber)
        {
            if (string.IsNullOrWhiteSpace(className))
            {
                throw new System.ArgumentException("message", nameof(className));
            }

            if (string.IsNullOrWhiteSpace(methodName))
            {
                throw new System.ArgumentException("message", nameof(methodName));
            }

            if (string.IsNullOrWhiteSpace(methodSignature))
            {
                throw new System.ArgumentException("message", nameof(methodSignature));
            }

            ClassName = className;
            SourceFile = sourceFile;
            MethodName = methodName;
            MethodSignature = methodSignature;
            StartLineNumber = startLineNumber;
            EndLineNumber = endLineNumber;
        }
    }
}
