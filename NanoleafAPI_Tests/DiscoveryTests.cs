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
            //Communication.RegisterIPAddress(new IPAddress(new byte[] { 192, 168, 1, 13 }));
            //Communication.RegisterIPAddress(new IPAddress(new byte[] { 192, 168, 10, 254 }));
            //Communication.RegisterIPAddress(IPAddress.Loopback);
            Communication.RegisterIPAddress(IPAddress.Any);
            //Communication.RegisterIPAddress(new IPAddress(new byte[] { 192, 168, 10, 76 }));
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
            Assert.IsTrue(eventFired);
            Assert.AreEqual(EDeviceType.Canvas, Communication.DiscoveredDevices.First().DeviceTyp);
            Assert.AreEqual("Canvas C097", Communication.DiscoveredDevices.First().Name);
            Assert.AreEqual(IP, Communication.DiscoveredDevices.First().IP);

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
            Assert.IsTrue(eventFired);
            Assert.AreEqual(EDeviceType.Canvas, Communication.DiscoveredDevices.First().DeviceTyp);
            Assert.AreEqual("Canvas C097", Communication.DiscoveredDevices.First().Name);
            Assert.AreEqual(IP, Communication.DiscoveredDevices.First().IP);

            Communication.StopDiscoverymDNSTask();
        }
    }
}