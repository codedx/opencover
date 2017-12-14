using System.Collections.Generic;

namespace CodePulse.Client.Instrumentation.Id
{
    public class ClassIdentifier
    {
        private int _nextClassId;

        private readonly Dictionary<string, ClassInformation> _classes = new Dictionary<string, ClassInformation>();

        public int Record(string className, string classSourceFile)
        {
            if (_classes.TryGetValue(className, out var classInformation))
            {
                return classInformation.Id;
            }

            var classId = _nextClassId++;
            _classes[className] = new ClassInformation(classId, className, classSourceFile);

            return classId;
        }
    }
}
