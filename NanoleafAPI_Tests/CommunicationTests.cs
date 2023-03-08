using NanoleafAPI;
using System.Net;
using EAttribute_StateEvent = NanoleafAPI.StateEvent.EAttribute;
using EAttribute_LayoutEvent = NanoleafAPI.LayoutEvent.EAttribute;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace NanoleafAPI_Tests
{
    public class CommunicationTests
    {
        private static ILogger? __logger = null;
        const string IP = "192.168.10.152";
        const string PORT = "16021";
        const string AUTH_TOKEN = "7lOFIqsyqmO8c8H2bYco74z4fK2DmXqK";
        [SetUp]
        public void Setup()
        {
            Tools.LoggerFactory = new TestLoggerFactory();
            __logger = Tools.LoggerFactory.CreateLogger(nameof(Communication));
            Communication.RegisterIPAddress(IPAddress.Any);
        }
        [Test]
        public async Task TestPing()
        {
            Stopwatch sw = new Stopwatch();
            for (int i = 0; i < 10; i++)
            {
                sw.Restart();
                var response = await Communication.Ping(IP, PORT);
                sw.Stop();
                Assert.That(response.Success, Is.True);
                __logger?.LogDebug($"Ping took: {sw.ElapsedMilliseconds}ms");
            }
        }
        [Test]
        public async Task TestPingFreakOut()
        {
            int countErrors = 0;
            Stopwatch sw = new Stopwatch();
            for (int i = 0; i < 1000; i++)
            {
                sw.Restart();
                var response = await Communication.Ping(IP, PORT);
                sw.Stop();
                if (!response.Success)
                    countErrors++;
                __logger?.LogDebug($"Ping took: {sw.ElapsedMilliseconds}ms");
            }
            Assert.That(countErrors, Is.InRange(0, 10));
            __logger?.LogDebug($"Error-Count: {countErrors}");
        }

        [Test]
        public async Task TestAddUserAndDeleteUserAsync()
        {
            for (int i = 0; i < 10; i++)
            {
                var responseAdd = await Communication.AddUser(IP, PORT);

                Assert.That(responseAdd.Success, Is.True);
                string? authToken = responseAdd.ResponseValue.AuthToken;

                Assert.That(authToken, Is.Not.Null);
                Assert.That(Tools.IsTokenValid(authToken), Is.True);
                var responseDelete = await Communication.DeleteUser(IP, PORT, authToken);
                Assert.That(responseDelete.Success, Is.True);
                await Task.Delay(100);
            }
        }
        [Test]
        public async Task TestManyGetMethodes()
        {
            await Task.Delay(500);
            var response = await Communication.GetAllPanelInfo(IP, PORT, AUTH_TOKEN);
            Assert.That(response.Success, Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(response.ResponseValue.SerialNumber, Is.EqualTo("S19124C8036"));
                Assert.That(response.ResponseValue.Model, Is.EqualTo("NL29"));
                Assert.That(response.ResponseValue.Name, Is.EqualTo("Canvas C097"));
                Assert.That(response.ResponseValue.HardwareVersion, Is.EqualTo("2.0-4"));
                Assert.That(response.ResponseValue.Manufacturer, Is.EqualTo("Nanoleaf"));
            });

            var responseGetStateBrightness = await Communication.GetStateBrightness(IP, PORT, AUTH_TOKEN);
            Assert.That(responseGetStateBrightness.Success, Is.True);
            Assert.That(responseGetStateBrightness.ResponseValue.Value, Is.EqualTo(response.ResponseValue.State.Brightness.Value));

            var responseGetStateSaturation = await Communication.GetStateSaturation(IP, PORT, AUTH_TOKEN);
            Assert.That(responseGetStateSaturation.Success, Is.True);
            Assert.That(responseGetStateSaturation.ResponseValue.Value, Is.EqualTo(response.ResponseValue.State.Saturation.Value));

            var responseGetStateHue = await Communication.GetStateHue(IP, PORT, AUTH_TOKEN);
            Assert.That(responseGetStateHue.Success, Is.True);
            Assert.That(responseGetStateHue.ResponseValue.Value, Is.EqualTo(response.ResponseValue.State.Hue.Value));

            var responseGetStateColorTemperature = await Communication.GetStateColorTemperature(IP, PORT, AUTH_TOKEN);
            Assert.That(responseGetStateColorTemperature.Success, Is.True);
            Assert.That(responseGetStateColorTemperature.ResponseValue.Value, Is.EqualTo(response.ResponseValue.State.ColorTemprature.Value));

            var responseGetColorMode = await Communication.GetColorMode(IP, PORT, AUTH_TOKEN);
            Assert.That(responseGetColorMode.Success, Is.True);
            Assert.That(responseGetColorMode.ResponseValue, Is.EqualTo(response.ResponseValue.State.ColorMode));

            var responseGetOnOff = await Communication.GetStateOnOff(IP, PORT, AUTH_TOKEN);
            Assert.That(responseGetOnOff.Success, Is.True);
            Assert.That(responseGetOnOff.ResponseValue.On, Is.EqualTo(response.ResponseValue.State.On.On));
        }

        [Test]
        public async Task TestStreaming()
        {
            await Task.Delay(500);
            var response = await Communication.GetPanelLayoutLayout(IP, PORT, AUTH_TOKEN);
            Assert.That(response.Success, Is.True);
            var info = response.ResponseValue;

            var responseSetExternalControlStreaming = await Communication.SetExternalControlStreaming(IP, PORT, AUTH_TOKEN, EDeviceType.Canvas);
            Assert.That(responseSetExternalControlStreaming.Success, Is.True);

            List<Panel> panels = new List<Panel>();
            var ids = info.PanelPositions.Select(p => p.PanelId);
            foreach (int id in ids)
            {
                var pp = info.PanelPositions.Single(p => p.PanelId.Equals(id));
                panels.Add(new Panel(IP, pp));
            }

            var rgbw = new RGBW(0, 0, 0, 0);
            byte val = 0;
            do
            {
                rgbw = new RGBW(val, 0, 0, 0);
                panels.ForEach(p => p.StreamingColor = rgbw);
                var data1 = Communication.CreateStreamingData(panels);
                Assert.That(data1, Is.Not.Null);
                await Communication.SendUDPCommand(responseSetExternalControlStreaming.ResponseValue, data1);
                if (val % 2 == 0)
                    await Task.Delay(1);
                val++;
            }
            while (val != 0);

            do
            {
                rgbw = new RGBW(255, val, val, 0);
                var controlPanel = panels.Where(p => p.Shape == PanelPosition.EShapeType.ControlSquarePrimary).ToList();
                controlPanel.ForEach(p => p.StreamingColor = rgbw);
                var data2 = Communication.CreateStreamingData(controlPanel);
                Assert.That(data2, Is.Not.Null);
                await Communication.SendUDPCommand(responseSetExternalControlStreaming.ResponseValue, data2);
                if (val % 2 == 0)
                    await Task.Delay(1);
                val++;
            }
            while (val != 0);
            do
            {
                panels.ForEach(p => p.StreamingColor = new RGBW((byte)(p.StreamingColor.R - 1), (byte)(p.StreamingColor.G - 1), (byte)(p.StreamingColor.B - 1), 0));
                var data3 = Communication.CreateStreamingData(panels);
                Assert.That(data3, Is.Not.Null);
                await Communication.SendUDPCommand(responseSetExternalControlStreaming.ResponseValue, data3);
                if (val % 2 == 0)
                    await Task.Delay(1);
                val++;
            }
            while (panels.First().StreamingColor.R != 0);

            byte[] randomValues = new byte[4];
            for (byte b = 0; b < byte.MaxValue; b++)
            {
                panels.ForEach(p =>
                {
                    Random.Shared.NextBytes(randomValues);
                    p.StreamingColor = new RGBW(randomValues[0], randomValues[1], randomValues[2], randomValues[3]);
                });
                var data4 = Communication.CreateStreamingData(panels);
                Assert.That(data4, Is.Not.Null);
                await Communication.SendUDPCommand(responseSetExternalControlStreaming.ResponseValue, data4);
                await Task.Delay(10);
            }

            rgbw = new RGBW(0, 0, 0, 0);
            panels.ForEach(p => p.StreamingColor = rgbw);
            var data5 = Communication.CreateStreamingData(panels);
            Assert.That(data5, Is.Not.Null);
            await Communication.SendUDPCommand(responseSetExternalControlStreaming.ResponseValue, data5);
        }
        [Test]
        public async Task TestGetSetEffects()
        {
            await Task.Delay(500);
            var response = await Communication.GetEffectList(IP, PORT, AUTH_TOKEN);
            Assert.That(response.Success, Is.True);

            foreach (string effect in response.ResponseValue!)
            {
                var responseSet = await Communication.SetSelectedEffect(IP, PORT, AUTH_TOKEN, effect);
                Assert.That(responseSet.Success, Is.True);
                var responseGet= await Communication.GetSelectedEffect(IP, PORT, AUTH_TOKEN);
                Assert.That(responseGet.Success, Is.True);
                Assert.That(responseGet.ResponseValue, Is.EqualTo(effect));
            }
        }
        [Test]
        public async Task TestGetSetColorTemperature()
        {
            await Task.Delay(500);
            var responseGet = await Communication.GetStateColorTemperature(IP, PORT, AUTH_TOKEN);
            Assert.That(responseGet.Success, Is.True);
            var backupCT = responseGet.ResponseValue;
            Assert.That(backupCT, Is.Not.Zero);

            var responseSet = await Communication.SetStateColorTemperature(IP, PORT, AUTH_TOKEN, 1200);
            Assert.That(responseSet.Success, Is.True);

            responseGet = await Communication.GetStateColorTemperature(IP, PORT, AUTH_TOKEN);
            Assert.That(responseGet.Success, Is.True);
            Assert.That(responseGet.ResponseValue.Value, Is.EqualTo(1200));

            await Task.Delay(500);

            responseSet = await Communication.SetStateColorTemperatureIncrement(IP, PORT, AUTH_TOKEN, 5300);
            Assert.That(responseSet.Success, Is.True);

            responseGet = await Communication.GetStateColorTemperature(IP, PORT, AUTH_TOKEN);
            Assert.That(responseGet.Success, Is.True);
            Assert.That(responseGet.ResponseValue.Value, Is.EqualTo(6500));

            await Task.Delay(500);

            responseSet = await Communication.SetStateColorTemperature(IP, PORT, AUTH_TOKEN, backupCT.Value);
            Assert.That(responseSet.Success, Is.True);

            responseGet = await Communication.GetStateColorTemperature(IP, PORT, AUTH_TOKEN);
            Assert.That(responseGet.Success, Is.True);
            Assert.That(responseGet.ResponseValue.Value, Is.EqualTo(backupCT.Value));
        }

        [Test]
        public async Task TestGetSetSaturation()
        {
            await Task.Delay(500);
            var responseGet = await Communication.GetStateSaturation(IP, PORT, AUTH_TOKEN);
            Assert.That(responseGet.Success, Is.True);
            var backupSat = responseGet.ResponseValue;

            var responseSet = await Communication.SetStateSaturation(IP, PORT, AUTH_TOKEN, 10);
            Assert.That(responseSet.Success, Is.True);

            responseGet = await Communication.GetStateSaturation(IP, PORT, AUTH_TOKEN);
            Assert.That(responseGet.Success, Is.True);
            Assert.That(responseGet.ResponseValue.Value, Is.EqualTo(10));

            await Task.Delay(500);

            responseSet = await Communication.SetStateSaturationIncrement(IP, PORT, AUTH_TOKEN, 90);
            Assert.That(responseSet.Success, Is.True);

            responseGet = await Communication.GetStateSaturation(IP, PORT, AUTH_TOKEN);
            Assert.That(responseGet.Success, Is.True);
            Assert.That(responseGet.ResponseValue.Value, Is.EqualTo(100));

            await Task.Delay(500);

            responseSet = await Communication.SetStateSaturation(IP, PORT, AUTH_TOKEN, backupSat.Value);
            Assert.That(responseSet.Success, Is.True);

            responseGet = await Communication.GetStateSaturation(IP, PORT, AUTH_TOKEN);
            Assert.That(responseGet.Success, Is.True);
            Assert.That(responseGet.ResponseValue.Value, Is.EqualTo(backupSat.Value));
        }
        [Test]
        public async Task TestGetSetHue()
        {
            await Task.Delay(500);
            var responseGet = await Communication.GetStateHue(IP, PORT, AUTH_TOKEN);
            Assert.That(responseGet.Success, Is.True);
            var backupHue = responseGet.ResponseValue;

            var responseSet = await Communication.SetStateHue(IP, PORT, AUTH_TOKEN, 0);
            Assert.That(responseSet.Success, Is.True);

            responseGet = await Communication.GetStateHue(IP, PORT, AUTH_TOKEN);
            Assert.That(responseGet.Success, Is.True);
            Assert.That(responseGet.ResponseValue.Value, Is.Zero);

            await Task.Delay(500);

            responseSet = await Communication.SetStateHueIncrement(IP, PORT, AUTH_TOKEN, 180);
            Assert.That(responseSet.Success, Is.True);

            responseGet = await Communication.GetStateHue(IP, PORT, AUTH_TOKEN);
            Assert.That(responseGet.Success, Is.True);
            Assert.That(responseGet.ResponseValue.Value, Is.EqualTo(180));

            await Task.Delay(500);

            responseSet = await Communication.SetStateHue(IP, PORT, AUTH_TOKEN, backupHue.Value);
            Assert.That(responseSet.Success, Is.True);

            responseGet = await Communication.GetStateHue(IP, PORT, AUTH_TOKEN);
            Assert.That(responseGet.Success, Is.True);
            Assert.That(responseGet.ResponseValue.Value, Is.EqualTo(backupHue.Value));
        }
        [Test]
        public async Task TestGetSetBrightness()
        {
            await Task.Delay(500);
            var responseGet = await Communication.GetStateBrightness(IP, PORT, AUTH_TOKEN);
            Assert.That(responseGet.Success, Is.True);
            var backupBrightness = responseGet.ResponseValue;

            var responseSet = await Communication.SetStateBrightness(IP, PORT, AUTH_TOKEN, 0);
            Assert.That(responseSet.Success, Is.True);

            responseGet = await Communication.GetStateBrightness(IP, PORT, AUTH_TOKEN);
            Assert.That(responseGet.Success, Is.True);
            Assert.That(responseGet.ResponseValue.Value, Is.Zero);

            await Task.Delay(500);

            responseSet = await Communication.SetStateBrightnessIncrement(IP, PORT, AUTH_TOKEN, 100);
            Assert.That(responseSet.Success, Is.True);
            await Task.Delay(1100);

            responseGet = await Communication.GetStateBrightness(IP, PORT, AUTH_TOKEN);
            Assert.That(responseGet.Success, Is.True);
            Assert.That(responseGet.ResponseValue.Value, Is.EqualTo(100));

            responseSet = await Communication.SetStateBrightness(IP, PORT, AUTH_TOKEN, 0, 1);
            Assert.That(responseSet.Success, Is.True);

            await Task.Delay(1100);

            responseGet = await Communication.GetStateBrightness(IP, PORT, AUTH_TOKEN);
            Assert.That(responseGet.Success, Is.True);
            Assert.That(responseGet.ResponseValue.Value, Is.Zero);

            await Task.Delay(500);

            responseSet = await Communication.SetStateBrightness(IP, PORT, AUTH_TOKEN, backupBrightness.Value);
            Assert.That(responseSet.Success, Is.True);

            responseGet = await Communication.GetStateBrightness(IP, PORT, AUTH_TOKEN);
            Assert.That(responseGet.Success, Is.True);
            Assert.That(responseGet.ResponseValue.Value, Is.EqualTo(backupBrightness.Value));
        }

        [Test]
        public async Task TestGetSetOnOff()
        {
            await Communication.SetStateOnOff(IP, PORT, AUTH_TOKEN, true);
            await Task.Delay(1000);
            StateEventArgs? args = null;
            Communication.StaticOnStateEvent += (o, e) => { args = e; };
            Communication.StartEventListener(IP, PORT, AUTH_TOKEN);
            await Task.Delay(2000);

            var responseSet = await Communication.SetStateOnOff(IP, PORT, AUTH_TOKEN, false);
            Assert.That(responseSet.Success, Is.True);

            var responseGet = await Communication.GetStateOnOff(IP, PORT, AUTH_TOKEN);
            Assert.That(responseGet.Success, Is.True);
            Assert.That(responseGet.ResponseValue.On, Is.False);
            while (args?.StateEvents.Events?.FirstOrDefault(e => e.Attribute == EAttribute_StateEvent.On) == null)
                await Task.Delay(1);
            Assert.That(args.IP, Is.EqualTo(IP));
            Assert.That(args.StateEvents.Events.First(e => e.Attribute == EAttribute_StateEvent.On).Attribute, Is.EqualTo(EAttribute_StateEvent.On));
            Assert.That(args.StateEvents.Events.First(e => e.Attribute == EAttribute_StateEvent.On).On, Is.False);
            args = null;

            await Task.Delay(2000);

            responseSet = await Communication.SetStateOnOff(IP, PORT, AUTH_TOKEN, true);
            Assert.That(responseSet.Success, Is.True);

            responseGet = await Communication.GetStateOnOff(IP, PORT, AUTH_TOKEN);
            Assert.That(responseGet.Success, Is.True);
            Assert.That(responseGet.ResponseValue.On, Is.True);
            while (args?.StateEvents.Events?.FirstOrDefault(e => e.Attribute == EAttribute_StateEvent.On) == null)
                await Task.Delay(1);
            Assert.That(args.IP, Is.EqualTo(IP));
            Assert.That(args.StateEvents.Events.First(e => e.Attribute == EAttribute_StateEvent.On).Attribute, Is.EqualTo(EAttribute_StateEvent.On));
            Assert.That(args.StateEvents.Events.First(e => e.Attribute == EAttribute_StateEvent.On).On, Is.True);
            args = null;
        }
        [Test]
        public async Task TestGetSetGlobalOrientation()
        {
            LayoutEventArgs? args = null;
            await Task.Delay(500);
            Communication.StaticOnLayoutEvent += (o, e) => { args = e; };
            Communication.StartEventListener(IP, PORT, AUTH_TOKEN);

            var responseGet = await Communication.GetPanelLayoutGlobalOrientation(IP, PORT, AUTH_TOKEN);
            Assert.That(responseGet.Success, Is.True);
            var backupGlobalOrientation = responseGet.ResponseValue;

            var responseSet = await Communication.SetPanelLayoutGlobalOrientation(IP, PORT, AUTH_TOKEN, 120);
            Assert.That(responseSet.Success, Is.True);

            responseGet = await Communication.GetPanelLayoutGlobalOrientation(IP, PORT, AUTH_TOKEN);
            Assert.That(responseGet.Success, Is.True);
            Assert.That(responseGet.ResponseValue.Value, Is.EqualTo(120));
            while (args == null)
                await Task.Delay(1);
            Assert.That(args.IP, Is.EqualTo(IP));
            Assert.That(args.LayoutEvents.Events.First(e => e.Attribute == EAttribute_LayoutEvent.GlobalOrientation).GlobalOrientation, Is.EqualTo(120));
            args = null;

            await Task.Delay(500);

            responseSet = await Communication.SetPanelLayoutGlobalOrientation(IP, PORT, AUTH_TOKEN, 270);
            Assert.That(responseSet.Success, Is.True);

            responseGet = await Communication.GetPanelLayoutGlobalOrientation(IP, PORT, AUTH_TOKEN);
            Assert.That(responseGet.Success, Is.True);
            Assert.That(responseGet.ResponseValue.Value, Is.EqualTo(270));
            while (args == null)
                await Task.Delay(1);
            Assert.That(args.IP, Is.EqualTo(IP));
            Assert.That(args.LayoutEvents.Events.First(e => e.Attribute == EAttribute_LayoutEvent.GlobalOrientation).GlobalOrientation, Is.EqualTo(270));
            args = null;

            await Task.Delay(500);

            responseSet = await Communication.SetPanelLayoutGlobalOrientation(IP, PORT, AUTH_TOKEN, 0);
            Assert.That(responseSet.Success, Is.True);

            responseGet = await Communication.GetPanelLayoutGlobalOrientation(IP, PORT, AUTH_TOKEN);
            Assert.That(responseGet.Success, Is.True);
            Assert.That(responseGet.ResponseValue.Value, Is.Zero);
            while (args == null)
                await Task.Delay(1);
            Assert.That(args.IP, Is.EqualTo(IP));
            Assert.That(args.LayoutEvents.Events.First(e => e.Attribute == EAttribute_LayoutEvent.GlobalOrientation).GlobalOrientation, Is.Zero);
            args = null;
        }

        [Test]
        public async Task TestIdentify()
        {
            await Task.Delay(500);
            var response = await Communication.Identify(IP, PORT, AUTH_TOKEN);
            Assert.That(response.Success, Is.True);
        }
        [Test]
        public async Task TestIdentifyAndroid()
        {
            await Task.Delay(500);
            var response = await Communication.IdentifyAndroid(IP);
            Assert.That(response.Success, Is.True);
        }

        [Test]
        public async Task TestFirmwareUpgrade()
        {
            await Task.Delay(500);
            var response = await Communication.GetFirmwareUpgrade(IP, PORT, AUTH_TOKEN);
            Assert.That(response.Success, Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(response.ResponseValue.NewFirmwareVersion, Is.Null);
                Assert.That(response.ResponseValue.FirmwareAvailability, Is.False);
            });
        }
        [Test]
        public async Task TestCommands()
        {
            await Task.Delay(500);
            Assert.That(await Communication.GetTouchConfig(IP, PORT, AUTH_TOKEN), Is.Not.Null);
            Assert.That(await Communication.GetTouchKillSwitch(IP, PORT, AUTH_TOKEN), Is.Not.Null);
        }
        [Test]
        public async Task TestRequestAll()
        {
            await Task.Delay(500);
            var response= await Communication.GetRequerstAll(IP, PORT, AUTH_TOKEN);
            Assert.That(response.Success, Is.True);
        }
    }
}