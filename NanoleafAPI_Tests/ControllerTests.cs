using NanoleafAPI;

namespace NanoleafAPI_Tests
{
    public class ControllerTests
    {
        const string IP = "192.168.10.152";
        const string PORT = "16021";
        const string AUTH_TOKEN = "7lOFIqsyqmO8c8H2bYco74z4fK2DmXqK";

        [SetUp]
        public void Setup()
        {
            Tools.LoggerFactory = new TestLoggerFactory();
        }
        [Test]
        public async Task TestControler()
        {
            Controller c = new Controller(IP, PORT,AUTH_TOKEN);
            await Task.Delay(6000);
            Assert.Multiple(() =>
            {
                Assert.That(c.Reachable, Is.True, "Reachable");
                Assert.That(c.SerialNumber, Is.EqualTo("S19124C8036"), "SerialNumber");
                Assert.That(c.Model, Is.EqualTo("NL29"), "Model");
                Assert.That(c.Name, Is.EqualTo("Canvas C097"), "Name");
                Assert.That(c.HardwareVersion, Is.EqualTo("2.0-4"), "HardwareVersion");
                Assert.That(c.Manufacturer, Is.EqualTo("Nanoleaf"), "Manufacturer");
            });
            await Task.Delay(6000);
            Assert.That(c.StreamingStarted, Is.True, "Stream");
        }
    }
}