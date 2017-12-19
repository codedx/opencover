using CodePulse.Client.Instrumentation.Id;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodePulse.Client.Test
{
    [TestClass]
    public class MethodIdentifierTests
    {
        [TestMethod]
        public void WhenMethodIdKnownClassIdReturned()
        {
            // arrange
            var methodIdentifier = new MethodIdentifier();
            var methodId = methodIdentifier.Record(1, "A", "B", 2, 3);

            // act
            var retVal = methodIdentifier.Record(1, "A", "B", 2, 3);

            // assert
            Assert.AreEqual(methodId, retVal);
            Assert.AreEqual("A", methodIdentifier.Lookup(methodId).Name);
        }

        [TestMethod]
        public void WhenMethodIdUnknownClassIdReturned()
        {
            // arrange
            var methodIdentifier = new MethodIdentifier();
            var methodId = methodIdentifier.Record(1, "A", "B", 2, 3);

            // act
            var retVal = methodIdentifier.Record(1, "C", "D", 2, 3);

            // assert
            Assert.AreEqual(methodId + 1, retVal);
            Assert.AreEqual("C", methodIdentifier.Lookup(methodId + 1).Name);
        }
    }
}
