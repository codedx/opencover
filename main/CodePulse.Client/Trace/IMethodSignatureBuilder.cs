using Mono.Cecil;

namespace CodePulse.Client.Trace
{
    public interface IMethodSignatureBuilder
    {
        string CreateSignature(MethodDefinition methodDefinition);
    }
}
