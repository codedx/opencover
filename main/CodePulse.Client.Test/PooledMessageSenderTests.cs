using System.IO;
using System.Threading;
using CodePulse.Client.Errors;
using CodePulse.Client.Message;
using CodePulse.Client.Queue;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CodePulse.Client.Test
{
    [TestClass]
    public class PooledMessageSenderTests
    {
        [TestMethod]
        public void WhenSenderStartsItRunsUntilShutdown()
        {
            // arrange
            var bufferPool = new BufferPool(2, 9);
            var binaryWriter = new Mock<BinaryWriter>();
            var errorHandler = new Mock<IErrorHandler>();
            var logger = new Mock<ILog>();

            // act
            var messageSender = new PooledMessageSender(bufferPool, binaryWriter.Object, errorHandler.Object, logger.Object);

            Thread.Sleep(5000);
            messageSender.Shutdown();
            Thread.Sleep(5000);

            // assert
            Assert.IsTrue(messageSender.IsIdle);
            Assert.IsTrue(messageSender.IsShutdown);
        }
    }
}
