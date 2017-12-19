using CodePulse.Client.Trace;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mono.Cecil;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using ParameterAttributes = Mono.Cecil.ParameterAttributes;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace CodePulse.Client.Test
{
    [TestClass]
    public class MethodSignatureTests
    {
        [TestMethod]
        public void CanBuildSignatureWithNoParamsOrReturn()
        {
            // arrange
            var signatureBuilder = new MethodSignatureBuilder();
            var methodDefinition = new MethodDefinition("Foo", MethodAttributes.Public, new TypeDefinition("System", "Void", TypeAttributes.Public))
            {
                DeclaringType = new TypeDefinition("Namespace", "Class", TypeAttributes.Public)
            };

            // act
            var signature = signatureBuilder.CreateSignature(methodDefinition);

            // assert
            Assert.AreEqual("Namespace.Class.Foo;1;();System.Void", signature);
        }

        [TestMethod]
        public void CanBuildSignatureWithNoParams()
        {
            // arrange
            var signatureBuilder = new MethodSignatureBuilder();
            var methodDefinition = new MethodDefinition("Foo", MethodAttributes.Public, new TypeDefinition("System", "Int32", TypeAttributes.Public))
            {
                DeclaringType = new TypeDefinition("Namespace", "Class", TypeAttributes.Public)
            };

            // act
            var signature = signatureBuilder.CreateSignature(methodDefinition);

            // assert
            Assert.AreEqual("Namespace.Class.Foo;1;();System.Int32", signature);
        }

        [TestMethod]
        public void CanBuildSignatureWithOneParams()
        {
            // arrange
            var signatureBuilder = new MethodSignatureBuilder();
            var methodDefinition = new MethodDefinition("Foo", MethodAttributes.Public, new TypeDefinition("System", "Int32", TypeAttributes.Public))
            {
                DeclaringType = new TypeDefinition("Namespace", "Class", TypeAttributes.Public),
                Parameters = { new ParameterDefinition("bar", ParameterAttributes.None, new TypeDefinition("System", "Int32", TypeAttributes.Public))}
            };

            // act
            var signature = signatureBuilder.CreateSignature(methodDefinition);

            // assert
            Assert.AreEqual("Namespace.Class.Foo;1;(System.Int32);System.Int32", signature);
        }

        [TestMethod]
        public void CanBuildSignatureWithMultipleParams()
        {
            // arrange
            var signatureBuilder = new MethodSignatureBuilder();
            var methodDefinition = new MethodDefinition("Foo", MethodAttributes.Public, new TypeDefinition("System", "Int32", TypeAttributes.Public))
            {
                DeclaringType = new TypeDefinition("Namespace", "Class", TypeAttributes.Public),
                Parameters =
                {
                    new ParameterDefinition("bar1", ParameterAttributes.None, new TypeDefinition("System", "Int32", TypeAttributes.Public)),
                    new ParameterDefinition("bar2", ParameterAttributes.None, new TypeDefinition("System", "Int16", TypeAttributes.Public)),
                    new ParameterDefinition("bar3", ParameterAttributes.None, new TypeDefinition("System", "Boolean", TypeAttributes.Public))
                }
            };

            // act
            var signature = signatureBuilder.CreateSignature(methodDefinition);

            // assert
            Assert.AreEqual("Namespace.Class.Foo;1;(System.Int32,System.Int16,System.Boolean);System.Int32", signature);
        }
    }
}
