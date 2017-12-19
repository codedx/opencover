using System.IO;
using CodePulse.Client.Control;
using CodePulse.Client.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodePulse.Client.Test
{
    [TestClass]
    public class ConfigurationTests
    {
        [TestMethod]
        public void WhenDeserializedRuntimeDataConfigurationIsCorrect()
        {
            // arrange
            var configurationReader = new ConfigurationReader();

            using (var outputStream = new MemoryStream())
            using (var binaryWriter = new BinaryWriter(outputStream))
            {
                binaryWriter.WriteUtfBigEndian("{\"RunId\":1,\"HeartbeatInterval\":2,\"Exclusions\":[\"Exclusion\"],\"Inclusions\":[\"Inclusion\"],\"BufferMemoryBudget\":3,\"QueueRetryCount\":4,\"NumDataSenders\":5}");

                using (var inputStream = new MemoryStream(outputStream.ToArray()))
                using (var binaryReader = new BinaryReader(inputStream))
                {
                    // act
                    var configuration = configurationReader.ReadConfiguration(binaryReader);

                    // assert
                    Assert.AreEqual(1, configuration.RunId);
                    Assert.AreEqual(2, configuration.HeartbeatInterval);
                    Assert.AreEqual("Exclusion", configuration.Exclusions[0]);
                    Assert.AreEqual("Inclusion", configuration.Inclusions[0]);
                    Assert.AreEqual(3, configuration.BufferMemoryBudget);
                    Assert.AreEqual(4, configuration.QueueRetryCount);
                    Assert.AreEqual(5, configuration.NumDataSenders);
                }
            }
        }
    }
}
