using System.Collections.Generic;

namespace CodePulse.Client.Instrumentation.Id
{
    public class MethodIdentifier
    {
        private int _nextMethodId;

        private readonly Dictionary<int, MethodInformation> _methodsById = new Dictionary<int, MethodInformation>();
        private readonly Dictionary<string, MethodInformation> _methodsBySignature = new Dictionary<string, MethodInformation>();

        public int Record(int classId, MethodAccess access, string name, string signature, int startLine, int endLine)
        {
            if (_methodsBySignature.TryGetValue(signature, out var methodInformation))
            {
                return methodInformation.Id;
            }

            var methodId = _nextMethodId++;
            var newMethodInformation = new MethodInformation(methodId, classId, access, name, signature, startLine, endLine);

            _methodsById[methodId] = newMethodInformation;
            _methodsBySignature[signature] = newMethodInformation;

            return methodId;
        }

        public MethodInformation Lookup(int methodId)
        {
            return !_methodsById.TryGetValue(methodId, out var info) ? null : info;
        }
    }
}
