using NanoleafAPI;
using System.Net;
using static NanoleafAPI.StateEvent;

namespace NanoleafAPI_Tests
{
    public class CommunicationTests
    {
        const string IP = "192.168.10.152";
        const string PORT = "16021";
        const string AUTH_TOKEN = "7lOFIqsyqmO8c8H2bYco74z4fK2DmXqK";
        [SetUp]
        public void Setup()
        {
            Communication.RegisterIPAddress(IPAddress.Any);
        }


        [Test]
        public async Task TestAddUserAndDeleteUserAsync()
        {
            string? authToken = await Communication.AddUser(IP, PORT);

            Assert.That(authToken, Is.Not.Null);
            Assert.That(Tools.IsTokenValid(authToken), Is.True);
            bool? sucess = await Communication.DeleteUser(IP, PORT, authToken);
            Assert.That(sucess, Is.True);
        }
        [Test]
        public async Task TestManyGetMethodes()
        {
            await Task.Delay(500);
            var info = await Communication.GetAllPanelInfo(IP, PORT, AUTH_TOKEN);
            Assert.Multiple(() =>
            {
                Assert.That(info.SerialNumber, Is.EqualTo("S19124C8036"));
                Assert.That(info.Model, Is.EqualTo("NL29"));
                Assert.That(info.Name, Is.EqualTo("Canvas C097"));
                Assert.That(info.HardwareVersion, Is.EqualTo("2.0-4"));
                Assert.That(info.Manufacturer, Is.EqualTo("Nanoleaf"));
            });

            Assert.That(await Communication.GetStateBrightness(IP, PORT, AUTH_TOKEN), Is.EqualTo(info.State.Brightness.Value));
            Assert.That(await Communication.GetStateSaturation(IP, PORT, AUTH_TOKEN), Is.EqualTo(info.State.Saturation.Value));
            Assert.That(await Communication.GetStateHue(IP, PORT, AUTH_TOKEN), Is.EqualTo(info.State.Hue.Value));
            Assert.That(await Communication.GetStateColorTemperature(IP, PORT, AUTH_TOKEN), Is.EqualTo(info.State.ColorTemprature.Value));
            Assert.That(await Communication.GetColorMode(IP, PORT, AUTH_TOKEN), Is.EqualTo(info.State.ColorMode));
            Assert.That(await Communication.GetStateOnOff(IP, PORT, AUTH_TOKEN), Is.EqualTo(info.State.On.On));
        }

        [Test]
        public async Task TestStreaming()
        {
            await Task.Delay(500);
            var info = await Communication.GetPanelLayoutLayout(IP, PORT, AUTH_TOKEN);

            var externalControlInfo = await Communication.SetExternalControlStreaming(IP, PORT, AUTH_TOKEN, EDeviceType.Canvas);
           // Assert.That(externalControlInfo, Is.Not.Null);

            List<Panel> panels = new List<Panel>();
            var ids = info.PanelPositions.Select(p => p.PanelId);
            foreach (int id in ids)
            {
                var pp = info.PanelPositions.Single(p => p.PanelId.Equals(id));
                panels.Add(new Panel(IP, pp));
            }

            var rgbw = new Panel.RGBW(0, 0, 0, 0);
            byte val = 0;
            do
            {
                rgbw = new Panel.RGBW(val, 0, 0, 0);
                panels.ForEach(p => p.StreamingColor = rgbw);
                var data1 = Communication.CreateStreamingData(panels);
                Assert.That(data1, Is.Not.Null);
                await Communication.SendUDPCommand(externalControlInfo, data1);
                if (val % 2 == 0)
                    await Task.Delay(1);
                val++;
            }
            while (val != 0);

            do
            {
                rgbw = new Panel.RGBW(255, val, val, 0);
                var controlPanel = panels.Where(p => p.Shape == PanelPosition.EShapeType.ControlSquarePrimary).ToList();
                controlPanel.ForEach(p => p.StreamingColor = rgbw);
                var data2 = Communication.CreateStreamingData(controlPanel);
                Assert.That(data2, Is.Not.Null);
                await Communication.SendUDPCommand(externalControlInfo, data2);
                if (val % 2 == 0)
                    await Task.Delay(1);
                val++;
            }
            while (val != 0);
            do
            {
                panels.ForEach(p => p.StreamingColor = new Panel.RGBW((byte)(p.StreamingColor.R - 1), (byte)(p.StreamingColor.G - 1), (byte)(p.StreamingColor.B - 1), 0));
                var data3 = Communication.CreateStreamingData(panels);
                Assert.That(data3, Is.Not.Null);
                await Communication.SendUDPCommand(externalControlInfo, data3);
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
                    p.StreamingColor = new Panel.RGBW(randomValues[0], randomValues[1], randomValues[2], randomValues[3]);
                });
                var data4 = Communication.CreateStreamingData(panels);
                Assert.That(data4, Is.Not.Null);
                await Communication.SendUDPCommand(externalControlInfo, data4);
                await Task.Delay(10);
            }

            rgbw = new Panel.RGBW(0, 0, 0, 0);
            panels.ForEach(p => p.StreamingColor = rgbw);
            var data5 = Communication.CreateStreamingData(panels);
            Assert.That(data5, Is.Not.Null);
            await Communication.SendUDPCommand(externalControlInfo, data5);
        }
        [Test]
        public async Task TestGetSetEffects()
        {
            await Task.Delay(500);
            var list = await Communication.GetEffectList(IP, PORT, AUTH_TOKEN);
            Assert.That(list, Is.Not.Null);
            foreach (string effect in list)
            {
                Assert.That(await Communication.SetSelectedEffect(IP, PORT, AUTH_TOKEN, effect), Is.True);
                var selectedEffect = await Communication.GetSelectedEffect(IP, PORT, AUTH_TOKEN);
                Assert.That(selectedEffect, Is.EqualTo(effect));
            }
        }
        [Test]
        public async Task TestGetSetColorTemperature()
        {
            await Task.Delay(500);
            var backupCT = await Communication.GetStateColorTemperature(IP, PORT, AUTH_TOKEN);
            Assert.That(backupCT, Is.Not.Zero);
            Assert.That(backupCT, Is.Not.Null);
            Assert.That(await Communication.SetStateColorTemperature(IP, PORT, AUTH_TOKEN, 1200), Is.True);
            Assert.That(await Communication.GetStateColorTemperature(IP, PORT, AUTH_TOKEN), Is.EqualTo(1200));
            await Task.Delay(500);

            Assert.That(await Communication.SetStateColorTemperatureIncrement(IP, PORT, AUTH_TOKEN, 5300), Is.True);
            Assert.That(await Communication.GetStateColorTemperature(IP, PORT, AUTH_TOKEN), Is.EqualTo(6500));
            await Task.Delay(500);
            Assert.That(await Communication.SetStateColorTemperature(IP, PORT, AUTH_TOKEN, (ushort)backupCT), Is.True);
            Assert.That(await Communication.GetStateColorTemperature(IP, PORT, AUTH_TOKEN), Is.EqualTo(backupCT));
        }

        [Test]
        public async Task TestGetSetSaturation()
        {
            await Task.Delay(500);
            var backupSat = await Communication.GetStateSaturation(IP, PORT, AUTH_TOKEN);
            Assert.That(backupSat, Is.Not.Null);
            Assert.That(await Communication.SetStateSaturation(IP, PORT, AUTH_TOKEN, 10), Is.True);
            Assert.That(await Communication.GetStateSaturation(IP, PORT, AUTH_TOKEN), Is.EqualTo(10));
            await Task.Delay(500);
            Assert.That(await Communication.SetStateSaturationIncrement(IP, PORT, AUTH_TOKEN, 90), Is.True);
            Assert.That(await Communication.GetStateSaturation(IP, PORT, AUTH_TOKEN), Is.EqualTo(100));
            await Task.Delay(500);
            Assert.That(await Communication.SetStateSaturation(IP, PORT, AUTH_TOKEN, (ushort)backupSat), Is.True);
            Assert.That(await Communication.GetStateSaturation(IP, PORT, AUTH_TOKEN), Is.EqualTo(backupSat));
        }
        [Test]
        public async Task TestGetSetHue()
        {
            await Task.Delay(500);
            var backupHue = await Communication.GetStateHue(IP, PORT, AUTH_TOKEN);
            Assert.That(backupHue, Is.Not.Null);
            Assert.That(await Communication.SetStateHue(IP, PORT, AUTH_TOKEN, 0), Is.True);
            Assert.That(await Communication.GetStateHue(IP, PORT, AUTH_TOKEN), Is.Zero);
            await Task.Delay(500);
            Assert.That(await Communication.SetStateHueIncrement(IP, PORT, AUTH_TOKEN, 180), Is.True);
            Assert.That(await Communication.GetStateHue(IP, PORT, AUTH_TOKEN), Is.EqualTo(180));
            await Task.Delay(500);
            Assert.That(await Communication.SetStateHue(IP, PORT, AUTH_TOKEN, (ushort)backupHue), Is.True);
            Assert.That(await Communication.GetStateHue(IP, PORT, AUTH_TOKEN), Is.EqualTo(backupHue));
        }
        [Test]
        public async Task TestGetSetBrightness()
        {
            await Task.Delay(500);
            var backupBrightness = await Communication.GetStateBrightness(IP, PORT, AUTH_TOKEN);
            Assert.That(backupBrightness, Is.Not.Null);
            Assert.That(await Communication.SetStateBrightness(IP, PORT, AUTH_TOKEN, 0), Is.True);
            Assert.That(await Communication.GetStateBrightness(IP, PORT, AUTH_TOKEN), Is.Zero);
            await Task.Delay(500);
            Assert.That(await Communication.SetStateBrightnessIncrement(IP, PORT, AUTH_TOKEN, 100), Is.True);
            Assert.That(await Communication.GetStateBrightness(IP, PORT, AUTH_TOKEN), Is.EqualTo(100));
            Assert.That(await Communication.SetStateBrightness(IP, PORT, AUTH_TOKEN, 0, 1), Is.True);
            await Task.Delay(1100);
            Assert.That(await Communication.GetStateBrightness(IP, PORT, AUTH_TOKEN), Is.Zero);
            await Task.Delay(500);
            Assert.That(await Communication.SetStateBrightness(IP, PORT, AUTH_TOKEN, (ushort)backupBrightness), Is.True);
            Assert.That(await Communication.GetStateBrightness(IP, PORT, AUTH_TOKEN), Is.EqualTo(backupBrightness));
        }

        [Test]
        public async Task TestGetSetOnOff()
        {
            StateEventArgs? args = null;
            Communication.StaticOnStateEvent += (o, e) => { args = e; };
            Communication.StartEventListener(IP, PORT, AUTH_TOKEN);
            await Task.Delay(5000);
            Assert.That(await Communication.SetStateOnOff(IP, PORT, AUTH_TOKEN, false), Is.True);
            Assert.That(await Communication.GetStateOnOff(IP, PORT, AUTH_TOKEN), Is.False);
            while (args?.StateEvents?.Events?.FirstOrDefault(e => e.Attribute == EAttribute.On) == null)
                await Task.Delay(1);
            Assert.That(args.IP, Is.EqualTo(IP));
            Assert.That(args.StateEvents.Events.First(e => e.Attribute == EAttribute.On).Attribute, Is.EqualTo(EAttribute.On));
            Assert.That(args.StateEvents.Events.First(e => e.Attribute == EAttribute.On).Value, Is.False);
            args = null;

            await Task.Delay(5000);
            Assert.That(await Communication.SetStateOnOff(IP, PORT, AUTH_TOKEN, true), Is.True);
            Assert.That(await Communication.GetStateOnOff(IP, PORT, AUTH_TOKEN), Is.True);
            while (args?.StateEvents?.Events?.FirstOrDefault(e => e.Attribute == EAttribute.On) == null)
                await Task.Delay(1);
            Assert.That(args.IP, Is.EqualTo(IP));
            Assert.That(args.StateEvents.Events.First(e => e.Attribute == EAttribute.On).Attribute, Is.EqualTo(EAttribute.On));
            Assert.That(args.StateEvents.Events.First(e => e.Attribute == EAttribute.On).Value, Is.True);
            args = null;
            await Task.Delay(5000);
        }
        [Test]
        public async Task TestGetSetGlobalOrientation()
        {
            LayoutEventArgs? args = null;
            await Task.Delay(500);
            Communication.StaticOnLayoutEvent += (o, e) => { args = e; };
            Communication.StartEventListener(IP, PORT, AUTH_TOKEN);
            var backupGlobalOrientation = await Communication.GetPanelLayoutGlobalOrientation(IP, PORT, AUTH_TOKEN);

            Assert.That(await Communication.SetPanelLayoutGlobalOrientation(IP, PORT, AUTH_TOKEN, 120), Is.True);
            Assert.That(await Communication.GetPanelLayoutGlobalOrientation(IP, PORT, AUTH_TOKEN), Is.EqualTo(120));
            while (args == null)
                await Task.Delay(1);
            Assert.That(args.IP, Is.EqualTo(IP));
            Assert.That(args.LayoutEvent.GlobalOrientation, Is.EqualTo(120));
            args = null;

            await Task.Delay(500);
            Assert.That(await Communication.SetPanelLayoutGlobalOrientation(IP, PORT, AUTH_TOKEN, 270), Is.True);
            Assert.That(await Communication.GetPanelLayoutGlobalOrientation(IP, PORT, AUTH_TOKEN), Is.EqualTo(270));
            while (args == null)
                await Task.Delay(1);
            Assert.That(args.IP, Is.EqualTo(IP));
            Assert.That(args.LayoutEvent.GlobalOrientation, Is.EqualTo(270));
            args = null;

            await Task.Delay(500);
            Assert.That(await Communication.SetPanelLayoutGlobalOrientation(IP, PORT, AUTH_TOKEN, 0), Is.True);
            Assert.That(await Communication.GetPanelLayoutGlobalOrientation(IP, PORT, AUTH_TOKEN), Is.Zero);
            while (args == null)
                await Task.Delay(1);
            Assert.That(args.IP, Is.EqualTo(IP));
            Assert.That(args.LayoutEvent.GlobalOrientation, Is.Zero);
            args = null;
        }

        [Test]
        public async Task TestIdentify()
        {
            await Task.Delay(500);
            Assert.That(await Communication.Identify(IP, PORT, AUTH_TOKEN), Is.True);
            await Task.Delay(5000);
        }
    }
}