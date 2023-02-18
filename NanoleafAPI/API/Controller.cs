using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.Net.NetworkInformation;
using static NanoleafAPI.Panel;

namespace NanoleafAPI
{
    public class Controller : IDisposable
    {
        private readonly ILogger<Controller> _logger;
        public string IP { get; private set; }
        public string Port { get; private set; }

        public string Auth_token { get; private set; } = null;
        private bool isDisposed = false;
        private Ping ping = new Ping();

        public string Name { get; private set; }
        public string Model { get; private set; }
        public string Manufacturer { get; private set; }
        public string SerialNumber { get; private set; }
        public string HardwareVersion { get; private set; }
        public string FirmwareVersion { get; private set; }

        public EDeviceType DeviceType { get; private set; }


        public uint NumberOfPanels { get; private set; }
        private ushort globalOrientation;
        public ushort GlobalOrientation
        {
            get { return globalOrientation; }
            private set
            {
                _ = Communication.SetPanelLayoutGlobalOrientation(IP, Port, Auth_token, value);
            }
        }
        public ushort GlobalOrientationStored { get; private set; }
        public ushort GlobalOrientationMin
        {
            get;
            private set;
        }
        public ushort GlobalOrientationMax
        {
            get;
            private set;
        }

        public string[] EffectList { get; private set; }
        public string SelectedEffect { get; private set; }
        public string SelectedEffectStored { get; private set; }

        public bool PowerOn { get; private set; }
        public bool PowerOff { get; private set; }
        public bool PowerOnStored { get; private set; }

        private bool reachable;
        public bool Reachable
        {
            get
            {
                return this.reachable;
            }
            private set
            {
                if (this.reachable == value)
                    return;
                this.reachable = value;
                _logger?.LogInformation($"{this} is reachable.");
                this.establishConnection();
            }
        }
        public void SetPowerOn()
        {
            _ = Communication.SetStateOnOff(IP, Port, Auth_token, true);
        }
        public void SetPowerOff()
        {
            _ = Communication.SetStateOnOff(IP, Port, Auth_token, false);
        }

        private ushort brightness;

        public ushort Brightness
        {
            get { return brightness; }
            set
            {
                _ = Communication.SetStateBrightness(IP, Port, Auth_token, value);
            }
        }
        public ushort BrightnessStored { get; private set; }
        public ushort BrightnessMin
        {
            get;
            private set;
        }
        public ushort BrightnessMax
        {
            get;
            private set;
        }
        private ushort hue;
        public ushort Hue
        {
            get { return hue; }
            set
            {
                _ = Communication.SetStateHue(IP, Port, Auth_token, value);
            }
        }
        public ushort HueStored { get; private set; }
        public ushort HueMin
        {
            get;
            private set;
        }
        public ushort HueMax
        {
            get;
            private set;
        }
        private ushort saturation;
        public ushort Saturation
        {
            get { return saturation; }
            set
            {
                _ = Communication.SetStateSaturation(IP, Port, Auth_token, value);
            }
        }
        public ushort SaturationStored { get; private set; }
        public ushort SaturationMin
        {
            get;
            private set;
        }
        public ushort SaturationMax
        {
            get;
            private set;
        }
        private ushort colorTemprature;
        public ushort ColorTemprature
        {
            get { return colorTemprature; }
            set
            {
                _ = Communication.SetStateColorTemperature(IP, Port, Auth_token, value);
            }
        }
        public ushort ColorTempratureStored { get; private set; }
        public ushort ColorTempratureMin
        {
            get;
            private set;
        }
        public ushort ColorTempratureMax
        {
            get;
            private set;
        }
        public string ColorMode { get; private set; }
        public string ColorModeStored { get; private set; }
        private List<Panel> panels = new List<Panel>();
        private List<Panel> changedPanels = new List<Panel>();
        public ReadOnlyCollection<Panel> Panels
        {
            get { return panels.AsReadOnly(); }
        }

        public event EventHandler PanelAdded;
        public event EventHandler PanelRemoved;
        public event EventHandler PanelLayoutChanged;
        public event EventHandler AuthTokenReceived;
        public event EventHandler UpdatedInfos;

        private ExternalControlConnectionInfo externalControlInfo;

        public Controller(JToken json)
        {
            _logger = Tools.LoggerFactory.CreateLogger<Controller>();
            IP = (string)json[nameof(IP)];
            Port = (string)json[nameof(Port)];
            Auth_token = (string)json[nameof(Auth_token)];
            Name = (string)json[nameof(Name)];
            Model = (string)json[nameof(Model)];
            Manufacturer = (string)json[nameof(Manufacturer)];
            SerialNumber = (string)json[nameof(SerialNumber)];
            HardwareVersion = (string)json[nameof(HardwareVersion)];
            FirmwareVersion = (string)json[nameof(FirmwareVersion)];
            DeviceType = Tools.ModelStringToEnum(Model);

            NumberOfPanels = (uint)json[nameof(NumberOfPanels)];
            globalOrientation = (ushort)json[nameof(GlobalOrientation)];
            GlobalOrientationMin = (ushort)json[nameof(GlobalOrientationMin)];
            GlobalOrientationMax = (ushort)json[nameof(GlobalOrientationMax)];

            //EffectList = (string[])json[nameof(EffectList)].Select(c=>c.)
            SelectedEffect = (string)json[nameof(SelectedEffect)];
            PowerOn = (bool)json[nameof(PowerOn)];
            PowerOff = (bool)json[nameof(PowerOff)];

            brightness = (ushort)json[nameof(Brightness)];
            BrightnessMin = (ushort)json[nameof(BrightnessMin)];
            BrightnessMax = (ushort)json[nameof(BrightnessMax)];
            hue = (ushort)json[nameof(Hue)];
            HueMin = (ushort)json[nameof(HueMin)];
            HueMax = (ushort)json[nameof(HueMax)];
            saturation = (ushort)json[nameof(Saturation)];
            SaturationMin = (ushort)json[nameof(SaturationMin)];
            SaturationMax = (ushort)json[nameof(SaturationMax)];
            colorTemprature = (ushort)json[nameof(ColorTemprature)];
            ColorTempratureMin = (ushort)json[nameof(ColorTempratureMin)];
            ColorTempratureMax = (ushort)json[nameof(ColorTempratureMax)];
            ColorMode = (string)json[nameof(ColorMode)];

            //Backup current state to restore it on shutdown
            GlobalOrientationStored = globalOrientation;
            SelectedEffectStored = SelectedEffect;
            PowerOnStored = PowerOn;
            BrightnessStored = brightness;
            HueStored = hue;
            SaturationStored = saturation;
            ColorTempratureStored = colorTemprature;
            ColorModeStored = ColorMode;

            var panels = json[nameof(Panels)];
            foreach (var p in panels)
                this.panels.Add(new Panel(p));

            startServices();
        }
        public Controller(string ip, string port, string auth_token = null)
        {
            IP = ip;
            Port = port;
            Auth_token = auth_token;
            if (Auth_token == null /*&& NanoleafPlugin.AutoRequestToken*/)
            {
                RequestToken();
            }
            startServices();
        }

        ~Controller()
        {
            Dispose();
        }

        private void startServices()
        {
            Task taskRun = new Task(() =>
            {
                runController();
            });
            taskRun.Start();
            Thread threadStream = new Thread(() =>
            {
                streamController();
            });
            threadStream.IsBackground = true;
            threadStream.Priority = ThreadPriority.AboveNormal;
            threadStream.SetApartmentState(ApartmentState.MTA);
            threadStream.Start();
        }

        public void RequestToken(int tryes=20)
        {
            int count = 0;
            Task.Run(async () =>
            {
                while (Auth_token == null && !this.isDisposed)
                    try
                    {
                        Auth_token = await Communication.AddUser(IP, Port);
                    }
                    catch (Exception)
                    {
                        _logger?.LogInformation($"Device({IP}) is maybe not in Pairing-Mode. Please hold the Powerbutton until you see a Visual Feedback on the Controller (5-7)s");
                        await Task.Delay(8000);// If the device is not in Pairing-Mode it takes 5-7s to enable the pairing mode by hand. We try it again after 8s.
                        count++;
                        if (count >= tryes && Auth_token == null)
                        {
                            _logger?.LogInformation($"Device({IP}) not Response after {count} retries");
                            return;
                        }
                    }

                if (Auth_token != null)
                {
                    _logger?.LogInformation($"Received AuthToken ({Auth_token}) from Device({IP}) after {count} retries");
                    AuthTokenReceived?.Invoke(this, EventArgs.Empty);
                }
            });
        }

        private async void runController()
        {
            while (!isDisposed && Auth_token == null)
                await Task.Delay(1000);

            do
            {
                try
                {
                    var res = await ping.SendPingAsync(IP);
                    if (!isDisposed)
                    {
                        if (res.Status == IPStatus.Success)
                            this.Reachable = true;
                        else
                            this.Reachable = false;
                    }

                    await Task.Delay(5000);

                    if (this.Reachable && !isDisposed)
                        UpdateInfos(await Communication.GetAllPanelInfo(IP, Port, Auth_token));
                }
                catch (Exception e)
                {
                }
            } while (!isDisposed);
        }
        private async void establishConnection()
        {
            try
            {
                Communication.StaticOnLayoutEvent -= Communication_StaticOnLayoutEvent;
                Communication.StaticOnLayoutEvent += Communication_StaticOnLayoutEvent;

                var infos = await Communication.GetAllPanelInfo(IP, Port, Auth_token);
                BackupSettings(infos);
                UpdateInfos(infos);
                await Communication.StartEventListener(IP, Port, Auth_token);
                externalControlInfo = await Communication.SetExternalControlStreaming(IP, Port, Auth_token, DeviceType);
            }
            catch (Exception e)
            {
            }
        }

        private void UpdateInfos(AllPanelInfo allPanelInfo)
        {
            if (allPanelInfo == null)
            {
                this.Reachable = false;
                return;
            }
            this.Reachable = true;

            Name = allPanelInfo.Name;
            Model = allPanelInfo.Model;
            Manufacturer = allPanelInfo.Manufacturer;
            SerialNumber = allPanelInfo.SerialNumber;
            HardwareVersion = allPanelInfo.HardwareVersion;
            FirmwareVersion = allPanelInfo.FirmwareVersion;

            switch (Model)
            {
                case "NL22":
                    DeviceType = EDeviceType.LightPanles;
                    break;
                case "NL29":
                    DeviceType = EDeviceType.Canvas;
                    break;
                case "NL42":
                    DeviceType = EDeviceType.Shapes;
                    break;
                case "NL45":
                    DeviceType = EDeviceType.Essentials;
                    break;
                case "NL52":
                    DeviceType = EDeviceType.Elements;
                    break;
                case "NL59":
                    DeviceType = EDeviceType.Lines;
                    break;
            }

            NumberOfPanels = allPanelInfo.PanelLayout.Layout.NumberOfPanels;
            globalOrientation = allPanelInfo.PanelLayout.GlobalOrientation.Value;
            GlobalOrientationMin = (ushort)allPanelInfo.PanelLayout.GlobalOrientation.Min;
            GlobalOrientationMax = (ushort)allPanelInfo.PanelLayout.GlobalOrientation.Max;

            EffectList = allPanelInfo.Effects.List.ToArray();
            SelectedEffect = allPanelInfo.Effects.Selected;
            PowerOn = allPanelInfo.State.On.On;
            PowerOff = !PowerOn;

            brightness = allPanelInfo.State.Brightness.Value;
            BrightnessMin = (ushort)allPanelInfo.State.Brightness.Min;
            BrightnessMax = (ushort)allPanelInfo.State.Brightness.Max;
            hue = allPanelInfo.State.Hue.Value;
            HueMin = (ushort)allPanelInfo.State.Hue.Min;
            HueMax = (ushort)allPanelInfo.State.Hue.Max;
            saturation = allPanelInfo.State.Saturation.Value;
            SaturationMin = (ushort)allPanelInfo.State.Saturation.Min;
            SaturationMax = (ushort)allPanelInfo.State.Saturation.Max;
            colorTemprature = allPanelInfo.State.ColorTemprature.Value;
            ColorTempratureMin = (ushort)allPanelInfo.State.ColorTemprature.Min;
            ColorTempratureMax = (ushort)allPanelInfo.State.ColorTemprature.Max;
            ColorMode = allPanelInfo.State.ColorMode;

            UpdatedInfos?.Invoke(this, EventArgs.Empty);

            UpdatePanelLayout(allPanelInfo.PanelLayout.Layout);
        }

        private void BackupSettings(AllPanelInfo allPanelInfo)
        {
            //Backup current state to restore it on shutdown
            GlobalOrientationStored = allPanelInfo.PanelLayout.GlobalOrientation.Value;
            SelectedEffectStored = allPanelInfo.Effects.Selected;
            PowerOnStored = allPanelInfo.State.On.On;
            BrightnessStored = allPanelInfo.State.Brightness.Value;
            HueStored = allPanelInfo.State.Hue.Value;
            SaturationStored = allPanelInfo.State.Saturation.Value;
            ColorTempratureStored = allPanelInfo.State.ColorTemprature.Value;
            ColorModeStored = allPanelInfo.State.ColorMode;
        }

        private void Communication_StaticOnLayoutEvent(object sender, LayoutEventArgs e)
        {
            if (!e.IP.Equals(IP))
                return;

            if (!isDisposed)
                UpdatePanelLayout(e.LayoutEvent.Layout);
        }
        private void UpdatePanelLayout(Layout layout)
        {
            var ids = layout.PanelPositions.Select(p => p.PanelId);
            foreach (int id in ids)
            {
                if (!panels.Any(p => p.ID.Equals(id)))
                {
                    var pp = layout.PanelPositions.Single(p => p.PanelId.Equals(id));
                    panels.Add(new Panel(IP, pp));
                    PanelAdded?.Invoke(null, EventArgs.Empty);
                }
            }
            bool panelRemoved = false;
            panels.RemoveAll((p) => 
            { 
                bool remove = !ids.Any(id => id.Equals(p.ID));
                if (remove)
                    panelRemoved = true;
                return remove;
            });
            if(panelRemoved)
                PanelRemoved?.Invoke(null, EventArgs.Empty);

            PanelLayoutChanged?.Invoke(null, EventArgs.Empty);
        }

        private void streamController()
        {
            while (!isDisposed && Auth_token == null)
                Thread.Sleep(1000);

            long lastTimestamp = 0;
            long nowTimestamp = 0;
            int frameCounter = 0;
            while (!isDisposed)
            {
                nowTimestamp = DateTime.Now.Ticks;
                int refreshRate = 60;// NanoleafPlugin.RefreshRate.Limit(10, 60);
                double milliSinceLast = ((double)(nowTimestamp - lastTimestamp)) / TimeSpan.TicksPerMillisecond;
                double frameDuration = (1000 / refreshRate);
                if (milliSinceLast < frameDuration)
                {
                    if (milliSinceLast > frameDuration*2)
                        _logger?.LogWarning($"Streaming-Thread last send {milliSinceLast}ms");
                    Thread.SpinWait(10);
                }
                else
                {
                    lastTimestamp = DateTime.Now.Ticks;
                    if (frameCounter >= refreshRate)//KeyFrame every 1s
                    {
                        lock (changedPanels)
                        {
                            changedPanels.Clear();
                            frameCounter = 0;
                            if (panels.Count != 0)
                                Communication.SendUDPCommand(externalControlInfo, Communication.CreateStreamingData(panels));
                        }
                    }
                    else if (externalControlInfo != null)//DeltaFrame
                    {
                        Panel[] _panels = new Panel[0];
                        lock (changedPanels)
                        {
                            if (changedPanels.Count != 0)
                            {
                                _panels = changedPanels.ToArray();
                                changedPanels.Clear();
                            }
                        }
                        if (_panels.Length > 0)
                            Communication.SendUDPCommand(externalControlInfo, Communication.CreateStreamingData(_panels));
                    }
                    frameCounter++;
                }
            }
        }


        public bool SetPanelColor(int panelID, RGBW color)
        {
            var panel = this.panels.FirstOrDefault(p => p.ID.Equals(panelID));
            if (panel != null)
            {
                panel.StreamingColor = color;
                lock (changedPanels)
                {
                    if (!changedPanels.Contains(panel))
                        changedPanels.Add(panel);
                }
                return true;
            }
            return false;
        }

        public void SelfDestruction(bool deleteUser=false)
        {
            Dispose();
            RestoreParameters();
            if (deleteUser)
                Communication.DeleteUser(IP, Port, Auth_token).GetAwaiter().GetResult();

            _logger?.LogInformation(string.Format("Destruct {0}", this));
        }

        public async void RestoreParameters()
        {
            try
            {
                await Communication.SetPanelLayoutGlobalOrientation(IP, Port, Auth_token, GlobalOrientationStored);
                await Communication.SetStateBrightness(IP, Port, Auth_token, BrightnessStored);

                //Check, if the last state is restorable
                if (SelectedEffectStored != null && !SelectedEffectStored.Equals("*Dynamic*"))
                {
                    await Communication.SetSelectedEffect(IP, Port, Auth_token, SelectedEffectStored);
                    await Communication.SetColorMode(IP, Port, Auth_token, ColorModeStored);

                    if (ColorModeStored != null && ColorModeStored.Equals("ct"))
                    {
                        await Communication.SetStateColorTemperature(IP, Port, Auth_token, ColorTempratureStored);
                    }
                    if (ColorModeStored != null && ColorModeStored.Equals("hs"))
                    {
                        await Communication.SetStateHue(IP, Port, Auth_token, HueStored);
                        await Communication.SetStateSaturation(IP, Port, Auth_token, SaturationStored);
                    }
                }
                else
                {
                    // If the selected effect was "Dynamic" then a preview scene was active which can not be restored. Thus, set a default scene
                    await Communication.SetColorMode(IP, Port, Auth_token, "ct");
                    await Communication.SetStateColorTemperature(IP, Port, Auth_token, 5000);
                }

                // Setting the power state must be the last thing to do due to the fact that all other commands activate the Nanoleafs
                await Communication.SetStateOnOff(IP, Port, Auth_token, PowerOnStored);
            }
            finally 
            {
                _logger?.LogInformation(string.Format("Reset parameters for {0}", this));
            }
        }

        public void Dispose()
        {
            isDisposed = true;
        }

        public override string ToString()
        {
            return $"Name: {Name} IP: {IP} Port: {Port} DeviceType: {DeviceType} SN: {SerialNumber}";
        }
    }
}
