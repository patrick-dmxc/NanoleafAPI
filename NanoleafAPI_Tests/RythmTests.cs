using Microsoft.Extensions.Logging;
using NanoleafAPI;
using NanoleafAPI.API;
using System.Net;

namespace NanoleafAPI_Tests
{
    public class RythmTests
    {
        private static ILogger? __logger = null;
        const string IP = "192.168.10.152";
        const string PORT = "16021";
        const string AUTH_TOKEN = "7lOFIqsyqmO8c8H2bYco74z4fK2DmXqK";
        [SetUp]
        public void Setup()
        {
            Tools.LoggerFactory = new TestLoggerFactory();
            __logger = Tools.LoggerFactory.CreateLogger(nameof(RythmTests));
            Communication.RegisterIPAddress(IPAddress.Any);
        }
        [Test]
        public async Task TestGetRhythmConnected()
        {
            var response = await Communication.GetRhythmConnected(IP, PORT, AUTH_TOKEN);
            Assert.That(response.Success, Is.True);
        }
        [Test]
        public async Task TestGetRhythmActive()
        {
            var response = await Communication.GetRhythmActive(IP, PORT, AUTH_TOKEN);
            Assert.That(response.Success, Is.True);
        }
        [Test]
        public async Task TestGetRhythmID()
        {
            var response = await Communication.GetRhythmID(IP, PORT, AUTH_TOKEN);
            Assert.That(response.Success, Is.True);
        }
        [Test]
        public async Task TestGetRhythmHardwareVersion()
        {
            var response = await Communication.GetRhythmHardwareVersion(IP, PORT, AUTH_TOKEN);
            Assert.That(response.Success, Is.True);
        }
        [Test]
        public async Task TestGetRhythmFirmwareVersion()
        {
            var response = await Communication.GetRhythmFirmwareVersion(IP, PORT, AUTH_TOKEN);
            Assert.That(response.Success, Is.True);
        }
        [Test]
        public async Task TestGetRhythmAuxAvailable()
        {
            var response = await Communication.GetRhythmAuxAvailable(IP, PORT, AUTH_TOKEN);
            Assert.That(response.Success, Is.True);
        }
        [Test]
        public async Task TestGetRhythmMode()
        {
            var response = await Communication.GetRhythmMode(IP, PORT, AUTH_TOKEN);
            Assert.That(response.Success, Is.True);
        }
        [Test]
        public async Task TestSetRhythmMode()
        {
            var responseGet = await Communication.GetRhythmMode(IP, PORT, AUTH_TOKEN);
            Assert.That(responseGet.Success, Is.True);
            ERythemMode backup = responseGet.ResponseValue;

            var responseSet = await Communication.SetRhythmMode(IP, PORT, AUTH_TOKEN, ERythemMode.AuxCable);
            Assert.That(responseSet.Success, Is.True);
            responseGet = await Communication.GetRhythmMode(IP, PORT, AUTH_TOKEN);
            Assert.That(responseGet.Success, Is.True);
            Assert.That(responseGet.ResponseValue, Is.EqualTo(ERythemMode.AuxCable));

            responseSet = await Communication.SetRhythmMode(IP, PORT, AUTH_TOKEN, ERythemMode.Microphone);
            Assert.That(responseSet.Success, Is.True);
            responseGet = await Communication.GetRhythmMode(IP, PORT, AUTH_TOKEN);
            Assert.That(responseGet.Success, Is.True);
            Assert.That(responseGet.ResponseValue, Is.EqualTo(ERythemMode.Microphone));

            responseSet = await Communication.SetRhythmMode(IP, PORT, AUTH_TOKEN, backup);
            Assert.That(responseSet.Success, Is.True);
            responseGet = await Communication.GetRhythmMode(IP, PORT, AUTH_TOKEN);
            Assert.That(responseGet.Success, Is.True);
            Assert.That(responseGet.ResponseValue, Is.EqualTo(backup));

        }

        [Test]
        public async Task TestGetRhythmPosition()
        {
            var response = await Communication.GetRhythmPosition(IP, PORT, AUTH_TOKEN);
            Assert.That(response.Success, Is.True);
        }
    }
}