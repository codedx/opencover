using System;
using System.Threading.Tasks;
using CodePulse.Client.Message;
using CodePulse.Client.Queue;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodePulse.Client.Test
{
    [TestClass]
    public class PooledBufferServiceTests
    {
        [TestMethod]
        public void WhenServiceIsSuspendedObtainBufferReturnsNull()
        {
            // arrange
            var bufferPool = new BufferPool(2, 9);
            var service = new PooledBufferService(bufferPool);

            // act
            service.SetSuspended(true);

            var buffer = service.ObtainBuffer();

            // assert
            Assert.IsNull(buffer);
        }

        [TestMethod]
        public void WhenServiceIsPausedObtainBufferBlocks()
        {
            // arrange
            var bufferPool = new BufferPool(2, 9);
            var service = new PooledBufferService(bufferPool);

            // act
            service.SetPaused(true);

            var task1 = Task.Run(() =>
            {
                service.ObtainBuffer();
            });
            var task2 = Task.Run(() =>
            {
                service.ObtainBuffer();
            });

            var waitResult1 = task1.Wait(TimeSpan.FromMilliseconds(2000));
            var waitResult2 = task2.Wait(TimeSpan.FromMilliseconds(2000));

            // assert
            Assert.IsFalse(waitResult1);
            Assert.IsFalse(waitResult2);

            service.SetPaused(false);

            waitResult1 = task1.Wait(TimeSpan.FromMilliseconds(2000));
            waitResult2 = task2.Wait(TimeSpan.FromMilliseconds(2000));

            Assert.IsTrue(waitResult1);
            Assert.IsTrue(waitResult2);
        }

        [TestMethod]
        public void WhenServiceIsNotSuspendedBufferIsReturned()
        {
            // arrange
            var bufferPool = new BufferPool(2, 9);
            var service = new PooledBufferService(bufferPool);

            // act
            var buffer = service.ObtainBuffer();

            // assert
            Assert.IsNotNull(buffer);
        }

        [TestMethod]
        public void WhenBufferRelinquishedReadableBufferCountIncreases()
        {
            // arrange
            var bufferPool = new BufferPool(2, 9);
            var service = new PooledBufferService(bufferPool);
            var readableBuffersBefore = bufferPool.ReadableBuffers;

            // act
            var buffer = service.ObtainBuffer();
            buffer.WriteByte(1);
            service.RelinquishBuffer(buffer);

            // assert
            Assert.IsNotNull(buffer);
            Assert.AreEqual(0, readableBuffersBefore);
            Assert.AreEqual(1, bufferPool.ReadableBuffers);
        }
    }
}
