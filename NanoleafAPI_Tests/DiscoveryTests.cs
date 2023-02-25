using NanoleafAPI;
using static NanoleafAPI.StateEvent;
using System.Diagnostics;
using System.Net;

namespace NanoleafAPI_Tests
{
    public class Tests
    {
        const string IP = "192.168.10.152";
        [SetUp]
        public void Setup()
        {
            Tools.LoggerFactory = new TestLoggerFactory();

            Communication.RegisterIPAddress(IPAddress.Any);
        }

        [Test]
        public async Task TestDiscovery_SSDP()
        {
            await Task.Delay(500);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            bool eventFired = false;
            Communication.DeviceDiscovered += (o, e) => { eventFired = true; };
            Communication.StartDiscoverySSDPTask();

            while (Communication.DiscoveredDevices.Count == 0)
            {
                if (sw.Elapsed.TotalSeconds > 180)
                    Assert.Fail("Discover should be done in max 1 minute!");
                else
                    await Task.Delay(1);
            }
            Assert.That(eventFired, Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(Communication.DiscoveredDevices.First().DeviceTyp, Is.EqualTo(EDeviceType.Canvas));
                Assert.That(Communication.DiscoveredDevices.First().Name, Is.EqualTo("Canvas C097"));
                Assert.That(Communication.DiscoveredDevices.First().IP, Is.EqualTo(IP));
            });
            Communication.StopDiscoverySSDPTask();
        }

        [Test]
        public async Task TestDiscovery_mDNS()
        {
            await Task.Delay(500);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            bool eventFired = false;
            Communication.DeviceDiscovered += (o, e) => { eventFired = true; };
            Communication.StartDiscoverymDNSTask();

            while (Communication.DiscoveredDevices.Count == 0)
            {
                if (sw.Elapsed.TotalSeconds > 180)
                    Assert.Fail("Discover should be done in max 1 minute!");
                else
                    await Task.Delay(1);
            }
            Assert.That(eventFired, Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(Communication.DiscoveredDevices.First().DeviceTyp, Is.EqualTo(EDeviceType.Canvas));
                Assert.That(Communication.DiscoveredDevices.First().Name, Is.EqualTo("Canvas C097"));
                Assert.That(Communication.DiscoveredDevices.First().IP, Is.EqualTo(IP));
            });
            Communication.StopDiscoverymDNSTask();
        }
    }
}