using System;

namespace CodePulse.Client.Instrumentation
{
    public class ClassInformation
    {
        public int Id { get; }
        public string Name { get; }
        public string SourceFile { get; }

        public ClassInformation(int id, string name, string sourceFile)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
            if (string.IsNullOrWhiteSpace(sourceFile))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(sourceFile));

            Id = id;
            Name = name;
            SourceFile = sourceFile;
        }
    }
}
