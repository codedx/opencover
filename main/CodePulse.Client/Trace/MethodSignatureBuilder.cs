using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace CodePulse.Client.Trace
{
    public enum Modifier
    {
        Public = 0x00000001,
        Private = 0x00000002,
        Protected = 0x00000004,
        Static = 0x00000008,
        Final = 0x00000010,
        Synchronized = 0x00000020,
        Volatile = 0x00000040,
        Transient = 0x00000080,
        Native = 0x00000100,
        Interface = 0x00000200,
        Abstract = 0x00000400,
        Strict = 0x00000800,

        Bridge = 0x00000040,
        Varargs = 0x00000080,
        Synthetic = 0x00001000,
        Annotation = 0x00002000,
        Enum = 0x00004000,
        Mandated = 0x00008000
    }

    public class MethodSignatureBuilder : IMethodSignatureBuilder
    {
        private readonly Dictionary<Modifier, Func<MethodDefinition, Modifier>> _accessModiferQueries = new Dictionary<Modifier, Func<MethodDefinition, Modifier>>()
        {
            { Modifier.Public, (method) => method.IsPublic ? Modifier.Public : 0 },
            { Modifier.Private, (method) => method.IsPrivate ? Modifier.Private : 0 },
            { Modifier.Abstract, (method) => method.IsAbstract ? Modifier.Abstract : 0 },
            { Modifier.Static, (method) => method.IsStatic ? Modifier.Static : 0 },
            { Modifier.Synchronized, (method) => method.IsSynchronized ? Modifier.Synchronized : 0 },
            { Modifier.Final, (method) => method.IsFinal ? Modifier.Final : 0 },
            { Modifier.Protected, (method) => method.IsFamily ? Modifier.Protected : 0 }
        };

        public string CreateSignature(MethodDefinition methodDefinition)
        {
            return $"{methodDefinition.DeclaringType?.FullName}.{methodDefinition.Name};{GetAccessModifiers(methodDefinition)};({string.Join(",", GetParameters(methodDefinition))});{methodDefinition.ReturnType.FullName}";
        }

        private List<String> GetParameters(MethodDefinition methodDefinition)
        {
            return methodDefinition.Parameters.Select(parameter => parameter.ParameterType.FullName).ToList();
        }

        private int GetAccessModifiers(MethodDefinition method)
        {
            return _accessModiferQueries.Values.Select(getModifier => getModifier(method))
                .Aggregate(0, (previous, next) => previous | (int)next);
        }
    }
}
