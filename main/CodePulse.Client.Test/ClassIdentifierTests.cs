using CodePulse.Client.Instrumentation.Id;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodePulse.Client.Test
{
    [TestClass]
    public class ClassIdentifierTests
    {
        [TestMethod]
        public void WhenClassIdKnownClassIdReturned()
        {
            // arrange
            var classIdentifier = new ClassIdentifier();
            var classId = classIdentifier.Record("A", "B");

            // act
            var retVal = classIdentifier.Record("A", "B");

            // assert
            Assert.AreEqual(classId, retVal);
        }

        [TestMethod]
        public void WhenClassIdUnknownClassIdAssigned()
        {
            // arrange
            var classIdentifier = new ClassIdentifier();
            var classId = classIdentifier.Record("A", "B");

            // act
            var retVal = classIdentifier.Record("B", "B");

            // assert
            Assert.AreEqual(classId + 1, retVal);
        }
    }
}
