using ISSDP.UPnP.PCL.Interfaces.Service;
using Microsoft.Extensions.Logging;
using SSDP.UPnP.PCL.Service;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Zeroconf;
using static NanoleafAPI.TouchEvent;

namespace NanoleafAPI
{
    public class Communication
    {
        private static ILogger? __logger = null;
        private static ILogger? _logger
        {
            get
            {
                if (__logger == null)
                    __logger = Tools.LoggerFactory.CreateLogger(nameof(Communication));
                return __logger;
            }
        }
        static CancellationTokenSource tokenSource = new CancellationTokenSource();
        static CancellationToken token = tokenSource.Token;

        private static List<IPAddress> ipAddresses = new List<IPAddress>();

        private static Socket? udpCommandSocket = null;
        private static ConcurrentDictionary<string, IPEndPoint> udpCommandEndpoints = new ConcurrentDictionary<string, IPEndPoint>();

        private static ReadOnlyCollection<IPAddress> IPAddresses
        {

            get
            {
                return ipAddresses.AsReadOnly();
            }
        }

        public static void RegisterIPAddress(IPAddress ipAddress)
        {
            if (ipAddresses.Any(ip => ip.Equals(ipAddress)))
            {
                _logger?.LogDebug($"The IP-Address: {ipAddress} is already registerd");
                return;
            }
            ipAddresses.Add(ipAddress);
            _logger?.LogDebug($"Registered IP-Address: {ipAddress}");
        }
        public static void UnregisterIPAddress(IPAddress ipAddress)
        {
            var old = ipAddresses.FirstOrDefault(ip => ip.Equals(ipAddress));
            if (old != null)
            {
                ipAddresses.Remove(old);
                _logger?.LogDebug($"Unregistered IP-Address: {ipAddress}");

            }
        }
        private static List<DiscoveredDevice> discoveredDevices = new List<DiscoveredDevice>();
        public static ReadOnlyCollection<DiscoveredDevice> DiscoveredDevices
        {
            get
            {
                return discoveredDevices.AsReadOnly();
            }
        }
        public static event EventHandler<DiscoveredEventArgs>? DeviceDiscovered;

        #region Discover SSDP
        private static Dictionary<IPAddress, IControlPoint> runningSSDPClients = new Dictionary<IPAddress, IControlPoint>();

        public static void StartDiscoverySSDPTask()
        {
            //if (_controlPoint != null)
            //    return;
            // Get host name
            String strHostName = Dns.GetHostName();

            // Find host by name
            IPHostEntry iphostentry = Dns.GetHostByName(strHostName);

            // Enumerate IP addresses
            foreach (IPAddress ipaddress in iphostentry.AddressList)
            {
                if (ipaddress.AddressFamily != AddressFamily.InterNetwork)
                    continue;
                IControlPoint _controlPoint = new ControlPoint(ipaddress);
                runningSSDPClients[ipaddress] = _controlPoint;
                _controlPoint.Start(token);
                var observerNotify = _controlPoint.NotifyObservable();
                var disposableNotify = observerNotify
               .Subscribe(
                   n =>
                   {
                       try
                       {
                           if (n.NT.Contains("nanoleaf"))
                           {
                               string? ip = n.Location.Host;
                               string? port = n.Location.Port.ToString();
                               string? name = n.Headers["NL-DEVICENAME"];
                               string? id = n.Headers["NL-DEVICEID"];
                               EDeviceType type = Tools.ModelStringToEnum(n.NT.Split(':')[1]);
                               if (string.IsNullOrWhiteSpace(ip) || string.IsNullOrWhiteSpace(port) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(id))
                               {
                                   _logger?.LogDebug($"Device Discovered via SSDP but can't be parsed correctly!");
                                   return;
                               }
                               else
                               {
                                   if (discoveredDevices.Any(d => d.IP.Equals(ip)))
                                       return;
                                   var device = new DiscoveredDevice(ip, port, name, id, type);
                                   discoveredDevices.Add(device);
                                   _logger?.LogDebug($"Device Discovered via SSDP: {device}");
                                   DeviceDiscovered?.InvokeFailSafe(null, new DiscoveredEventArgs(device));
                               }
                           }
                       }
                       catch (Exception ex)
                       {
                           _logger?.LogWarning("Not able to decode the SSDP Notification", ex);
                       }
                   }
                   );
            }
        }

        public static void StopDiscoverySSDPTask()
        {
            _logger?.LogDebug("Request stop for SSDP DiscoverTask");
            foreach(var _controlPoint in runningSSDPClients.Select(k => k.Value))
                _controlPoint?.Dispose();
            runningSSDPClients.Clear();
            _logger?.LogDebug("Await SSDP DiscoverTask stopped");
        }
        #endregion

        #region Discover mDNS
        private static bool discoverymDNSTaskRunning = false;
        private static Task? discovermDNSTask = null;

        public static void StartDiscoverymDNSTask()
        {
            if (discoverymDNSTaskRunning || discovermDNSTask != null)
                return;

            discoverymDNSTaskRunning = true;
            discovermDNSTask = new Task(async () =>
            {
                while (discoverymDNSTaskRunning)
                {
                    IReadOnlyList<IZeroconfHost> results = await
                    ZeroconfResolver.ResolveAsync("_nanoleafapi._tcp.local.");
                    foreach (var r in results)
                    {
                        if (discoveredDevices.Any(d => d.IP.Equals(r.IPAddress)))
                            continue;
                        try
                        {
                            KeyValuePair<string, Zeroconf.IService> s = r.Services.FirstOrDefault();
                            IService service = s.Value;
                            EDeviceType type = Tools.ModelStringToEnum(service.Properties.FirstOrDefault()?.FirstOrDefault(p => p.Key.Equals("md")).Value);
                            string ip = r.IPAddress;
                            string port = service.Port.ToString();
                            string name = r.DisplayName;
                            string? id = service.Properties.FirstOrDefault()?.FirstOrDefault(p => p.Key.Equals("id")).Value;

                            if (string.IsNullOrWhiteSpace(ip) || string.IsNullOrWhiteSpace(port) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(id))
                            {
                                _logger?.LogDebug($"Device Discovered via mDNS but can't be parsed correctly", service);
                                return;
                            }
                            else
                            {
                                var device = new DiscoveredDevice(ip, port, name, id, type);
                                discoveredDevices.Add(device);
                                _logger?.LogDebug($"Device Discovered via mDNS: {device}");
                                DeviceDiscovered?.InvokeFailSafe(null, new DiscoveredEventArgs(device));
                            }

                        }
                        catch (Exception ex)
                        {
                            _logger?.LogWarning("Not able to decode the mDNS Datagram",ex);
                        }
                    }
                    await Task.Delay(500);
                }
            }, token);
            discovermDNSTask.Start();
        }
        public static void StopDiscoverymDNSTask()
        {
            _logger?.LogDebug("Request stop for mDNS DiscoverTask");
            discoverymDNSTaskRunning = false;
            for (int i = 0; i < 10; i++)
            {
                if (discovermDNSTask?.IsCompleted ?? true)
                    return;
                _logger?.LogDebug("Await mDNS DiscoverTask stopped");
                Task.Delay(100).GetAwaiter();
            }
            discovermDNSTask = null;
        }
        #endregion

        public static async Task<Result<T>> SendRequest<T>(Request request, bool v1 = true, [CallerMemberName] string? caller = null)
        {
            using (HttpClient client = new HttpClient() { Timeout = new TimeSpan(0, 0, 0, 1, 500) })
            {
                Exception? exception = null;
                T? deserialized = default;
                bool success = false;
                try
                {
                    string address = string.Empty;
                    if (v1)
                    {
                        if (string.IsNullOrWhiteSpace(request.AuthToken))
                            address = createUrl(request.IP, request.Port, request.Endpoint);
                        else
                            address = createUrl(request.IP, request.Port, request.AuthToken, request.Endpoint);
                    }
                    else
                    {
                        address = $"http://{request.IP}:{request.Port}/{request.Endpoint}";
                    }

                    string commandString = request.Command?.ToString() ?? string.Empty;
                    var req = new HttpRequestMessage(request.Method, address)
                    {
                        Content = new StringContent(commandString)
                    };

                    if (string.IsNullOrWhiteSpace(commandString))
                        _logger?.LogDebug($"Request {caller} for Url:{Environment.NewLine}{address}");
                    else
                        _logger?.LogDebug($"Request {caller} for Url:{Environment.NewLine}{address}{Environment.NewLine}Command: {commandString}");
                    var response = await client.SendAsync(req);
                    if (request.ExpectedResponseStatusCode.Contains(response.StatusCode))
                    {
                        var content = await response.Content.ReadAsStringAsync();

                        if (request.ExpectedResponseStatusCode.Contains(HttpStatusCode.NoContent))
                            success = true;
                        else
                        {
                            try
                            {
                                deserialized = JsonSerializer.Deserialize<T?>(content);
                                success = true;
                            }
                            catch (Exception e)
                            {
                                exception = e;
                            }
                        }
                        if (success)
                        {
                            if (deserialized != null)
                                _logger?.LogDebug($"Received {caller} response:{Environment.NewLine}{response.StatusCode}{Environment.NewLine}Deserialized:{Environment.NewLine}{deserialized}");
                            else
                                _logger?.LogDebug($"Received {caller} response:{Environment.NewLine}{response.StatusCode}");
                            return new Result<T>(request, response.StatusCode, deserialized);
                        }
                        else if (exception != null)
                        {
                            _logger?.LogWarning($"Exception on {caller} while deserialize response.{Environment.NewLine}Content:{Environment.NewLine}{content}{Environment.NewLine}Exception:{Environment.NewLine}{exception}");
                            return new Result<T>(request, response.StatusCode, exception);
                        }
                    }
                    _logger?.LogWarning($"Received {caller} response can't be Deserialized!");
                    return new Result<T>(request, response.StatusCode);

                }
                catch (HttpRequestException he)
                {
                    exception = he;
                }
                catch (Exception e)
                {
                    exception = e;
                }
                if (exception != null)
                {
                    _logger?.LogWarning($"Exception on {caller} while Send Request.{Environment.NewLine}Exception:{Environment.NewLine}{exception}");
                    return new Result<T>(request, exception);
                }
            }
            return new Result<T>(request);
        }

        public static async Task<Result<object>> Ping(string ip, string port)
        {
            var res = await SendRequest<object>(new Request(ip, port, null, string.Empty, null, HttpMethod.Post, HttpStatusCode.Unauthorized, HttpStatusCode.NoContent));
            return res;
        }

        #region User
        public static async Task<Result<User>> AddUser(string ip, string port)
        {
            var res = await SendRequest<User>(new Request(ip, port, null, "new", null, HttpMethod.Post, HttpStatusCode.OK));
            return res;
        }
        public static async Task<Result<object>> DeleteUser(string ip, string port, string auth_token)
        {
            var res = await SendRequest<object>(new Request(ip, port, auth_token, string.Empty, null, HttpMethod.Delete, HttpStatusCode.NoContent));
            return res;
        }
        #endregion
        #region All Panel Info
        public static async Task<Result<AllPanelInfo>> GetAllPanelInfo(string ip, string port, string auth_token)
        {
            var res = await SendRequest<AllPanelInfo>(new Request(ip, port, auth_token, string.Empty, null, HttpMethod.Get, HttpStatusCode.OK));
            return res;
        }
        #endregion
        #region State
        #region On/Off
        public static async Task<Result<StateOnOff>> GetStateOnOff(string ip, string port, string auth_token)
        {
            var res = await SendRequest<StateOnOff>(new Request(ip, port, auth_token, "state/on", null, HttpMethod.Get, HttpStatusCode.OK));
            return res;
        }
        public static async Task<Result<object>> SetStateOnOff(string ip, string port, string auth_token, bool value)
        {
            var res = await SendRequest<object>(new Request(ip, port, auth_token, "state", new Command(new { on = new { value = value } }), HttpMethod.Put, HttpStatusCode.NoContent));
            return res;
        }
        #endregion
        #region Brightness
        public static async Task<Result<StateInfo>> GetStateBrightness(string ip, string port, string auth_token)
        {
            var res = await SendRequest<StateInfo>(new Request(ip, port, auth_token, "state/brightness", null, HttpMethod.Get, HttpStatusCode.OK));
            return res;
        }
        public static async Task<Result<object>> SetStateBrightness(string ip, string port, string auth_token, float value, float duration = 0)
        {
            var res = await SendRequest<object>(new Request(ip, port, auth_token, "state", new Command(new { brightness = new { value = value, duration = duration } }), HttpMethod.Put, HttpStatusCode.NoContent));
            return res;
        }
        public static async Task<Result<object>> SetStateBrightnessIncrement(string ip, string port, string auth_token, float value)
        {
            var res = await SendRequest<object>(new Request(ip, port, auth_token, "state", new Command(new { brightness = new { increment = value } }), HttpMethod.Put, HttpStatusCode.NoContent));
            return res;
        }
        #endregion
        #region Hue
        public static async Task<Result<StateInfo>> GetStateHue(string ip, string port, string auth_token)
        {
            var res = await SendRequest<StateInfo>(new Request(ip, port, auth_token, "state/hue", null, HttpMethod.Get, HttpStatusCode.OK));
            return res;
        }
        public static async Task<Result<object>> SetStateHue(string ip, string port, string auth_token, float value)
        {
            var res = await SendRequest<object>(new Request(ip, port, auth_token, "state", new Command(new { hue = new { value = value } }), HttpMethod.Put, HttpStatusCode.NoContent));
            return res;
        }
        public static async Task<Result<object>> SetStateHueIncrement(string ip, string port, string auth_token, float value)
        {
            var res = await SendRequest<object>(new Request(ip, port, auth_token, "state", new Command(new { hue = new { increment = value } }), HttpMethod.Put, HttpStatusCode.NoContent));
            return res;
        }
        #endregion
        #region Saturation
        public static async Task<Result<StateInfo>> GetStateSaturation(string ip, string port, string auth_token)
        {
            var res = await SendRequest<StateInfo>(new Request(ip, port, auth_token, "state/sat", null, HttpMethod.Get, HttpStatusCode.OK));
            return res;
        }
        public static async Task<Result<object>> SetStateSaturation(string ip, string port, string auth_token, float value)
        {
            var res = await SendRequest<object>(new Request(ip, port, auth_token, "state", new Command(new { sat = new { value = value } }), HttpMethod.Put, HttpStatusCode.NoContent));
            return res;
        }
        public static async Task<Result<object>> SetStateSaturationIncrement(string ip, string port, string auth_token, float value)
        {
            var res = await SendRequest<object>(new Request(ip, port, auth_token, "state", new Command(new { sat = new { increment = value } }), HttpMethod.Put, HttpStatusCode.NoContent));
            return res;
        }
        #endregion
        #region ColorTemperature
        public static async Task<Result<StateInfo>> GetStateColorTemperature(string ip, string port, string auth_token)
        {
            var res = await SendRequest<StateInfo>(new Request(ip, port, auth_token, "state/ct", null, HttpMethod.Get, HttpStatusCode.OK));
            return res;
        }
        public static async Task<Result<object>> SetStateColorTemperature(string ip, string port, string auth_token, float value)
        {
            var res = await SendRequest<object>(new Request(ip, port, auth_token, "state", new Command(new { ct = new { value = value } }), HttpMethod.Put, HttpStatusCode.NoContent));
            return res;
        }
        public static async Task<Result<object>> SetStateColorTemperatureIncrement(string ip, string port, string auth_token, float value)
        {
            var res = await SendRequest<object>(new Request(ip, port, auth_token, "state", new Command(new { ct = new { increment = value } }), HttpMethod.Put, HttpStatusCode.NoContent));
            return res;
        }
        #endregion
        #region ColorMode
        public static async Task<Result<string>> GetColorMode(string ip, string port, string auth_token)
        {
            var res = await SendRequest<string>(new Request(ip, port, auth_token, "state/colorMode", null, HttpMethod.Get, HttpStatusCode.OK));
            return res;
        }
        #endregion
        #endregion

        #region Effects
        public static async Task<Result<string>> GetSelectedEffect(string ip, string port, string auth_token)
        {
            var res = await SendRequest<string>(new Request(ip, port, auth_token, "effects/select", null, HttpMethod.Get, HttpStatusCode.OK));
            return res;
            //string? result = null;
            //string address = createUrl(ip, port, auth_token, "effects/select");
            //using (HttpClient hc = new HttpClient())
            //{
            //    try
            //    {
            //        _logger?.LogDebug($"Request {nameof(GetSelectedEffect)} for \"{ip}\"");
            //        var response = await hc.GetAsync(address);
            //        if (response?.StatusCode == HttpStatusCode.OK)
            //        {
            //            result = await response.Content.ReadAsStringAsync();
            //            if (result != null)
            //            {
            //                result = result.Replace("\"", "");
            //                _logger?.LogDebug($"Received {nameof(GetStateSaturation)}: {result}");
            //            }
            //        }
            //        else
            //            _logger?.LogDebug($"Received Response for {nameof(GetSelectedEffect)}: {response}");
            //    }
            //    catch (HttpRequestException he)
            //    {
            //        _logger?.LogDebug(he, string.Empty);
            //    }
            //    catch (Exception e)
            //    {
            //        _logger?.LogWarning(e, string.Empty);
            //    }
            //}
            //return result;
        }
        public static async Task<Result<object>> SetSelectedEffect(string ip, string port, string auth_token, string value)
        {
            var res = await SendRequest<object>(new Request(ip, port, auth_token, "effects", new Command(new { select = value }), HttpMethod.Put, HttpStatusCode.NoContent));
            return res;
            //bool? result = null;
            //string address = createUrl(ip, port, auth_token, "effects");
            //string contentString = "{" + $"\"select\": \"{value}\"" + "}";
            //HttpContent httpContent = new StringContent(contentString);
            //using (HttpClient hc = new HttpClient())
            //{
            //    try
            //    {
            //        _logger?.LogDebug($"Request {nameof(SetSelectedEffect)} for \"{ip}\"");
            //        var response = await hc.PutAsync(address, httpContent);
            //        result = response?.StatusCode == HttpStatusCode.NoContent;

            //        if (result == true)
            //            _logger?.LogDebug($"Received {nameof(SetSelectedEffect)} response: successfull");
            //    }
            //    catch (Exception e)
            //    {
            //        _logger?.LogWarning(e, string.Empty);
            //    }
            //}
            //return result;
        }
        public static async Task<Result<IReadOnlyList<string>>> GetEffectList(string ip, string port, string auth_token)
        {
            var res = await SendRequest<IReadOnlyList<string>>(new Request(ip, port, auth_token, "effects/effectsList", null, HttpMethod.Get, HttpStatusCode.OK));
            return res;
        }
        ///TODO 5.4.3. Write
        #endregion

        #region PanelLayout
        public static async Task<Result<StateInfo>> GetPanelLayoutGlobalOrientation(string ip, string port, string auth_token)
        {
            var res = await SendRequest<StateInfo>(new Request(ip, port, auth_token, "panelLayout/globalOrientation", null, HttpMethod.Get, HttpStatusCode.OK));
            return res;
        }
        public static async Task<Result<object>> SetPanelLayoutGlobalOrientation(string ip, string port, string auth_token, float value)
        {
            var res = await SendRequest<object>(new Request(ip, port, auth_token, "panelLayout", new Command(new { globalOrientation = new { value = value } }), HttpMethod.Put, HttpStatusCode.NoContent));
            return res;
        }
        public static async Task<Result<Layout>> GetPanelLayoutLayout(string ip, string port, string auth_token)
        {
            var res = await SendRequest<Layout>(new Request(ip, port, auth_token, "panelLayout/layout", null, HttpMethod.Get, HttpStatusCode.OK));
            return res;
        }
        #endregion
        #region Identify

        public static async Task<Result<object>> Identify(string ip, string port, string auth_token)
        {
            var res = await SendRequest<object>(new Request(ip, port, auth_token, "identify", null, HttpMethod.Put, HttpStatusCode.NoContent, HttpStatusCode.OK));
            return res;
        }
        public static async Task<Result<object>> IdentifyAndroid(string ip)
        {
            var res = await SendRequest<object>(new Request(ip, 6517.ToString(), null, "identify-android", null, HttpMethod.Post, HttpStatusCode.NoContent, HttpStatusCode.OK), false);
            return res;
        }
        #endregion
        #region FirmwareUpgrade
        public static async Task<Result<FirmwareUpgrade>> GetFirmwareUpgrade(string ip, string port, string auth_token)
        {
            var res = await SendRequest<FirmwareUpgrade>(new Request(ip, port, auth_token, "firmwareUpgrade", null, HttpMethod.Get, HttpStatusCode.OK));
            return res;
        }
        #endregion

        #region Commands
        public static async Task<Result<Animations>> GetRequerstAll(string ip, string port, string auth_token)
        {
            var res = await SendRequest<Animations>(new Request(ip, port, auth_token, "effects", new Command(new { write = new { command = "requestAll" } }), HttpMethod.Put, HttpStatusCode.OK));
            return res;
        }
        public static async Task<string?> GetTouchConfig(string ip, string port, string auth_token)
        {
            string? result = null;
            string address = createUrl(ip, port, auth_token, "effects");
            string contentString = "{" + "\"write\":{ \"command\":\"requestTouchConfig\"} }";
            HttpContent httpContent = new StringContent(contentString);
            using (HttpClient hc = new HttpClient())
            {
                try
                {
                    _logger?.LogDebug($"Request {nameof(GetTouchConfig)} for \"{ip}\"");
                    var response = await hc.PutAsync(address, httpContent);
                    if (response?.StatusCode == HttpStatusCode.OK)
                    {
                        string? content = await response.Content.ReadAsStringAsync();
                        result = content;//JsonConvert.DeserializeObject<Layout>(content);
                        if (result != null)
                            _logger?.LogDebug($"Received {nameof(GetTouchConfig)}: {result}");
                        else
                            _logger?.LogDebug($"Received {nameof(GetTouchConfig)} response can't be Deserialized: {content}");
                    }
                    else
                        _logger?.LogDebug($"Received Response for {nameof(GetTouchConfig)}: {response}");
                }
                catch (Exception e)
                {
                    _logger?.LogWarning(e, string.Empty);
                }
            }
            return result;
        }
        public static async Task<string?> GetTouchKillSwitch(string ip, string port, string auth_token)
        {
            string? result = null;
            string address = createUrl(ip, port, auth_token, "effects");
            string contentString = "{" + "\"write\":{ \"command\":\"getTouchKillSwitch\"} }";
            HttpContent httpContent = new StringContent(contentString);
            using (HttpClient hc = new HttpClient())
            {
                try
                {
                    _logger?.LogDebug($"Request {nameof(GetTouchKillSwitch)} for \"{ip}\"");
                    var response = await hc.PutAsync(address, httpContent);
                    if (response?.StatusCode == HttpStatusCode.OK)
                    {
                        string? content = await response.Content.ReadAsStringAsync();
                        result = content;//JsonConvert.DeserializeObject<Layout>(content);
                        if (result != null)
                            _logger?.LogDebug($"Received {nameof(GetTouchKillSwitch)}: {result}");
                        else
                            _logger?.LogDebug($"Received {nameof(GetTouchKillSwitch)} response can't be Deserialized: {content}");
                    }
                    else
                        _logger?.LogDebug($"Received Response for {nameof(GetTouchKillSwitch)}: {response}");
                }
                catch (Exception e)
                {
                    _logger?.LogWarning(e, string.Empty);
                }
            }
            return result;
        }
        public static async Task<bool?> SetTouchKillSwitch(string ip, string port, string auth_token, bool enabled)
        {
            bool? result = null;
            string address = createUrl(ip, port, auth_token, "effects");
            string contentString = "{" + "\"write\":{\"command\":\"setTouchKillSwitch\",\"touchKillSwitchOn\":" + enabled + "}}";
            HttpContent httpContent = new StringContent(contentString);
            using (HttpClient hc = new HttpClient())
            {
                try
                {
                    _logger?.LogDebug($"Request {nameof(SetCommandSceneChangeAnimation)} for \"{ip}\"");
                    var response = await hc.PutAsync(address, httpContent);
                    result = response?.StatusCode == HttpStatusCode.NoContent;

                    if (result == true)
                        _logger?.LogDebug($"Received {nameof(SetCommandSceneChangeAnimation)} response: successfull");
                }
                catch (Exception e)
                {
                    _logger?.LogWarning(e, string.Empty);
                }
            }
            return result;
        }
        public static async Task<bool?> SetCommandControllerButtons(string ip, string port, string auth_token, bool enabled)
        {
            bool? result = null;
            string address = createUrl(ip, port, auth_token, "effects");
            string contentString = enabled ? "{" + "\"write\":{ \"command\":\"enableAllControllerButtons\"} }" : "{" + "\"write\":{ \"command\":\"disableAllControllerButtons\"} }";
            HttpContent httpContent = new StringContent(contentString);
            using (HttpClient hc = new HttpClient())
            {
                try
                {
                    _logger?.LogDebug($"Request {nameof(SetCommandControllerButtons)} for \"{ip}\"");
                    var response = await hc.PutAsync(address, httpContent);
                    result = response?.StatusCode == HttpStatusCode.NoContent;

                    if (result == true)
                        _logger?.LogDebug($"Received {nameof(SetCommandControllerButtons)} response: successfull");
                }
                catch (Exception e)
                {
                    _logger?.LogWarning(e, string.Empty);
                }
            }
            return result;
        }
        public static async Task<bool?> SetCommandSceneChangeAnimation(string ip, string port, string auth_token, bool enabled)
        {
            bool? result = null;
            string address = createUrl(ip, port, auth_token, "effects");
            string contentString = enabled ? "{\"write\":{ \"command\":\"enableSceneChangeAnimation\"} }" : "{\"write\":{ \"command\":\"disableSceneChangeAnimation\"} }";
            HttpContent httpContent = new StringContent(contentString);
            using (HttpClient hc = new HttpClient())
            {
                try
                {
                    _logger?.LogDebug($"Request {nameof(SetCommandSceneChangeAnimation)} for \"{ip}\"");
                    var response = await hc.PutAsync(address, httpContent);
                    result = response?.StatusCode == HttpStatusCode.NoContent;

                    if (result == true)
                        _logger?.LogDebug($"Received {nameof(SetCommandSceneChangeAnimation)} response: successfull");
                }
                catch (Exception e)
                {
                    _logger?.LogWarning(e, string.Empty);
                }
            }
            return result;
        }
        public static async Task<bool?> SetCommandConfigureTouch(string ip, string port, string auth_token, bool enabled)
        {
            bool? result = null;
            string address = createUrl(ip, port, auth_token, "effects");
            string contentString = "{\"write\":{\"command\":\"configureTouch\",\"touchConfig\":{\"userSystemConfig\":{\"enabled\":" + enabled + "}}}}";
            HttpContent httpContent = new StringContent(contentString);
            using (HttpClient hc = new HttpClient())
            {
                try
                {
                    _logger?.LogDebug($"Request {nameof(SetCommandSceneChangeAnimation)} for \"{ip}\"");
                    var response = await hc.PutAsync(address, httpContent);
                    result = response?.StatusCode == HttpStatusCode.NoContent;

                    if (result == true)
                        _logger?.LogDebug($"Received {nameof(SetCommandSceneChangeAnimation)} response: successfull");
                }
                catch (Exception e)
                {
                    _logger?.LogWarning(e, string.Empty);
                }
            }
            return result;
        }
        #endregion

        #region External Control (Streaming)
        public static async Task<Result<ExternalControlConnectionInfo>> SetExternalControlStreaming(string ip, string port, string auth_token, EDeviceType deviceType)
        {
            var res = await SendRequest<ExternalControlConnectionInfo>(new Request(ip, port, auth_token, "effects", new Command(new { write = new { command = "display", animType = "extControl", extControlVersion = "v2" } }), HttpMethod.Put, HttpStatusCode.OK));
            if (!res.Success)
            {
                if (res.ResponseValue.StreamIPAddress == null && res.ResponseValue.StreamProtocol == null && res.StatusCode == HttpStatusCode.NoContent)
                    res = new Result<ExternalControlConnectionInfo>(res.Request, HttpStatusCode.OK, new ExternalControlConnectionInfo(ip, 60222, "udp"));
            }
            return res;
        }
        public static byte[]? CreateStreamingData(IEnumerable<Panel> panels)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    var panelCount = BitConverter.GetBytes(panels.Count()).Take(2);
                    if (BitConverter.IsLittleEndian)
                        panelCount = panelCount.Reverse();
                    ms.WriteByte(panelCount.ElementAt(0));
                    ms.WriteByte(panelCount.ElementAt(1));

                    foreach (var panel in panels)
                    {
                        var panelIdBytes = BitConverter.GetBytes(panel.ID).Take(2);
                        if (BitConverter.IsLittleEndian)
                            panelIdBytes = panelIdBytes.Reverse();

                        ms.WriteByte(panelIdBytes.ElementAt(0));
                        ms.WriteByte(panelIdBytes.ElementAt(1));
                        ms.WriteByte(Convert.ToByte(panel.StreamingColor.R));
                        ms.WriteByte(Convert.ToByte(panel.StreamingColor.G));
                        ms.WriteByte(Convert.ToByte(panel.StreamingColor.B));
                        ms.WriteByte(Convert.ToByte(panel.StreamingColor.W));
                        ms.WriteByte(0);
                        ms.WriteByte(0);
                    }

                    return ms.ToArray();
                }
            }
            catch (Exception e)
            {
                _logger?.LogWarning(e, string.Empty);
            }
            return null;
        }
        public static async Task SendUDPCommand(ExternalControlConnectionInfo _externalControlConnectionInfo, params byte[] data)
        {
            try
            {
                if (udpCommandSocket == null)
                    udpCommandSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                IPEndPoint? endpoint = null;
                if (udpCommandEndpoints.ContainsKey(_externalControlConnectionInfo.StreamIPAddress))
                    endpoint = udpCommandEndpoints[_externalControlConnectionInfo.StreamIPAddress];
                else
                    endpoint = udpCommandEndpoints[_externalControlConnectionInfo.StreamIPAddress] = new IPEndPoint(IPAddress.Parse(_externalControlConnectionInfo.StreamIPAddress), _externalControlConnectionInfo.StreamPort);

                if (endpoint != null)
                    await udpCommandSocket.SendToAsync(data, SocketFlags.None, endpoint);
                else
                    _logger?.LogError("Endpoint is null");
            }
            catch (Exception e)
            {
                _logger?.LogError(e, string.Empty);
            }
        }
        #endregion
        private static string createUrl(string ip, string port, string auth_token,string path)
        {
            validateCredentials(ip, port, auth_token);

            if (path== null) //If the Path is Empty, its correct!!!
                throw new ArgumentException($"Property {nameof(path)} isn't Valid: {path}");

            return $"http://{ip}:{port}/api/v1/{auth_token}/{path}";
        }
        private static string createUrl(string ip, string port, string path)
        {
            validateCredentials(ip, port);

            if (path == null) //If the Path is Empty, its correct!!!
                throw new ArgumentException($"Property {nameof(path)} isn't Valid: {path}");

            return $"http://{ip}:{port}/api/v1/{path}";
        }
        private static void validateCredentials(string ip, string port, string auth_token)
        {
            validateCredentials(ip, port);

            if (!Tools.IsTokenValid(auth_token))
                throw new ArgumentException($"Property {nameof(auth_token)} isn't Valid: {auth_token}");
        }
        private static void validateCredentials(string ip, string port)
        {
            if (!Tools.IsIPValid(ip))
                throw new ArgumentException($"Property {nameof(ip)} isn't Valid: {ip}");
            if (!Tools.IsPortValid(port))
                throw new ArgumentException($"Property {nameof(port)} isn't Valid: {port}");
        }

        #region Events

        public static event EventHandler<TouchEventArgs>? StaticOnTouchEvent;
        public static event EventHandler<StateEventArgs>? StaticOnStateEvent;
        public static event EventHandler<LayoutEventArgs>? StaticOnLayoutEvent;
        public static event EventHandler<GestureEventArgs>? StaticOnGestureEvent;
        public static event EventHandler<EffectEventArgs>? StaticOnEffectEvent;

        private static int _touchEventsPort = -1;
        private static Thread? eventListenerThread = null;
        private static Thread? eventListenerThreadTouch = null;
        private static Dictionary<string, TouchEvent> lastTouchEvent = new Dictionary<string, TouchEvent>();
        private static Thread? eventCleanLoop = null;
        private static bool eventListenerThreadRunningTouch = false;
        private static bool eventCleanLoopThreadRunning = false;

        public static void StartEventListener()
        {
            _logger?.LogDebug("Start Event listener");
            if (eventCleanLoop == null)
            {
                eventCleanLoop = new Thread(() =>
                {
                    eventCleanLoopThreadRunning = true;
                    while (eventCleanLoopThreadRunning)
                    {
                        Dictionary<string, TouchEvent> outgoingEvents = new Dictionary<string, TouchEvent>();
                        Thread.Sleep(100);
                        lock (lastTouchEvent)
                        {
                            foreach (var last in lastTouchEvent)
                            {
                                var hovering = last.Value.TouchPanelEvents.Where(p => p.Type == ETouch.Hover).ToArray();
                                if (hovering.Length > 0)
                                {
                                    long timestamp = DateTime.UtcNow.Ticks;
                                    if (timestamp - last.Value.Timestamp >= 5000000)
                                    {
                                        outgoingEvents[last.Key] = new TouchEvent(last.Value.TouchedPanelsNumber - hovering.Length, hovering.Select(h => new TouchPanelEvent(h.PanelId, ETouch.Up)).ToArray());
                                    }
                                }
                            }
                        }
                        foreach (var _event in outgoingEvents)
                        {
                            lastTouchEvent[_event.Key] = _event.Value;
                            _logger?.LogDebug($"Event occured: {_event}");
                            StaticOnTouchEvent?.InvokeFailSafe(null, new TouchEventArgs(_event.Key, _event.Value));
                        }
                    }
                });
                eventCleanLoop.Name = "Nanoleaf Event-Cleaner";
                eventCleanLoop.Priority = ThreadPriority.Lowest;
                eventCleanLoop.IsBackground = true;
                eventCleanLoop.Start();
            }

            if (eventListenerThreadTouch != null)
                return;

            eventListenerThreadTouch = new Thread(new ParameterizedThreadStart(async (o) =>
            {
                eventListenerThreadRunningTouch = true;
                try
                {
                    using (var client = new UdpClient(NextFreePort()))
                    {
                        var endpoint = (IPEndPoint?)client.Client.LocalEndPoint;
                        if(endpoint == null)
                        {
                            _logger?.LogError("Endpoint in eventListenerThreadTouch is null");
                            return;
                        }
                        _touchEventsPort = endpoint.Port;
                        do
                        {
                            if (String.IsNullOrWhiteSpace(Thread.CurrentThread.Name))
                                Thread.CurrentThread.Name = $"Nanoleaf EventListener";
                            try
                            {

                                UdpReceiveResult result = await client.ReceiveAsync();
                                string ip = result.RemoteEndPoint.Address.ToString();
                                byte[] datagram = result.Buffer;
                                var touchEvent = TouchEvent.FromArray(datagram);
                                lock (lastTouchEvent)
                                {
                                    lastTouchEvent[ip] = touchEvent;
                                    _logger?.LogDebug($"Event occured: {touchEvent}");
                                    StaticOnTouchEvent?.InvokeFailSafe(null, new TouchEventArgs(ip, touchEvent));
                                }
                            }
                            catch (Exception)
                            {

                            }
                        } while (eventListenerThreadRunningTouch);
                    }
                }
                catch (Exception)
                {

                }
            }));
            eventListenerThreadTouch.Name = $"Nanoleaf TouchEventListener";
            eventListenerThreadTouch.Priority = ThreadPriority.BelowNormal;
            eventListenerThreadTouch.IsBackground = true;
            eventListenerThreadTouch.Start();

            bool IsFree(int port)
            {
                IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
                IPEndPoint[] listeners = properties.GetActiveTcpListeners();
                int[] openPorts = listeners.Select(item => item.Port).ToArray<int>();
                return openPorts.All(openPort => openPort != port);
            }
            int NextFreePort(int port = 0)
            {
                port = (port > 0) ? port : new Random().Next(1, 65535);
                while (!IsFree(port))
                {
                    port += 1;
                }
                return port;
            }
        }
        public static void StopEventListener()
        {
            eventCleanLoopThreadRunning = eventListenerThreadRunningTouch = false;
            eventListenerThread = null;
            eventListenerThreadTouch = null;
            eventCleanLoop = null;
        }

        public static void StartEventListener(string ip, string port, string auth_token)
        {
            if (eventListenerThread != null)
                return;

            eventListenerThread = new Thread(new ParameterizedThreadStart(async (o) =>
            {
                string address = $"http://{ip}:{port}/api/v1/{auth_token}/events?id=1,2,3,4";
#pragma warning disable SYSLIB0014
                WebClient wc = new WebClient();
#pragma warning restore SYSLIB0014
                wc.Headers.Add("TouchEventsPort", _touchEventsPort.ToString());
                wc.OpenReadAsync(new Uri(address));
                bool isListening = true;
                bool restart = false;
                wc.OpenReadCompleted += (sender, args) =>
                {
                    while (!shutdown)
                    {
                        string res = string.Empty;
                        byte[] buffer;
                        using (MemoryStream ms = new MemoryStream())
                        {
                            List<byte[]> buffers = new List<byte[]>();
                            do
                            {
                                buffer = new byte[128];
                                try
                                {
                                    args.Result.Read(buffer, 0, buffer.Length);
                                }
                                catch (Exception e) when (e is IOException || e is WebException)//Timeout! Restart Listener without Logging
                                {
                                    _logger?.LogDebug("Restarting EventListener because of:" + Environment.NewLine, e.Message);
                                    restart = true;
                                    isListening = false;
                                    goto DISPOSE;
                                }
                                catch (Exception e) when (e is TargetInvocationException)
                                {
                                    _logger?.LogInformation("Connection Refused");
                                }
                                catch (Exception e)
                                {
                                    _logger?.LogDebug(string.Empty, e);
                                }
                                ms.Write(buffer, 0, buffer.Length);
                            }
                            while (buffer[buffer.Length - 1] != 0);
                            res = System.Text.Encoding.Default.GetString(TrimTailingZeros(ms.GetBuffer()));
                        }
                        FireEvent(ip, res);
                    }
                DISPOSE:
                    wc.Dispose();
                };
                while (isListening)
                    await Task.Delay(10);

                eventListenerThread = null;
                if (restart)
                    StartEventListener(ip, port, auth_token);
            }));
            eventListenerThread.Name = $"Nanoleaf StreamEventListener";
            eventListenerThread.Priority = ThreadPriority.BelowNormal;
            eventListenerThread.IsBackground = true;
            eventListenerThread.Start();
        }

        private static void FireEvent(string ip, string eventData)
        {
            eventData = eventData.Remove(0, 4);
            byte id = byte.Parse(eventData.First().ToString());
            eventData = eventData.Remove(0, 8).Replace("\n", "");

            switch (id)
            {
                case 1:
                    var stateEvents = JsonSerializer.Deserialize<StateEvents>(eventData);
                    StaticOnStateEvent?.InvokeFailSafe(ip, new StateEventArgs(ip, stateEvents));
                    break;
                case 2:
                    var layoutEvents = JsonSerializer.Deserialize<LayoutEvents>(eventData);
                    StaticOnLayoutEvent?.InvokeFailSafe(ip, new LayoutEventArgs(ip, layoutEvents));
                    break;
                case 3:
                    var effectEvent = JsonSerializer.Deserialize<EffectEvents>(eventData);
                    StaticOnEffectEvent?.InvokeFailSafe(ip, new EffectEventArgs(ip, effectEvent));
                    break;
                case 4:
                    var gestureEvents = JsonSerializer.Deserialize<GestureEvents>(eventData);
                    StaticOnGestureEvent?.InvokeFailSafe(ip, new GestureEventArgs(ip, gestureEvents));
                    break;
            }
        }
        public static byte[] TrimTailingZeros(byte[] arr)
        {
            if (arr == null)
                return new byte[0];
            if (arr.Length == 0)
                return arr;
            return arr.Reverse().SkipWhile(x => x == 0).Reverse().ToArray();
        }

        #endregion
        private static bool shutdown = false;
        public static void Shutdown()
        {
            _logger?.LogDebug($"Shutdown");
            shutdown = true;
            tokenSource.Cancel();
            Task.Delay(1000).GetAwaiter();
            tokenSource.Dispose();

            StopEventListener();

            tokenSource = new CancellationTokenSource();
            token = tokenSource.Token;
            discoveredDevices.Clear();
            _logger?.LogDebug($"Shutdown done!");
        }
#if DEBUG //Tests
        public static void Restart()
        {
            Shutdown();
            shutdown = false;
        }
#endif
    }
}
