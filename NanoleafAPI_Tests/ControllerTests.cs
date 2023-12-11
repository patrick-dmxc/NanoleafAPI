using NanoleafAPI;
using System.Text.Json;

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
            await Task.Delay(1000);
            Assert.Multiple(() =>
            {
                Assert.That(c.Reachable, Is.True, "Reachable");
                Assert.That(c.SerialNumber, Is.EqualTo("S19124C8036"), "SerialNumber");
                Assert.That(c.Model, Is.EqualTo("NL29"), "Model");
                Assert.That(c.Name, Is.EqualTo("Canvas C097"), "Name");
                Assert.That(c.HardwareVersion, Is.EqualTo("2.0-4"), "HardwareVersion");
                Assert.That(c.Manufacturer, Is.EqualTo("Nanoleaf"), "Manufacturer");
            });
            Assert.That(c.StreamingStarted, Is.False, "Stream");
            await c.StartStreaming();
            Assert.That(c.StreamingStarted, Is.True, "Stream");
            await Task.Delay(1000);
            foreach (var panel in c.Panels)
                panel.StreamingColor= new RGBW(255,0,255);
            await Task.Delay(1000);
            Assert.That(c.StreamingStarted, Is.True, "Stream");
            foreach (var panel in c.Panels)
                panel.StreamingColor = new RGBW(0, 0, 255);
            await Task.Delay(1000);
            foreach (var panel in c.Panels)
                panel.StreamingColor = new RGBW(255, 0, 0);
            await Task.Delay(1000);
            foreach (var panel in c.Panels)
                panel.StreamingColor = new RGBW(0, 255, 0);
        }
        [Test]
        public async Task TestControlerJSON()
        {
            Controller c = new Controller(IP, PORT, AUTH_TOKEN);
            await Task.Delay(6000);
            string json=JsonSerializer.Serialize(c);

            Controller des= JsonSerializer.Deserialize<Controller>(json)!;
        }

        [Test]
        public void TestControlerJSONDeserialize()
        {
            string jsonControllers = "[{\"IP\":\"192.168.10.152\",\"Port\":\"16021\",\"Auth_token\":\"wwS6B8Meg3O2AlLXhBym9yVHPuPgKGvW\",\"Name\":\"Canvas C097\",\"Model\":\"NL29\",\"Manufacturer\":\"Nanoleaf\",\"SerialNumber\":\"S19124C8036\",\"HardwareVersion\":\"2.0-4\",\"FirmwareVersion\":\"7.1.0\",\"DeviceType\":2,\"NumberOfPanels\":9,\"GlobalOrientation\":0,\"GlobalOrientationStored\":0,\"GlobalOrientationMin\":0,\"GlobalOrientationMax\":360,\"EffectList\":[\"*ExtControl*\",\"Color Burst333\",\"DancingTiles\",\"Dynamic 26-4-2021 20:32\",\"EDM Strobe\",\"Electric Chill\",\"Energize\",\"Falling Whites\",\"Fireworks\",\"Flames\",\"Forest\",\"Inner Peace\",\"Magic Strobe\",\"Meteor Shower\",\"Nemo\",\"Northern Lights\",\"Paint Splatter\",\"Pulse Pop Beats\",\"Radial Sound Bar\",\"Red Beat\",\"Red Meteor\",\"Rhythmic Northern Lights\",\"Romantic\",\"Shooting Stars\",\"Soda\",\"Sound Bar\",\"Streaking Notes\",\"Strobe\",\"Sub 49 Strobe Fireworks\",\"Super Strobe\",\"Thunder\",\"Vader\",\"eeee\",\"\\u661F\\u591CStarry Night\"],\"SelectedEffect\":\"*ExtControl*\",\"SelectedEffectStored\":\"Northern Lights\",\"PowerOn\":true,\"PowerOff\":false,\"PowerOnStored\":true,\"Reachable\":true,\"StreamingStarted\":true,\"Brightness\":100,\"BrightnessStored\":100,\"BrightnessMin\":0,\"BrightnessMax\":100,\"Hue\":0,\"HueStored\":0,\"HueMin\":0,\"HueMax\":360,\"Saturation\":0,\"SaturationStored\":0,\"SaturationMin\":0,\"SaturationMax\":100,\"ColorTemprature\":5000,\"ColorTempratureStored\":5000,\"ColorTempratureMin\":1200,\"ColorTempratureMax\":6500,\"ColorMode\":\"effect\",\"ColorModeStored\":\"effect\",\"Panels\":[{\"IP\":\"192.168.10.152\",\"ID\":47583,\"X\":600,\"Y\":0,\"Orientation\":0,\"Shape\":3,\"StreamingColor\":{},\"LastUpdate\":0,\"SideLength\":100},{\"IP\":\"192.168.10.152\",\"ID\":35641,\"X\":500,\"Y\":0,\"Orientation\":270,\"Shape\":2,\"StreamingColor\":{},\"LastUpdate\":0,\"SideLength\":100},{\"IP\":\"192.168.10.152\",\"ID\":12783,\"X\":400,\"Y\":0,\"Orientation\":270,\"Shape\":2,\"StreamingColor\":{},\"LastUpdate\":0,\"SideLength\":100},{\"IP\":\"192.168.10.152\",\"ID\":43562,\"X\":300,\"Y\":0,\"Orientation\":270,\"Shape\":2,\"StreamingColor\":{},\"LastUpdate\":0,\"SideLength\":100},{\"IP\":\"192.168.10.152\",\"ID\":30232,\"X\":200,\"Y\":0,\"Orientation\":270,\"Shape\":2,\"StreamingColor\":{},\"LastUpdate\":0,\"SideLength\":100},{\"IP\":\"192.168.10.152\",\"ID\":5439,\"X\":100,\"Y\":0,\"Orientation\":270,\"Shape\":2,\"StreamingColor\":{},\"LastUpdate\":0,\"SideLength\":100},{\"IP\":\"192.168.10.152\",\"ID\":24381,\"X\":0,\"Y\":0,\"Orientation\":270,\"Shape\":2,\"StreamingColor\":{},\"LastUpdate\":0,\"SideLength\":100},{\"IP\":\"192.168.10.152\",\"ID\":65307,\"X\":0,\"Y\":0,\"Orientation\":270,\"Shape\":2,\"StreamingColor\":{},\"LastUpdate\":0,\"SideLength\":100},{\"IP\":\"192.168.10.152\",\"ID\":42857,\"X\":250,\"Y\":0,\"Orientation\":90,\"Shape\":2,\"StreamingColor\":{},\"LastUpdate\":0,\"SideLength\":100}],\"RefreshRate\":44}]";

            var objControllers = JsonSerializer.Deserialize<IReadOnlyList<Controller>>(jsonControllers);
            Assert.Pass(jsonControllers);
        }
        [Test]
        public async Task TestControlerWithoutToken()
        {
            Controller c = Controller.CreateFromIPPort(IP, PORT);
            await c.Initialize();
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
            Assert.That(c.StreamingStarted, Is.False, "Stream");
            await c.StartStreaming();
            Assert.That(c.StreamingStarted, Is.True, "Stream");

            foreach (var panel in c.Panels)
                panel.StreamingColor = new RGBW(255, 0, 255);
        }
    }
}