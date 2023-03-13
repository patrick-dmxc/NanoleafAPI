using NanoleafAPI;
using System.Text.Json;

namespace NanoleafAPI_Tests
{
    public class ToolsSerializer
    {

        [SetUp]
        public void Setup()
        {
            Tools.LoggerFactory = new TestLoggerFactory();
        }


        [Test]
        public void TestDiscoveredDevice()
        {
            var dd = new DiscoveredDevice("192.168.1.111", "16021", "Shapes", "wweerr", EDeviceType.Shapes);
            string json = JsonSerializer.Serialize(dd);

            var des= JsonSerializer.Deserialize<DiscoveredDevice>(json);

            Assert.That(des.IP, Is.EqualTo(dd.IP));
            Assert.That(des.ID, Is.EqualTo(dd.ID));
            Assert.That(des.Name, Is.EqualTo(dd.Name));
            Assert.That(des.Port, Is.EqualTo(dd.Port));
            Assert.That(des.DeviceType, Is.EqualTo(dd.DeviceType));
        }
    }
}