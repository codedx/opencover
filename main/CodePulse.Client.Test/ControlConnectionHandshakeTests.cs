using System;
using System.IO;
using CodePulse.Client.Control;
using CodePulse.Client.Init;
using CodePulse.Client.Message;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CodePulse.Client.Test
{
    [TestClass]
    public class ControlConnectionHandshakeTests
    {
        [TestMethod]
        public void WhenControlConnectionHandshakeSuccessConfigurationReturned()
        {
            // arrange
            var messageProtocol = new MessageProtocol();
            var configurationReader = new Mock<ConfigurationReader>();
            var controlConnectionHandshake = new ControlConnectionHandshake(messageProtocol, configurationReader.Object);

            var binaryReader = new Mock<BinaryReader>(new MemoryStream());
            var binaryWriter = new Mock<BinaryWriter>(new MemoryStream());

            binaryReader.Setup(x => x.ReadByte())
                .Returns(MessageTypes.Configuration);

            const string configurationJson = "{\"RunId\":1,\"HeartbeatInterval\":2,\"Exclusions\":[\"Exclusion\"],\"Inclusions\":[\"Inclusion\"],\"BufferMemoryBudget\":3,\"QueueRetryCount\":4,\"NumDataSenders\":5}";

            binaryReader.Setup(x => x.ReadBytes(sizeof(short)))
                .Returns(new byte[] {0x0, Convert.ToByte(configurationJson.Length)});

            binaryReader.Setup(x => x.ReadBytes(configurationJson.Length))
                .Returns(System.Text.Encoding.UTF8.GetBytes(configurationJson));

            var connection = new Connection(binaryReader.Object, binaryWriter.Object);
            
            // act
            var configuration = controlConnectionHandshake.PerformHandshake(connection);

            // assert
            binaryWriter.Verify(x => x.Write(MessageTypes.Hello), Times.Once);
            binaryWriter.Verify(x => x.Write(messageProtocol.ProtocolVersion), Times.Once());

            Assert.IsNotNull(configuration);
        }

        [TestMethod]
        public void WhenControlConnectionHandshakeHasErrorExceptionThrown()
        {
            // arrange
            var messageProtocol = new MessageProtocol();
            var configurationReader = new Mock<ConfigurationReader>();
            var controlConnectionHandshake = new ControlConnectionHandshake(messageProtocol, configurationReader.Object);

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

            // act
            try
            {
               controlConnectionHandshake.PerformHandshake(connection);
            }
            catch (HandshakeException e)
            {
                Assert.AreEqual("Error", e.Message);
            }
        }

        [TestMethod]
        public void WhenControlConnectionHandshakeHasUnknownErrorExceptionThrown()
        {
            // arrange
            var messageProtocol = new MessageProtocol();
            var configurationReader = new Mock<ConfigurationReader>();
            var controlConnectionHandshake = new ControlConnectionHandshake(messageProtocol, configurationReader.Object);

            var binaryReader = new Mock<BinaryReader>(new MemoryStream());
            var binaryWriter = new Mock<BinaryWriter>(new MemoryStream());

            binaryReader.Setup(x => x.ReadByte())
                .Returns(0xFE);

            var connection = new Connection(binaryReader.Object, binaryWriter.Object);

            // act
            try
            {
                controlConnectionHandshake.PerformHandshake(connection);
            }
            catch (HandshakeException e)
            {
                Assert.AreEqual("Handshake operation failed with unexpected reply: 254", e.Message);
            }
        }
    }
}
