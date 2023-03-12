using Microsoft.Extensions.Logging;
using NanoleafAPI;
using NanoleafAPI.API;
using System.Net;

namespace NanoleafAPI_Tests
{
    public class BrightnessSensorConfigTests
    {
        private static ILogger? __logger = null;
        const string IP = "192.168.10.152";
        const string PORT = "16021";
        const string AUTH_TOKEN = "7lOFIqsyqmO8c8H2bYco74z4fK2DmXqK";
        [SetUp]
        public void Setup()
        {
            Tools.LoggerFactory = new TestLoggerFactory();
            __logger = Tools.LoggerFactory.CreateLogger(nameof(BrightnessSensorConfigTests));
            Communication.RegisterIPAddress(IPAddress.Any);
        }
        [Test]
        public async Task TestGetBrightnessSensorConfig()
        {
            var responseGet = await Communication.GetBrightnessSensorConfig(IP, PORT, AUTH_TOKEN);
            Assert.That(responseGet.Success, Is.True);
        }
        [Test]
        public async Task TestSetBrightnessSensorConfig()
        {
            var responseGet = await Communication.GetBrightnessSensorConfig(IP, PORT, AUTH_TOKEN);
            Assert.That(responseGet.Success, Is.True);
            Assert.That(responseGet.ResponseValue.BrightnessSensorConfig.HasValue, Is.True);
            BrightnessSensorConfig backup = responseGet.ResponseValue.BrightnessSensorConfig.Value;

            var responseSet = await Communication.SetBrightnessSensorConfig(IP, PORT, AUTH_TOKEN, new BrightnessSensorConfig(true, EBrightnessMode.Atmospheric, 95, 5));
            Assert.That(responseSet.Success, Is.True);
            responseGet = await Communication.GetBrightnessSensorConfig(IP, PORT, AUTH_TOKEN);
            Assert.That(responseGet.ResponseValue.BrightnessSensorConfig.HasValue, Is.True);
            var resValue = responseGet.ResponseValue.BrightnessSensorConfig.Value;
            Assert.That(resValue.Enabled, Is.True);
            Assert.That(resValue.BrightnessSensorMode, Is.EqualTo(0));
            Assert.That(resValue.UserMaxBrightness, Is.EqualTo(95));
            Assert.That(resValue.UserMinBrightness, Is.EqualTo(5));

            responseSet = await Communication.SetBrightnessSensorConfig(IP, PORT, AUTH_TOKEN, new BrightnessSensorConfig(backup.Enabled, backup.BrightnessSensorMode, backup.UserMaxBrightness, backup.UserMinBrightness));
            Assert.That(responseSet.Success, Is.True);
        }
    }
}