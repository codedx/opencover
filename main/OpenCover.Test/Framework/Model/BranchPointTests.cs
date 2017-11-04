using NUnit.Framework;
using OpenCover.Framework.Model;

namespace OpenCover.Test.Framework.Model
{
    [TestFixture]
    public class BranchPointTests
    {
        [Test]
        public void CanReturnLineNumbers()
        {
            // arrange
            var sequencePoint = new BranchPoint { StartLine = 1 };

            // act
            var lineNumbers = sequencePoint.GetLineNumbers();

            // assert
            Assert.AreEqual(1, lineNumbers.Item1);
            Assert.IsNull(lineNumbers.Item2);
        }
    }
}
