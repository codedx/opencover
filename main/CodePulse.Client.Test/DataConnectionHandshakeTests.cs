using System;
using System.IO;
using CodePulse.Client.Init;
using CodePulse.Client.Message;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CodePulse.Client.Test
{
    [TestClass]
    public class DataConnectionHandshakeTests
    {
        [TestMethod]
        public void WhenDataConnectionHandshakeSuccessConfigurationReturned()
        {
            // arrange
            var messageProtocol = new MessageProtocol();
            var dataConnectionHandshake = new DataConnectionHandshake(messageProtocol);

            var binaryReader = new Mock<BinaryReader>(new MemoryStream());
            var binaryWriter = new Mock<BinaryWriter>(new MemoryStream());

            // set up configuration message read
            binaryReader.Setup(x => x.ReadByte())
                .Returns(MessageTypes.DataHelloReply);

            var connection = new Connection(binaryReader.Object, binaryWriter.Object);

            // act
            var success = dataConnectionHandshake.PerformHandshake(1, connection);

            // assert
            binaryWriter.Verify(x => x.Write(MessageTypes.DataHello), Times.Once);

            Assert.IsTrue(success);
        }

        [TestMethod]
        public void WhenDataConnectionHandshakeHasErrorExceptionThrown()
        {
            // arrange
            var messageProtocol = new MessageProtocol();
            var dataConnectionHandshake = new DataConnectionHandshake(messageProtocol);

            var binaryReader = new Mock<BinaryReader>(new MemoryStream());
            var binaryWriter = new Mock<BinaryWriter>(new MemoryStream());

            binaryReader.Setup(x => x.ReadByte())
                .Returns(MessageTypes.Error);

            const string errorString = "Error";

            binaryReader.Setup(x => x.ReadBytes(sizeof(short)))
                .Returns(new byte[] { 0x0, Convert.ToByte(errorString.Length) });

            binaryReader.Setup(x => x.ReadBytes(errorString.Length))
                .Returns(System.Text.Encoding.UTF8.GetBytes(errorString));

            var connection = new Connection(binaryReader.Object, binaryWriter.Object);

            try
            {
                // act
                dataConnectionHandshake.PerformHandshake(1, connection);
            }
            catch (HandshakeException e)
            {
                Assert.AreEqual("Error", e.Message);
            }

            // assert
            binaryWriter.Verify(x => x.Write(MessageTypes.DataHello), Times.Once);
        }

        [TestMethod]
        public void WhenDataConnectionHandshakeHasUnknownErrorExceptionThrown()
        {
            // arrange
            var messageProtocol = new MessageProtocol();
            var dataConnectionHandshake = new DataConnectionHandshake(messageProtocol);

            var binaryReader = new Mock<BinaryReader>(new MemoryStream());
            var binaryWriter = new Mock<BinaryWriter>(new MemoryStream());

            binaryReader.Setup(x => x.ReadByte())
                .Returns(0xFE);

            var connection = new Connection(binaryReader.Object, binaryWriter.Object);

            try
            {
                // act
                dataConnectionHandshake.PerformHandshake(1, connection);
            }
            catch (HandshakeException e)
            {
                Assert.AreEqual("Handshake operation failed with unexpected reply: 254", e.Message);
            }

            // assert
            binaryWriter.Verify(x => x.Write(MessageTypes.DataHello), Times.Once);
        }
    }
}
