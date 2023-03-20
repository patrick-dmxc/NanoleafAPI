using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace NanoleafAPI
{
    public class Controller : IDisposable
    {
        private readonly ILogger<Controller>? _logger;
        public string IP { get; private set; }
        public string Port { get; private set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public string? Auth_token { get; private set; } = null;
        private bool isDisposed = false;
        public bool IsInitialized { get; private set; } = false;
        protected bool IsSendPossible
        {
            get
            {
                if (!IsInitialized)
                    return false;
                if (isDisposed)
                    return false;
                if (!Tools.IsTokenValid(Auth_token))
                    return false;
                if(!Reachable)
                    return false;

                return true;
            }
        }

        public string Name { get; private set; }
        public string Model { get; private set; }
        public string Manufacturer { get; private set; }
        public string SerialNumber { get; private set; }
        public string HardwareVersion { get; private set; }
        public string FirmwareVersion { get; private set; }

        public EDeviceType DeviceType { get; private set; }


        public uint NumberOfPanels { get; private set; }
        private float globalOrientation;
        public float GlobalOrientation
        {
            get { return globalOrientation; }
            private set
            {
                if (!IsSendPossible)
                    return;

                _ = Communication.SetPanelLayoutGlobalOrientation(IP, Port, Auth_token!, value);
            }
        }
        [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
        public float GlobalOrientationStored { get; private set; }
        public float GlobalOrientationMin
        {
            get;
            private set;
        }
        public float GlobalOrientationMax
        {
            get;
            private set;
        }

        public string[] EffectList { get; private set; }
        public string SelectedEffect { get; private set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
        public string SelectedEffectStored { get; private set; }

        public bool PowerOn { get; private set; }
        public bool PowerOff { get; private set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
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
                if ((!externalControlInfo.HasValue) && value && StreamingStarted)
                    _ = RestartStreaming();
                ReachableChanged?.InvokeFailSafe(this, EventArgs.Empty);
            }
        }
        public event EventHandler? ReachableChanged;

        public bool StreamingStarted
        {
            get;
            private set;
        }

        public void SetPowerOn()
        {
            if (!IsSendPossible)
                return;

            _ = Communication.SetStateOnOff(IP, Port, Auth_token!, true);
        }
        public void SetPowerOff()
        {
            if (!IsSendPossible)
                return;

            _ = Communication.SetStateOnOff(IP, Port, Auth_token!, false);
        }

        private float brightness;

        public float Brightness
        {
            get { return brightness; }
            set
            {
                return;


                _ = Communication.SetStateBrightness(IP, Port, Auth_token!, value);
            }
        }
        [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
        public float BrightnessStored { get; private set; }
        public float BrightnessMin
        {
            get;
            private set;
        }
        public float BrightnessMax
        {
            get;
            private set;
        }
        private float hue;
        public float Hue
        {
            get { return hue; }
            set
            {
                if (!IsSendPossible)
                    return;

                _ = Communication.SetStateHue(IP, Port, Auth_token!, value);
            }
        }
        [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
        public float HueStored { get; private set; }
        public float HueMin
        {
            get;
            private set;
        }
        public float HueMax
        {
            get;
            private set;
        }
        private float saturation;
        public float Saturation
        {
            get { return saturation; }
            set
            {
                if (!IsSendPossible)
                    return;

                _ = Communication.SetStateSaturation(IP, Port, Auth_token!, value);
            }
        }
        [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
        public float SaturationStored { get; private set; }
        public float SaturationMin
        {
            get;
            private set;
        }
        public float SaturationMax
        {
            get;
            private set;
        }
        private float colorTemprature;
        public float ColorTemprature
        {
            get { return colorTemprature; }
            set
            {
                if (!IsSendPossible)
                    return;

                _ = Communication.SetStateColorTemperature(IP, Port, Auth_token!, value);
            }
        }
        [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
        public float ColorTempratureStored { get; private set; }
        public float ColorTempratureMin
        {
            get;
            private set;
        }
        public float ColorTempratureMax
        {
            get;
            private set;
        }
        public string ColorMode { get; private set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
        public string ColorModeStored { get; private set; }
        private List<Panel> panels = new List<Panel>();
        private ConcurrentDictionary<int, Panel> changedPanels = new ConcurrentDictionary<int, Panel>();
        public IReadOnlyList<Panel> Panels
        {
            get { return panels.AsReadOnly(); }
        }

        public event EventHandler PanelAdded;
        public event EventHandler PanelRemoved;
        public event EventHandler PanelLayoutChanged;
        public event EventHandler AuthTokenReceived;
        public event EventHandler UpdatedInfos;

        private ExternalControlConnectionInfo? externalControlInfo;
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
        [JsonConstructor]
        public Controller(string ip, string port, string? auth_token = null, bool initialize = true)
        {
            _logger = Tools.LoggerFactory.CreateLogger<Controller>();
            IP = ip;
            Port = port;
            Auth_token = auth_token;
            if (initialize)
                _ = startServices();
        }
#pragma warning restore CS8618

        ~Controller()
        {
            Dispose();
        }
        public async Task Initialize()
        {
            if (this.IsInitialized)
                return;

            await this.startServices();
        }
        private async Task startServices()
        {
            if (!Tools.IsTokenValid(Auth_token))
            {
                await RequestToken();
            }
            _ = this.runController();
            this.streamController();
            IsInitialized = true;
        }

        public async Task RequestToken(int tryes = 20)
        {
            _logger?.LogInformation($"Request AuthToken for Device({IP})");
            int count = 0;
            while (!Tools.IsTokenValid(Auth_token) && !this.isDisposed)
            {
                try
                {
                    var user = await Communication.AddUser(IP, Port);
                    if (user.Success)
                    {
                        Auth_token = user.ResponseValue.AuthToken;
                        break;
                    }
                    else
                    {
                        _logger?.LogInformation($"Device({IP}) can't obtain AuthToken");
                        if (string.Equals("6517", this.Port))
                        {
                            _logger?.LogInformation($"Port is: {this.Port}, falback to 16021");
                            this.Port = "16021";
                        }
                    }
                }
                catch (Exception e)
                {
                    count++;
                    if (count >= tryes && Auth_token == null)
                    {
                        _logger?.LogInformation($"Device({IP}) not Response after {count} retries");
                        break;
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
            _logger?.LogDebug($"Run Controller ({IP})");
            while (!isDisposed && !Tools.IsTokenValid(Auth_token))
                await Task.Delay(1000);

            do
            {
                try
                {
                    var responsePing= await Communication.Ping(IP, Port);
                    this.Reachable = responsePing.Success;

                    await Task.Delay(5000);

                    if (this.Reachable && !isDisposed && Tools.IsTokenValid(Auth_token))
                    {
                        var response = await Communication.GetAllPanelInfo(IP, Port, Auth_token);
                        if (response.Success)
                            updateInfos(response.ResponseValue);
                        else
                        {
                            _logger?.LogDebug($"{nameof(Communication.GetAllPanelInfo)} returned null!");
                            _logger?.LogDebug($"Checking Connection to {IP}");

                            responsePing = await Communication.Ping(IP, Port);
                            this.Reachable = responsePing.Success;
                            if (this.Reachable)
                            {
                                _logger?.LogDebug($"Reset Auth_Token for {IP}");
                                Auth_token = null;
                                await RequestToken();
                                _logger?.LogDebug($"New Auth_Token for {IP} is {Auth_token}");
                            }
                        }
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

                var reponse = await Communication.GetAllPanelInfo(IP, Port, Auth_token);
                if (!reponse.Success)
                    return;

                backupSettings(reponse.ResponseValue);
                updateInfos(reponse.ResponseValue);
                Communication.StartEventListener(IP, Port, Auth_token);
            }
            catch (Exception e)
            {
                _logger?.LogError(e, string.Empty);
            }
        }
        public async Task StartStreaming()
        {
            _logger?.LogInformation($"Starting Stream to {IP}");
            if (IsSendPossible)
            {
                var response = await Communication.GetAllPanelInfo(IP, Port, Auth_token);
                if (response.Success)
                {
                    backupSettings(response.ResponseValue);
                    updateInfos(response.ResponseValue);
                }

                var eci = await Communication.SetExternalControlStreaming(IP, Port, Auth_token, DeviceType);
                if (eci.Success)
                {
                    externalControlInfo = eci.ResponseValue;

                    StreamingStarted = true;
                    return;
                }
                else
                    _logger?.LogDebug($"{nameof(Communication.SetExternalControlStreaming)} returned null");

                _logger?.LogInformation($"Started Stream to {IP}");
            }
            else
                _logger?.LogInformation($"{nameof(Auth_token)} for {IP} is invalid");

            StreamingStarted = false;
        }
        public async Task RestartStreaming()
        {
            if (IsSendPossible)
            {
                var eci = await Communication.SetExternalControlStreaming(IP, Port, Auth_token, DeviceType);
                if (eci.Success)
                {
                    externalControlInfo = eci.ResponseValue;
                    StreamingStarted = true;
                    return;
                }
                else
                    _logger?.LogDebug($"{nameof(Communication.SetExternalControlStreaming)} returned null");
            }
            StreamingStarted = false;
        }
        public async Task StopStreaming()
        {
            _logger?.LogInformation($"Stopping Stream to {IP}");
            externalControlInfo = null;
            if (IsSendPossible)
            {
                await restoreParameters();
                _logger?.LogInformation($"Stopped Stream to {IP}");
            }
            else
                _logger?.LogInformation($"{nameof(Auth_token)} for {IP} is invalid");
            StreamingStarted = false;
        }

        private void updateInfos(AllPanelInfo? allPanelInfo)
        {
            if (!allPanelInfo.HasValue)
            {
                this.Reachable = false;
                return;
            }
            this.Reachable = true;

            AllPanelInfo apl = allPanelInfo.Value;

            Name = apl.Name;
            Model = apl.Model;
            Manufacturer = apl.Manufacturer;
            SerialNumber = apl.SerialNumber;
            HardwareVersion = apl.HardwareVersion;
            FirmwareVersion = apl.FirmwareVersion;

            DeviceType = Tools.ModelStringToEnum(Model);

            NumberOfPanels = apl.PanelLayout.Layout.NumberOfPanels;
            globalOrientation = apl.PanelLayout.GlobalOrientation.Value;
            GlobalOrientationMin = (float)apl.PanelLayout.GlobalOrientation.Min;
            GlobalOrientationMax = (float)apl.PanelLayout.GlobalOrientation.Max;

            EffectList = apl.Effects.List.ToArray();
            SelectedEffect = apl.Effects.Selected;
            PowerOn = apl.State.On.On;
            PowerOff = !PowerOn;

            brightness = apl.State.Brightness.Value;
            BrightnessMin = (float)apl.State.Brightness.Min;
            BrightnessMax = (float)apl.State.Brightness.Max;
            hue = apl.State.Hue.Value;
            HueMin = (float)apl.State.Hue.Min;
            HueMax = (float)apl.State.Hue.Max;
            saturation = apl.State.Saturation.Value;
            SaturationMin = (float)apl.State.Saturation.Min;
            SaturationMax = (float)apl.State.Saturation.Max;
            colorTemprature = apl.State.ColorTemprature.Value;
            ColorTempratureMin = (float)apl.State.ColorTemprature.Min;
            ColorTempratureMax = (float)apl.State.ColorTemprature.Max;
            ColorMode = apl.State.ColorMode;

            UpdatedInfos?.InvokeFailSafe(this, EventArgs.Empty);

            UpdatePanelLayout(apl.PanelLayout.Layout);
        }

        private void backupSettings(AllPanelInfo allPanelInfo)
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

            foreach (var @event in e.LayoutEvents.Events)
                if (!isDisposed && @event.Layout.HasValue)
                    UpdatePanelLayout(@event.Layout.Value);
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

        private void streamController()
        {
            streamThread = new Thread(async () =>
            {
                _logger?.LogDebug("Start Stream");
                while (!isDisposed && Auth_token == null)
                    Thread.Sleep(1000);

                DateTime lastTimestamp = default;
                DateTime nowTimestamp = default;
                int frameCounter = 0;
                while (!isDisposed)
                {
                    nowTimestamp = DateTime.UtcNow;
                    if (!externalControlInfo.HasValue)
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
                                    if (data != null && externalControlInfo.HasValue)
                                        await Communication.SendUDPCommand(externalControlInfo.Value, data);
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
                                    if (data != null && externalControlInfo.HasValue)
                                        await Communication.SendUDPCommand(externalControlInfo.Value, data);
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
            });
            streamThread.Name = "NanoleafAPI-StreamThread";
            streamThread.Priority = ThreadPriority.AboveNormal;
            streamThread.IsBackground = true;
            streamThread.Start();
        }

        public async Task<bool> SetEffect(bool externalControl, string? effectName = null)
        {
            if (!IsSendPossible)
                return false;

            if (externalControl)
            {
                var info = Communication.SetExternalControlStreaming(IP, Port, Auth_token!, DeviceType);

                return (info != null);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(effectName))
                    return false;

                var response = await Communication.SetSelectedEffect(IP, Port, Auth_token!, effectName);
                return response.Success;
            }
        }

        public bool SetPanelColor(int panelID, RGBW color)
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

        public async Task SelfDestruction(bool deleteUser = false)
        {
            Dispose();
            await restoreParameters();
            if (deleteUser && IsSendPossible)
                Communication.DeleteUser(IP, Port, Auth_token!).GetAwaiter().GetResult();

            _logger?.LogInformation(string.Format("Destruct {0}", this));
        }

        private async Task restoreParameters()
        {
            if (!IsSendPossible)
                return;

            try
            {
                await Communication.SetPanelLayoutGlobalOrientation(IP, Port, Auth_token, GlobalOrientationStored);
                await Communication.SetStateBrightness(IP, Port, Auth_token, BrightnessStored);

                //Check, if the last state is restorable
                if (SelectedEffectStored != null && !SelectedEffectStored.Equals("*Dynamic*"))
                {
                    await Communication.SetSelectedEffect(IP, Port, Auth_token, SelectedEffectStored);

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
                    await Communication.SetStateColorTemperature(IP, Port, Auth_token, 5000);
                }

                await Task.Delay(2000);
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
            IsInitialized = false;
            isDisposed = true;
            streamThread = null;
        }

        public override string ToString()
        {
            return $"Name: {Name} IP: {IP} Port: {Port} DeviceType: {DeviceType} SN: {SerialNumber}";
        }
    }
}
