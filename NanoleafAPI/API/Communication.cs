using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Linq;
using System.Text;
using static NanoleafAPI.TouchEvent;
using Zeroconf;
using System.Xml.Linq;
using System;

namespace NanoleafAPI
{
    public class Communication
    {
        private static ILoggerFactory loggerFactory = new LoggerFactory();
        private static ILogger __logger;
        private static  ILogger _logger
        {
            get
            {
                if (__logger == null)
                    __logger = loggerFactory.CreateLogger(nameof(Communication));
                return __logger;
            }
        }
        static CancellationTokenSource tokenSource = new CancellationTokenSource();
        static CancellationToken token = tokenSource.Token;

        private static List<IPAddress> ipAddresses = new List<IPAddress>();
        private static ReadOnlyCollection<IPAddress> IPAddresses
        {

            get
            {
                return ipAddresses.AsReadOnly();
            }
        }

        public static void RegisterIPAddress(IPAddress ipAddress)
        {
            if(ipAddresses.Any(ip=>ip.Equals(ipAddress)))
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
        public static event EventHandler<DiscoveredEventArgs> DeviceDiscovered;

        private static async Task<RestResponse> put(string address, string contentString)
        {
            RestClient restClient = new RestClient(address);
            var request = new RestRequest((String)null, Method.Put)
            {
                Timeout = 1000
            };
            request.AddJsonBody(contentString);
            return await restClient.ExecuteAsync(request).ConfigureAwait(false);
        }

        #region Discover SSDP
        private static bool discoverySSDPTaskRunning = false;
        private static Task discoverSSDPTask = null;

        private static IPAddress SSDP_IP = new IPAddress(new byte[] { 239, 255, 255, 250 });
        private static int SSDP_PORT = 1900;

        private static Dictionary<IPAddress, UdpClient> runningSSDPClients = new Dictionary<IPAddress, UdpClient>();

        public static void StartDiscoverySSDPTask()
        {
            if (discoverySSDPTaskRunning || discoverSSDPTask != null)
                return;

            discoverySSDPTaskRunning = true;
            discoverSSDPTask = new Task(async () =>
            {
                foreach (var ip in ipAddresses)
                    try
                    {
                        var client = new UdpClient();
                        runningSSDPClients.Add(ip, client);
                        client.ExclusiveAddressUse = false;
                        client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                        IPEndPoint e = new IPEndPoint(ip, SSDP_PORT);
                        client.Client.Bind(e);
                        client.JoinMulticastGroup(SSDP_IP);
                        client.MulticastLoopback = true;
                        UdpState s = new UdpState(client, e);
                        var result = client.BeginReceive(OnSSDPReceived, s);
                    }
                    catch (Exception e)
                    {
                        _logger?.LogWarning("The Socket is already in use." + Environment.NewLine +
                            "there Are a feaw things to fix this issue." + Environment.NewLine +
                            "Open the CMD.exe and perform the command \"netstat -a -n -o\"" + Environment.NewLine +
                            "Now you see all open Ports" + Environment.NewLine +
                            "find TCP [the IP address]:[port number] .... #[target_PID]# (ditto for UDP)" + Environment.NewLine +
                            "Open TaskManager and Klick on Processes" + Environment.NewLine +
                            "Enable \"PID\" column by going to: View > Select Columns > Check the box for PID" + Environment.NewLine +
                            "Find the PID of interest and \"END PROCESS\"" + Environment.NewLine + Environment.NewLine +
                            "Common Programs are Spotify or the SSDPSRF-Service"
                            , e);
                    }
                _logger?.LogDebug("SSDP DiscoverTask started");
                while (discoverySSDPTaskRunning)
                    Task.Delay(500);
                foreach (var client in runningSSDPClients)
                {
                    if (client.Value.Client != null)
                        client.Value.Close();
                }
                discoverySSDPTaskRunning = false;
                _logger?.LogDebug("SSDP Discover stopped");
                _logger?.LogDebug("SSDP DiscoverTask stopped");
            }, token);
            discoverSSDPTask.Start();
        }


        static void OnSSDPReceived(IAsyncResult result)
        {
            if (result.AsyncState is UdpState)
            {
                UdpClient client = ((UdpState)result.AsyncState).UdpClient;
                IPEndPoint e = ((UdpState)result.AsyncState).EndPoint;
                if (client.Client == null)
                    return;
                byte[] buffer = client.EndReceive(result, ref e);
                // Handle received data in buffer, send reply to client etc...

                string message = Encoding.Default.GetString(buffer);
                // Start a new async receive on the client to receive more data.
                client.BeginReceive(OnSSDPReceived, result.AsyncState);
                try
                {
                    if (message.Contains("nl-devicename"))
                    {
                        string remoteAddress = e.Address.ToString();
                        if (discoveredDevices.Any(d => d.IP.Equals(remoteAddress)))
                            return;
                        var array = message.Replace("\r\n", "|").Split('|');
                        EDeviceType type = Tools.ModelStringToEnum(array.FirstOrDefault(s => s.StartsWith("NT"))?.Replace("NT: ", "").Split(':').LastOrDefault());
                        string url = array.FirstOrDefault(s => s.StartsWith("Location"))?.Replace("Location: ", "").Replace("http://", "");
                        string ip = url.Split(':')[0];
                        string port = url.Split(':')[1];
                        string name = array.FirstOrDefault(s => s.StartsWith("nl-devicename"))?.Replace("nl-devicename: ", "");
                        string id = array.FirstOrDefault(s => s.StartsWith("nl-deviceid"))?.Replace("nl-deviceid: ", "");
                        var device = new DiscoveredDevice(remoteAddress, port, name, id, type); ;
                        discoveredDevices.Add(device);
                        DeviceDiscovered?.Invoke(null, new DiscoveredEventArgs(device));
                    }
                }
                catch(Exception ex)
                {
                    _logger.LogWarning("Not able to decode the SSDP Datagram");
                }
            }
        }
        public static void StopDiscoverySSDPTask()
        {
            _logger?.LogDebug("Request stop for SSDP DiscoverTask");
            discoverySSDPTaskRunning = false;
            for (int i = 0; i < 10; i++)
            {
                if (discoverSSDPTask?.IsCompleted ?? true)
                    return;
                _logger?.LogDebug("Await SSDP DiscoverTask stopped");
                Task.Delay(100).GetAwaiter();
            }
            discoverSSDPTask = null;
        }
        #endregion

        #region Discover mDNS
        private static bool discoverymDNSTaskRunning = false;
        private static Task discovermDNSTask = null;

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
                            string id = service.Properties.FirstOrDefault()?.FirstOrDefault(p => p.Key.Equals("id")).Value;

                            var device = new DiscoveredDevice(ip, port, name, id, type); ;
                            discoveredDevices.Add(device);
                            DeviceDiscovered?.Invoke(null, new DiscoveredEventArgs(device));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning("Not able to decode the mDNS Datagram");
                        }
                    }
                    Task.Delay(500);
                }
            }, token);
            discovermDNSTask.Start();
        }
        static void OnmDNSReceived(IAsyncResult result)
        {
            if (result.AsyncState is UdpState)
            {
                UdpClient client = ((UdpState)result.AsyncState).UdpClient;
                IPEndPoint e = ((UdpState)result.AsyncState).EndPoint;
                if (client.Client == null)
                    return;
                byte[] buffer = client.EndReceive(result, ref e);
                // Handle received data in buffer, send reply to client etc...

                string message = Encoding.Default.GetString(buffer);
                // Start a new async receive on the client to receive more data.
                client.BeginReceive(OnmDNSReceived, result.AsyncState);
                try
                {
                    if (message.Contains("nanoleaf"))
                    {
                        string remoteAddress = e.Address.ToString();
                        if (discoveredDevices.Any(d => d.IP.Equals(remoteAddress)))
                            return;
                        var array = message.Replace("\r\n", "|").Split('|');
                        EDeviceType type = EDeviceType.UNKNOWN;
                        switch (array.FirstOrDefault(s => s.StartsWith("NT"))?.Replace("NT: ", ""))
                        {
                            case "nanoleaf:nl22":
                            case "Nanoleaf_aurora:light":
                                type = EDeviceType.LightPanles;
                                break;
                            case "nanoleaf:nl29":
                                type = EDeviceType.Canvas;
                                break;
                            case "nanoleaf:nl42":
                                type = EDeviceType.Shapes;
                                break;
                            case "nanoleaf:nl45":
                                type = EDeviceType.Essentials;
                                break;
                            case "nanoleaf:nl52":
                                type = EDeviceType.Elements;
                                break;
                            case "nanoleaf:nl59":
                                type = EDeviceType.Lines;
                                break;
                        }
                        string url = array.FirstOrDefault(s => s.StartsWith("Location"))?.Replace("Location: ", "").Replace("http://", "");
                        string ip = url.Split(':')[0];
                        string port = url.Split(':')[1];
                        string name = array.FirstOrDefault(s => s.StartsWith("nl-devicename"))?.Replace("nl-devicename: ", "");
                        string id = array.FirstOrDefault(s => s.StartsWith("nl-deviceid"))?.Replace("nl-deviceid: ", "");
                        var device = new DiscoveredDevice(remoteAddress, port, name, id, type); ;
                        discoveredDevices.Add(device);
                        DeviceDiscovered?.Invoke(null, new DiscoveredEventArgs(device));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Not able to decode the SSDP Datagram");
                }
            }
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

        #region User
        public static async Task<string> AddUser(string ip, string port)
        {
            string result = null;
            string address = $"http://{ip}:{port}/api/v1/new";
            using (HttpClient hc = new HttpClient())
            {
                StringContent queryString = new StringContent("");
                var response = hc.PostAsync(address, queryString).GetAwaiter().GetResult();
                var responseStrings = await response.Content.ReadAsStringAsync();

                var jObject = JObject.Parse(responseStrings);
                result = jObject["auth_token"].ToString();

            }
            return result.Replace("\"", "");
        }
        public static async Task<bool> DeleteUser(string ip, string port, string auth_token)
        {
            bool result = false;
            string address = $"http://{ip}:{port}/api/v1/{auth_token}";
            using (HttpClient hc = new HttpClient())
            {
                var response = await hc.DeleteAsync(address);
                result = response.StatusCode == System.Net.HttpStatusCode.NoContent;
            }
            return result;
        }
        #endregion
        #region All Panel Info
        public static async Task<AllPanelInfo> GetAllPanelInfo(string ip, string port, string auth_token)
        {
            AllPanelInfo result = null;
            string address = $"http://{ip}:{port}/api/v1/{auth_token}";
            using (HttpClient hc = new HttpClient())
            {
                try
                {
                    var response = await hc.GetAsync(address);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        string res = await response.Content.ReadAsStringAsync();
                        result = JsonConvert.DeserializeObject<AllPanelInfo>(res);
                    }
                }
                catch (HttpRequestException)
                {
                    return null;
                }
                catch (Exception e)
                {
                    _logger?.LogWarning(e, string.Empty);
                }
            }
            return result;
        }
        #endregion
        #region State
        #region On/Off
        public static async Task<bool> GetStateOnOff(string ip, string port, string auth_token)
        {
            bool result = false;
            string address = $"http://{ip}:{port}/api/v1/{auth_token}/state/on";
            using (HttpClient hc = new HttpClient())
            {
                try
                {
                    var response = await hc.GetAsync(address);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        result = JsonConvert.DeserializeObject<StateOnOff>(await response.Content.ReadAsStringAsync()).On;
                }
                catch (HttpRequestException)
                {
                    return false;
                }
                catch (Exception e)
                {
                    _logger?.LogWarning(e, string.Empty);
                }
            }
            return result;
        }
        public static async Task<bool> SetStateOnOff(string ip, string port, string auth_token, bool value)
        {
            bool result = false;
            string address = $"http://{ip}:{port}/api/v1/{auth_token}/state";
            string contentString = value ? "{\"on\" : {\"value\": true}}" : "{\"on\" : {\"value\": false}}";

            var response = await put(address, contentString);
            result = response.StatusCode == System.Net.HttpStatusCode.NoContent;

            return result;
        }
        #endregion
        #region Brightness
        public static async Task<ushort> GetStateBrightness(string ip, string port, string auth_token)
        {
            ushort result = 0;
            string address = $"http://{ip}:{port}/api/v1/{auth_token}/state/brightness";
            using (HttpClient hc = new HttpClient())
            {
                try
                {
                    var response = await hc.GetAsync(address);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        result = JsonConvert.DeserializeObject<StateInfo>(await response.Content.ReadAsStringAsync()).Value;
                }
                catch (HttpRequestException)
                {
                    return 0;
                }
                catch (Exception e)
                {
                    _logger?.LogWarning(e, string.Empty);
                }
            }
            return result;
        }
        public static async Task<bool> SetStateBrightness(string ip, string port, string auth_token, ushort value, ushort duration = 0)
        {
            bool result = false;
            string address = $"http://{ip}:{port}/api/v1/{auth_token}/state";
            string contentString = null;
            if (duration == 0)
                contentString = "{\"brightness\": {\"value\": " + value + "}}";
            else
                contentString = "{\"brightness\": {\"value\": " + value + ", \"duration\": " + duration + "}}";

            var response = await put(address, contentString);
            result = response.StatusCode == System.Net.HttpStatusCode.NoContent;

            return result;
        }
        public static async Task<bool> SetStateBrightnessIncrement(string ip, string port, string auth_token, short value)
        {
            bool result = false;
            string address = $"http://{ip}:{port}/api/v1/{auth_token}/state";
            string contentString = null;
            contentString = "{\"brightness\": {\"increment\": " + value + "}}";

            var response = await put(address, contentString);
            result = response.StatusCode == System.Net.HttpStatusCode.NoContent;

            return result;
        }
        #endregion
        #region Hue
        public static async Task<ushort> GetStateHue(string ip, string port, string auth_token)
        {
            ushort result = 0;
            string address = $"http://{ip}:{port}/api/v1/{auth_token}/state/hue";
            using (HttpClient hc = new HttpClient())
            {
                try
                {
                    var response = await hc.GetAsync(address);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        result = JsonConvert.DeserializeObject<StateInfo>(await response.Content.ReadAsStringAsync()).Value;
                }
                catch (HttpRequestException)
                {
                    return 0;
                }
                catch (Exception e)
                {
                    _logger?.LogWarning(e, string.Empty);
                }
            }
            return result;
        }
        public static async Task<bool> SetStateHue(string ip, string port, string auth_token, ushort value)
        {
            bool result = false;
            string address = $"http://{ip}:{port}/api/v1/{auth_token}/state";
            string contentString = "{\"hue\" : {\"value\": " + value + "}}";

            var response = await put(address, contentString);
            result = response.StatusCode == System.Net.HttpStatusCode.NoContent;

            return result;
        }
        public static async Task<bool> SetStateHueIncrement(string ip, string port, string auth_token, short value)
        {
            bool result = false;
            string address = $"http://{ip}:{port}/api/v1/{auth_token}/state";
            string contentString = "{\"hue\": {\"increment\": " + value + "}}";

            var response = await put(address, contentString);
            result = response.StatusCode == System.Net.HttpStatusCode.NoContent;

            return result;
        }
        #endregion
        #region Saturation
        public static async Task<ushort> GetStateSaturation(string ip, string port, string auth_token)
        {
            ushort result = 0;
            string address = $"http://{ip}:{port}/api/v1/{auth_token}/state/sat";
            using (HttpClient hc = new HttpClient())
            {
                try
                {
                    var response = await hc.GetAsync(address);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        result = JsonConvert.DeserializeObject<StateInfo>(await response.Content.ReadAsStringAsync()).Value;
                }
                catch (HttpRequestException)
                {
                    return 0;
                }
                catch (Exception e)
                {
                    _logger?.LogWarning(e, string.Empty);
                }
            }
            return result;
        }
        public static async Task<bool> SetStateSaturation(string ip, string port, string auth_token, ushort value)
        {
            bool result = false;
            string address = $"http://{ip}:{port}/api/v1/{auth_token}/state";
            string contentString = "{\"sat\" : {\"value\": " + value + "}}";

            var response = await put(address, contentString);
            result = response.StatusCode == System.Net.HttpStatusCode.NoContent;

            return result;
        }
        public static async Task<bool> SetStateSaturationIncrement(string ip, string port, string auth_token, short value)
        {
            bool result = false;
            string address = $"http://{ip}:{port}/api/v1/{auth_token}/state";
            string contentString = "{\"sat\": {\"increment\": " + value + "}}";

            var response = await put(address, contentString);
            result = response.StatusCode == System.Net.HttpStatusCode.NoContent;

            return result;
        }
        #endregion
        #region ColorTemperature
        public static async Task<ushort> GetStateColorTemperature(string ip, string port, string auth_token)
        {
            ushort result = 0;
            string address = $"http://{ip}:{port}/api/v1/{auth_token}/state/ct";
            using (HttpClient hc = new HttpClient())
            {
                try
                {
                    var response = await hc.GetAsync(address);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        result = JsonConvert.DeserializeObject<StateInfo>(await response.Content.ReadAsStringAsync()).Value;
                }
                catch (HttpRequestException)
                {
                    return 0;
                }
                catch (Exception e)
                {
                    _logger?.LogWarning(e, string.Empty);
                }
            }
            return result;
        }
        public static async Task<bool> SetStateColorTemperature(string ip, string port, string auth_token, ushort value)
        {
            bool result = false;
            string address = $"http://{ip}:{port}/api/v1/{auth_token}/state";
            string contentString = "{\"ct\" : {\"value\": " + value + "}}";

            var response = await put(address, contentString);
            result = response.StatusCode == System.Net.HttpStatusCode.NoContent;

            return result;
        }
        public static async Task<bool> SetStateColorTemperatureIncrement(string ip, string port, string auth_token, short value)
        {
            bool result = false;
            string address = $"http://{ip}:{port}/api/v1/{auth_token}/state";
            string contentString = "{\"ct\": {\"increment\": " + value + "}}";

            var response = await put(address, contentString);
            result = response.StatusCode == System.Net.HttpStatusCode.NoContent;

            return result;
        }
        #endregion
        #region ColorMode
        public static async Task<string> GetColorMode(string ip, string port, string auth_token)
        {
            string result = null;
            string address = $"http://{ip}:{port}/api/v1/{auth_token}/state/colorMode";
            using (HttpClient hc = new HttpClient())
            {
                try
                {
                    var response = await hc.GetAsync(address);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        result = await response.Content.ReadAsStringAsync();
                }
                catch (HttpRequestException)
                {
                    return null;
                }
                catch (Exception e)
                {
                    _logger?.LogWarning(e, string.Empty);
                }
            }
            return result.Replace("\"", "");
        }

        public static async Task<bool> SetColorMode(string ip, string port, string auth_token, string value)
        {
            bool result = false;
            string address = $"http://{ip}:{port}/api/v1/{auth_token}/state/colorMode";
            string contentString = "{" + $"\"select\": \"{value}\"" + "}";

            var response = await put(address, contentString);
            result = response.StatusCode == System.Net.HttpStatusCode.NoContent;

            return result;
        }
        #endregion
        #endregion

        #region Effects
        public static async Task<string> GetSelectedEffect(string ip, string port, string auth_token)
        {
            string result = null;
            string address = $"http://{ip}:{port}/api/v1/{auth_token}/effects/select";
            using (HttpClient hc = new HttpClient())
            {
                try
                {
                    var response = await hc.GetAsync(address);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        result = await response.Content.ReadAsStringAsync();
                }
                catch (HttpRequestException)
                {
                    return null;
                }
                catch (Exception e)
                {
                    _logger?.LogWarning(e, string.Empty);
                }
            }
            return result.Replace("\"", "");
        }
        public static async Task<bool> SetSelectedEffect(string ip, string port, string auth_token, string value)
        {
            bool result = false;
            string address = $"http://{ip}:{port}/api/v1/{auth_token}/effects";
            string contentString = "{" + $"\"select\": \"{value}\"" + "}";

            var response = await put(address, contentString);
            result = response.StatusCode == System.Net.HttpStatusCode.NoContent;

            return result;
        }
        public static async Task<string[]> GetEffectList(string ip, string port, string auth_token)
        {
            string[] result = null;
            string address = $"http://{ip}:{port}/api/v1/{auth_token}/effects/effectsList";
            using (HttpClient hc = new HttpClient())
            {
                try
                {
                    var response = await hc.GetAsync(address);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        result = JsonConvert.DeserializeObject<IEnumerable<string>>(await response.Content.ReadAsStringAsync()).ToArray();
                }
                catch (HttpRequestException)
                {
                    return null;
                }
                catch (Exception e)
                {
                    _logger?.LogWarning(e, string.Empty);
                }
            }
            return result;
        }
        ///TODO 5.4.3. Write
        #endregion

        #region PanelLayout
        public static async Task<ushort> GetPanelLayoutGlobalOrientation(string ip, string port, string auth_token)
        {
            ushort result = 0;
            string address = $"http://{ip}:{port}/api/v1/{auth_token}/panelLayout/globalOrientation";
            using (HttpClient hc = new HttpClient())
            {
                try
                {
                    var response = await hc.GetAsync(address);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        result = JsonConvert.DeserializeObject<StateInfo>(await response.Content.ReadAsStringAsync()).Value;
                }
                catch (HttpRequestException)
                {
                    return 0;
                }
                catch (Exception e)
                {
                    _logger?.LogWarning(e, string.Empty);
                }
            }
            return result;
        }
        public static async Task<bool> SetPanelLayoutGlobalOrientation(string ip, string port, string auth_token, ushort value)
        {
            bool result = false;
            string address = $"http://{ip}:{port}/api/v1/{auth_token}/panelLayout";
            string contentString = "{\"globalOrientation\" : {\"value\": " + value + "}}";

            var response = await put(address, contentString);
            result = response.StatusCode == System.Net.HttpStatusCode.NoContent;

            return result;
        }
        public static async Task<Layout> GetPanelLayoutLayout(string ip, string port, string auth_token)
        {
            Layout result = null;
            string address = $"http://{ip}:{port}/api/v1/{auth_token}/panelLayout/layout";
            using (HttpClient hc = new HttpClient())
            {
                try
                {
                    var response = await hc.GetAsync(address);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        string res = await response.Content.ReadAsStringAsync();
                        result = JsonConvert.DeserializeObject<Layout>(res);
                    }
                }
                catch (HttpRequestException)
                {
                    return null;
                }
                catch (Exception e)
                {
                    _logger?.LogWarning(e, string.Empty);
                }
            }
            return result;
        }
        #endregion
        #region Identify

        public static async Task<bool> Identify(string ip, string port, string auth_token)
        {
            bool result = false;
            string address = $"http://{ip}:{port}/api/v1/{auth_token}/identify";

            var response = await put(address, string.Empty);
            result = response.StatusCode == System.Net.HttpStatusCode.NoContent || response.StatusCode == System.Net.HttpStatusCode.OK;

            return result;
        }
        #endregion

        #region External Control (Streaming)
        public static async Task<ExternalControlConnectionInfo> SetExternalControlStreaming(string ip, string port, string auth_token, EDeviceType deviceType)
        {
            ExternalControlConnectionInfo result = null;
            string address = $"http://{ip}:{port}/api/v1/{auth_token}/effects";
            string contentString = "{\"write\": {\"command\": \"display\", \"animType\": \"extControl\", \"extControlVersion\": \"v2\"}}";

            var response = await put(address, contentString);

            switch (deviceType)
            {
                case EDeviceType.LightPanles:
                    result = JsonConvert.DeserializeObject<ExternalControlConnectionInfo>(response.Content);
                    break;

                case EDeviceType.Shapes:
                case EDeviceType.Canvas:
                case EDeviceType.Elements:
                case EDeviceType.Lines:
                case EDeviceType.Essentials:
                default:
                    result = new ExternalControlConnectionInfo() { StreamIPAddress = ip, StreamPort = 60222, StreamProtocol = "udp" };
                    break;
            }
            return result;
        }
        public static byte[] CreateStreamingData(IEnumerable<Panel> panels)
        {
            byte[] result = null;
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

                    result = ms.ToArray();
                }
            }
            return result;
        }
        public static void SendUDPCommand(ExternalControlConnectionInfo _externalControlConnectionInfo, params byte[] data)
        {
            if (_externalControlConnectionInfo == null)
                return;
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            var endpoint = new IPEndPoint(IPAddress.Parse(_externalControlConnectionInfo.StreamIPAddress), _externalControlConnectionInfo.StreamPort);
            socket.SendTo(data, endpoint);
            socket.Close();
        }
        #endregion

        #region Events

        public static event EventHandler<TouchEventArgs> StaticOnTouchEvent;
        public static event EventHandler<StateEventArgs> StaticOnStateEvent;
        public static event EventHandler<LayoutEventArgs> StaticOnLayoutEvent;
        public static event EventHandler<GestureEventArgs> StaticOnGestureEvent;
        public static event EventHandler<EffectEventArgs> StaticOnEffectEvent;

        private static int _touchEventsPort = -1;
        private static Thread eventListenerThread = null;
        private static Dictionary<string, TouchEvent> lastTouchEvent = new Dictionary<string, TouchEvent>();
        private static Thread eventCleanLoop = null;
        private static bool eventListenerThreadRunning = false;
        private static bool eventCleanLoopThreadRunning = false;

        public static void StartEventListener()
        {
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
                                    long timestamp = DateTime.Now.Ticks;
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
                            StaticOnTouchEvent?.Invoke(null, new TouchEventArgs(_event.Key, _event.Value));
                        }
                    }
                });
                eventCleanLoop.Name = "Nanoleaf Event-Cleaner";
                eventCleanLoop.Priority = ThreadPriority.Lowest;
                eventCleanLoop.IsBackground = true;
                eventCleanLoop.Start();
            }

            if (eventListenerThread != null)
                return;

            eventListenerThread = new Thread(new ParameterizedThreadStart(async (o) =>
            {
                eventListenerThreadRunning = true;
                try
                {
                    using (var client = new UdpClient(NextFreePort()))
                    {
                        _touchEventsPort = ((IPEndPoint)client.Client.LocalEndPoint).Port;
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
                                    StaticOnTouchEvent?.Invoke(null, new TouchEventArgs(ip, touchEvent));
                                }
                            }
                            catch (Exception)
                            {

                            }
                        } while (eventListenerThreadRunning);
                    }
                }
                catch (Exception)
                {

                }
            }));
            eventListenerThread.Name = $"Nanoleaf TouchEventListener";
            eventListenerThread.Priority = ThreadPriority.BelowNormal;
            eventListenerThread.IsBackground = true;
            eventListenerThread.Start();

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
            eventCleanLoopThreadRunning = eventListenerThreadRunning = false;

            eventListenerThread?.Abort();
            eventListenerThread = null;

            eventCleanLoop?.Abort();
            eventCleanLoop = null;
        }

        public static async Task StartEventListener(string ip, string port, string auth_token)
        {
            eventListenerThread = new Thread(new ParameterizedThreadStart(async (o) =>
            {
                string address = $"http://{ip}:{port}/api/v1/{auth_token}/events?id=1,2,3,4";
                WebClient wc = new WebClient();
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
                                   //// NanoleafPlugin.Log.LogDebug("Restarting EventListener because of:" + Environment.NewLine, e.Message);
                                    restart = true;
                                    isListening = false;
                                    goto DISPOSE;
                                }
                                catch (Exception e) when (e is TargetInvocationException)
                                {
                                    //NanoleafPlugin.Log.LogInformation("Connection Refused");
                                }
                                catch (Exception e)
                                {
                                   //// NanoleafPlugin.Log.ErrorOrDebug(string.Empty, e);
                                }
                                ms.Write(buffer, 0, buffer.Length);
                            }
                            while (buffer[buffer.Length - 1] != 0);
                            res = System.Text.Encoding.Default.GetString(TrimTailingZeros(ms.GetBuffer()));
                        }
                        _ = FireEvent(ip, res);
                    }
                DISPOSE:
                    wc.Dispose();
                };
                while (isListening)
                    await Task.Delay(10);

                if (restart)
                    _ = StartEventListener(ip, port, auth_token);
            }));
            eventListenerThread.Name = $"Nanoleaf StreamEventListener";
            eventListenerThread.Priority = ThreadPriority.BelowNormal;
            eventListenerThread.IsBackground = true;
            eventListenerThread.Start();
        }

        private static async Task FireEvent(string ip, string eventData)
        {
            eventData = eventData.Remove(0, 4);
            byte id = byte.Parse(eventData.First().ToString());
            eventData = eventData.Remove(0, 8).Replace("\n", "");

            switch (id)
            {
                case 1:
                    StateEvents stateEvents = JsonConvert.DeserializeObject<StateEvents>(eventData);
                    StaticOnStateEvent?.Invoke(null, new StateEventArgs(ip, stateEvents));
                    break;
                case 2:
                    LayoutEvent layoutEvent = JsonConvert.DeserializeObject<LayoutEvent>(eventData, LayoutEventConverter.Instance);
                    StaticOnLayoutEvent?.Invoke(null, new LayoutEventArgs(ip, layoutEvent));
                    break;
                case 3:
                    EffectEvents effectEvent = JsonConvert.DeserializeObject<EffectEvents>(eventData);
                    StaticOnEffectEvent?.Invoke(null, new EffectEventArgs(ip, effectEvent));
                    break;
                case 4:
                    GestureEvents gestureEvents = JsonConvert.DeserializeObject<GestureEvents>(eventData);
                    StaticOnGestureEvent?.Invoke(null, new GestureEventArgs(ip, gestureEvents));
                    break;
            }
        }
        public static byte[] TrimTailingZeros(byte[] arr)
        {
            if (arr == null || arr.Length == 0)
                return arr;
            return arr.Reverse().SkipWhile(x => x == 0).Reverse().ToArray();
        }

        #endregion
        private static bool shutdown = false;
        public static void Shutdown()
        {
            shutdown = true;
            tokenSource.Cancel();
            Task.Delay(1000).GetAwaiter();
            tokenSource.Dispose();
            discoverSSDPTask = null;
            discoverySSDPTaskRunning = false;

            eventListenerThread?.Abort();
            eventListenerThread = null;

            eventCleanLoop?.Abort();
            eventCleanLoop = null;

            tokenSource = new CancellationTokenSource();
            token = tokenSource.Token;
        }
#if DEBUG //Tests
        public static void Restart()
        {
            shutdown = false;
        }
#endif
    }
}
