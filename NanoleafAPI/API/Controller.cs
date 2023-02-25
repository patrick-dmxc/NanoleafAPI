﻿using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Net.NetworkInformation;
using static NanoleafAPI.Panel;

namespace NanoleafAPI
{
    public class Controller : IDisposable
    {
        private readonly ILogger<Controller>? _logger;
        public string IP { get; private set; }
        public string Port { get; private set; }

        public string? Auth_token { get; private set; } = null;
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
                if (!Tools.IsTokenValid(Auth_token))
                    return;

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
                _ = this.establishConnection();
            }
        }

        public bool StreamingStarted
        {
            get;
            private set;
        }

        public void SetPowerOn()
        {
            if (!Tools.IsTokenValid(Auth_token))
                return;

            _ = Communication.SetStateOnOff(IP, Port, Auth_token, true);
        }
        public void SetPowerOff()
        {
            if (!Tools.IsTokenValid(Auth_token))
                return;

            _ = Communication.SetStateOnOff(IP, Port, Auth_token, false);
        }

        private ushort brightness;

        public ushort Brightness
        {
            get { return brightness; }
            set
            {
                if (!Tools.IsTokenValid(Auth_token))
                    return;

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
                if (!Tools.IsTokenValid(Auth_token))
                    return;

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
                if (!Tools.IsTokenValid(Auth_token))
                    return;

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
                if (!Tools.IsTokenValid(Auth_token))
                    return;

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
        private ConcurrentDictionary<int, Panel> changedPanels = new ConcurrentDictionary<int, Panel>();
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
        private Thread? streamThread;

        private double refreshRate = 44;
        public double RefreshRate
        {
            get => refreshRate;
            set
            {
                refreshRate = Math.Min(60, Math.Max(10, value));
            }
        }

#pragma warning disable CS8618
        public Controller(JToken json)
        {
            _logger = Tools.LoggerFactory.CreateLogger<Controller>();
#pragma warning disable CS8600
#pragma warning disable CS8601
#pragma warning disable CS8604

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
            if (string.IsNullOrWhiteSpace(SelectedEffect))
                throw new NullReferenceException($"{nameof(SelectedEffect)} is null!");
            SelectedEffectStored = SelectedEffect;
            PowerOnStored = PowerOn;
            BrightnessStored = brightness;
            HueStored = hue;
            SaturationStored = saturation;
            ColorTempratureStored = colorTemprature;
            if (string.IsNullOrWhiteSpace(ColorMode))
                throw new NullReferenceException($"{nameof(ColorMode)} is null!");
            ColorModeStored = ColorMode;

            var panels = json[nameof(Panels)];
            if (panels != null)
                foreach (var p in panels)
                    this.panels.Add(new Panel(p));


#pragma warning restore CS8600
#pragma warning restore CS8601
#pragma warning restore CS8604
            _ = startServices();
        }
        public Controller(string ip, string port, string? auth_token = null)
        {
            _logger = Tools.LoggerFactory.CreateLogger<Controller>();
            IP = ip;
            Port = port;
            Auth_token = auth_token;
            _ = startServices();
        }
#pragma warning restore CS8618

        ~Controller()
        {
            Dispose();
        }

        private async Task startServices()
        {
            if (!Tools.IsTokenValid(Auth_token))
            {
                await RequestToken();
            }
            _ = this.runController();
            _ = this.streamController();
        }

        public async Task RequestToken(int tryes = 20)
        {
            _logger?.LogInformation($"Request AuthToken for Device({IP})");
            int count = 0;
            while (!Tools.IsTokenValid(Auth_token) && !this.isDisposed)
            {
                try
                {
                    Auth_token = await Communication.AddUser(IP, Port);
                }
                catch (Exception e)
                {
                    count++;
                    if (count >= tryes && Auth_token == null)
                    {
                        _logger?.LogInformation($"Device({IP}) not Response after {count} retries");
                        return;
                    }
                    _logger?.LogDebug(string.Empty, e);
                }
                _logger?.LogInformation($"Device({IP}) is maybe not in Pairing-Mode. Please hold the Powerbutton until you see a Visual Feedback on the Controller (5-7)s");
                await Task.Delay(8000);// If the device is not in Pairing-Mode it takes 5-7s to enable the pairing mode by hand. We try it again after 8s.
            }

            if (Auth_token != null)
            {
                _logger?.LogInformation($"Received AuthToken ({Auth_token}) from Device({IP}) after {count} retries");
                AuthTokenReceived?.InvokeFailSafe(this, EventArgs.Empty);
            }
            else
                _logger?.LogInformation($"Didn't received AuthToken ({Auth_token}) from Device({IP}) after {count} retries");
        }

        private async Task runController()
        {
            _logger?.LogDebug("Run Controller");
            while (!isDisposed && !Tools.IsTokenValid(Auth_token))
                await Task.Delay(1000);

            do
            {
                try
                {
                    this.Reachable = await Communication.Ping(IP,Port);

                    await Task.Delay(5000);

                    if (this.Reachable && !isDisposed && Tools.IsTokenValid(Auth_token))
                    {
                        var allPanelInfo = await Communication.GetAllPanelInfo(IP, Port, Auth_token);
                        if (allPanelInfo != null)
                            await UpdateInfos(allPanelInfo);
                        else
                            _logger?.LogDebug($"{nameof(Communication.GetAllPanelInfo)} returned null!");
                    }
                }
                catch (Exception e)
                {
                    _logger?.LogDebug(string.Empty, e);
                }
            } while (!isDisposed);
        }
        private async Task establishConnection()
        {
            try
            {
                if (!Tools.IsTokenValid(Auth_token))
                    return;

                Communication.StaticOnLayoutEvent -= Communication_StaticOnLayoutEvent;
                Communication.StaticOnLayoutEvent += Communication_StaticOnLayoutEvent;

                var infos = await Communication.GetAllPanelInfo(IP, Port, Auth_token);
                if (infos == null)
                    return;

                await BackupSettings(infos);
                await UpdateInfos(infos);
                Communication.StartEventListener(IP, Port, Auth_token);
                var eci= await Communication.SetExternalControlStreaming(IP, Port, Auth_token, DeviceType);
                if (eci != null)
                    externalControlInfo = eci;
                else
                    _logger?.LogDebug($"{nameof(Communication.SetExternalControlStreaming)} returned null");
            }
            catch (Exception e)
            {
                _logger?.LogError(e, string.Empty);
            }
        }

        private async Task UpdateInfos(AllPanelInfo allPanelInfo)
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

            DeviceType = Tools.ModelStringToEnum(Model);

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

            UpdatedInfos?.InvokeFailSafe(this, EventArgs.Empty);

            UpdatePanelLayout(allPanelInfo.PanelLayout.Layout);
        }

        private async Task BackupSettings(AllPanelInfo allPanelInfo)
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

        private void Communication_StaticOnLayoutEvent(object? sender, LayoutEventArgs e)
        {
            if (!e.IP.Equals(IP))
                return;

            if (!isDisposed && e.LayoutEvent.Layout != null)
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
                    PanelAdded?.InvokeFailSafe(null, EventArgs.Empty);
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
            if (panelRemoved)
                PanelRemoved?.InvokeFailSafe(null, EventArgs.Empty);

            PanelLayoutChanged?.InvokeFailSafe(null, EventArgs.Empty);
        }

        private async Task streamController()
        {
            streamThread = new Thread(async () =>
            {
                _logger?.LogDebug("Start Stream");
                StreamingStarted = true;
                while (!isDisposed && Auth_token == null)
                    Thread.Sleep(1000);

                DateTime lastTimestamp = default;
                DateTime nowTimestamp = default;
                int frameCounter = 0;
                while (!isDisposed)
                {
                    nowTimestamp = DateTime.UtcNow;
                    if (externalControlInfo == null)
                    {
                        await Task.Delay(10);
                        continue;
                    }
                    try
                    {
                        double milliSinceLast = ((double)(nowTimestamp.TimeOfDay.TotalMilliseconds - lastTimestamp.TimeOfDay.TotalMilliseconds));
                        double frameDuration = (1000 / refreshRate);
                        double frameTime = nowTimestamp.TimeOfDay.TotalMilliseconds - frameDuration;
                        if (milliSinceLast < frameDuration)
                        {
                            if (milliSinceLast > frameDuration * 2)
                                _logger?.LogWarning($"Streaming-Thread last send {milliSinceLast}ms");
                            Thread.SpinWait(10);
                        }
                        else
                        {
                            if (frameCounter == 0) //Key-Frame
                            {
#if DEBUG
                                _logger?.LogDebug("Key-Frame");
#endif
                                if (panels.Count != 0)
                                {
                                    var data = Communication.CreateStreamingData(panels);
                                    if (data != null)
                                        await Communication.SendUDPCommand(externalControlInfo, data);
                                    else
                                        _logger?.LogDebug($"{nameof(Communication.CreateStreamingData)} returned null!");
                                }
                            }
                            else //Delta-Frame
                            {
#if DEBUG
                                _logger?.LogDebug("Delta-Frame");
#endif
                                var _panels = panels.Where(p => frameTime < p.LastUpdate);
                                if (_panels.Count() > 0)
                                {
                                    var data = Communication.CreateStreamingData(_panels);
                                    if (data != null)
                                        await Communication.SendUDPCommand(externalControlInfo, data);
                                    else
                                        _logger?.LogDebug($"{nameof(Communication.CreateStreamingData)} returned null!");
                                }
                            }
                            frameCounter++;
                            lastTimestamp = DateTime.UtcNow;
                            if (frameCounter >= refreshRate)
                                frameCounter = 0;
                        }
                    }
                    catch (Exception e)
                    {
                        _logger?.LogError(e, string.Empty);
                    }
                    Thread.SpinWait(10);
                }
                StreamingStarted = false;
            });
            streamThread.Name = "NanoleafAPI-StreamThread";
            streamThread.Priority = ThreadPriority.AboveNormal;
            streamThread.IsBackground = true;
            streamThread.Start();
        }


        public async Task<bool> SetPanelColor(int panelID, RGBW color)
        {
            try
            {
                var panel = this.panels.FirstOrDefault(p => p.ID.Equals(panelID));
                if (panel != null)
                {
                    panel.StreamingColor = color;
                    return true;
                }
            }
            catch (Exception e)
            {
                _logger?.LogError(e, string.Empty);
            }
            return false;
        }

        public void SelfDestruction(bool deleteUser = false)
        {
            Dispose();
            RestoreParameters();
            if (deleteUser && Tools.IsTokenValid(Auth_token))
                Communication.DeleteUser(IP, Port, Auth_token).GetAwaiter().GetResult();

            _logger?.LogInformation(string.Format("Destruct {0}", this));
        }

        public async void RestoreParameters()
        {
            if (!Tools.IsTokenValid(Auth_token))
                return;

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
            streamThread = null;
        }

        public override string ToString()
        {
            return $"Name: {Name} IP: {IP} Port: {Port} DeviceType: {DeviceType} SN: {SerialNumber}";
        }
    }
}
