using System;

namespace CodePulse.Client.Instrumentation
{
    public class MethodInformation
    {
        public int Id { get; }
        public int ClassId { get; }
        public MethodAccess Access { get; }
        public string Name { get; }
        public string Signature { get; }
        public int StartLine { get; }
        public int EndLine { get; }

        public MethodInformation(int id, int classId, MethodAccess access, string name, string signature, int startLine, int endLine)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
            if (string.IsNullOrWhiteSpace(signature))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(signature));

            Id = id;
            ClassId = classId;
            Access = access;
            Name = name;
            Signature = signature;
            StartLine = startLine;
            EndLine = endLine;
        }
    }
}
