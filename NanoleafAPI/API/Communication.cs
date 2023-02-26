using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using Zeroconf;
using static NanoleafAPI.TouchEvent;
using static System.Net.Mime.MediaTypeNames;
using static System.Net.WebRequestMethods;

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

        private static async Task<RestResponse?> put(string address, string contentString)
        {
            using (RestClient restClient = new RestClient(address))
            {
                try
                {
                    var request = new RestRequest((string?)null, Method.Put)
                    {
                        Timeout = 1000
                    };
                    request.AddJsonBody(contentString);
                    _logger?.LogDebug($"Put: {address} {contentString}");
                    return await restClient.ExecuteAsync(request).ConfigureAwait(false);
                }

                catch (HttpRequestException he)
                {
                    _logger?.LogDebug(he, string.Empty);
                }
            }
            return null;
        }

        #region Discover SSDP
        private static bool discoverySSDPTaskRunning = false;
        private static Task? discoverSSDPTask = null;

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
                    await Task.Delay(500);
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
                IPEndPoint? e = ((UdpState)result.AsyncState).EndPoint;
                if (client.Client == null ||e == null) 
                    return;

                byte[] buffer = client.EndReceive(result, ref e);
                // Handle received data in buffer, send reply to client etc...

                if (e == null)
                    return;
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
                        string? url = array.FirstOrDefault(s => s.StartsWith("Location"))?.Replace("Location: ", "").Replace("http://", "") ?? null;
                        string? ip = url?.Split(':')[0] ?? null;
                        string? port = url?.Split(':')[1] ?? null;
                        string? name = array.FirstOrDefault(s => s.StartsWith("nl-devicename"))?.Replace("nl-devicename: ", "") ?? null;
                        string? id = array.FirstOrDefault(s => s.StartsWith("nl-deviceid"))?.Replace("nl-deviceid: ", "") ?? null;
                        if (string.IsNullOrWhiteSpace(remoteAddress) || string.IsNullOrWhiteSpace(port) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(id))
                        {
                            _logger?.LogDebug($"Device Discovered via SSDP but can't be parsed correctly: {message}");
                            return;
                        }
                        else
                        {
                            var device = new DiscoveredDevice(remoteAddress, port, name, id, type);
                            discoveredDevices.Add(device);
                            _logger?.LogDebug($"Device Discovered via SSDP: {device}");
                            DeviceDiscovered?.InvokeFailSafe(null, new DiscoveredEventArgs(device));
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning("Not able to decode the SSDP Datagram", ex);
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

        public static async Task<bool> Ping(string ip, string port)
        {
            bool result = false;
            string address = $"http://{ip}:{port}/api/v1/";
            using (HttpClient hc = new HttpClient())
            {
                try
                {
                    StringContent queryString = new StringContent("");
                    _logger?.LogDebug($"Request {nameof(Ping)} for \"{ip}\"");
                    var response = await hc.PostAsync(address, queryString);

                    _logger?.LogDebug($"Received Response for {nameof(Ping)}: {response.StatusCode}");
                    return true;

                }
                catch (HttpRequestException he)
                {
                    _logger?.LogDebug(he, string.Empty);
                }
                catch (Exception e)
                {
                    _logger?.LogWarning(e, string.Empty);
                }
            }
            return result;
        }

        #region User
        public static async Task<string?> AddUser(string ip, string port)
        {
            string? result = null;
            string address = createUrl(ip, port, "new");
            using (HttpClient hc = new HttpClient())
            {
                try
                {
                    StringContent queryString = new StringContent("");
                    _logger?.LogDebug($"Request {nameof(AddUser)} for \"{ip}\"");
                    var response = await hc.PostAsync(address, queryString);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var responseStrings = await response.Content.ReadAsStringAsync();
                        var jObject = JObject.Parse(responseStrings);
                        result = jObject["auth_token"]?.ToString();
                        if (result != null)
                        {
                            result = result.Replace("\"", "");
                            _logger?.LogDebug($"Received {nameof(AddUser)} response: {jObject}");
                        }
                    }
                    else
                        _logger?.LogDebug($"Received Response for {nameof(AddUser)}: {response}");

                }
                catch (HttpRequestException he)
                {
                    _logger?.LogDebug(he, string.Empty);
                }
                catch (Exception e)
                {
                    _logger?.LogWarning(e, string.Empty);
                }
            }
            return result;
        }
        public static async Task<bool?> DeleteUser(string ip, string port, string auth_token)
        {
            bool? result = null;
            string address = createUrl(ip, port, auth_token, string.Empty);
            using (HttpClient hc = new HttpClient())
            {
                try
                {
                    _logger?.LogDebug($"Request {nameof(DeleteUser)} for \"{ip}\"");
                    var response = await hc.DeleteAsync(address);
                    result = response?.StatusCode == HttpStatusCode.NoContent;
                    if (result == true)
                        _logger?.LogDebug($"Received {nameof(DeleteUser)} response: successfull");
                }
                catch (HttpRequestException he)
                {
                    _logger?.LogDebug(he, string.Empty);
                }
                catch (Exception e)
                {
                    _logger?.LogWarning(e, string.Empty);
                }
            }
            return result;
        }
        #endregion
        #region All Panel Info
        public static async Task<AllPanelInfo?> GetAllPanelInfo(string ip, string port, string auth_token)
        {
            AllPanelInfo? result = null;
            string address = createUrl(ip, port, auth_token, string.Empty);
            using (HttpClient hc = new HttpClient())
            {
                try
                {
                    _logger?.LogDebug($"Request {nameof(GetAllPanelInfo)} for \"{ip}\"");
                    var response = await hc.GetAsync(address);
                    if (response?.StatusCode == HttpStatusCode.OK)
                    {
                        string content = await response.Content.ReadAsStringAsync();
                        result = JsonConvert.DeserializeObject<AllPanelInfo>(content);
                        if (result != null)
                            _logger?.LogDebug($"Received {nameof(GetAllPanelInfo)} response: {result}");
                        else
                            _logger?.LogDebug($"Received {nameof(GetAllPanelInfo)} response can't be Deserialized: {content}");
                    }
                    else
                        _logger?.LogDebug($"Received Response for {nameof(GetAllPanelInfo)}: {response}");
                }
                catch (HttpRequestException he)
                {
                    _logger?.LogDebug(he, string.Empty);
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
        public static async Task<bool?> GetStateOnOff(string ip, string port, string auth_token)
        {
            bool? result = null;
            string address = createUrl(ip, port, auth_token, "state/on");
            using (HttpClient hc = new HttpClient())
            {
                try
                {
                    _logger?.LogDebug($"Request {nameof(GetStateOnOff)} for \"{ip}\"");
                    var response = await hc.GetAsync(address);
                    if (response?.StatusCode == HttpStatusCode.OK)
                    {
                        string content = await response.Content.ReadAsStringAsync();
                        result = JsonConvert.DeserializeObject<StateOnOff>(content)?.On;
                        if (result != null)
                            _logger?.LogDebug($"Received {nameof(GetStateOnOff)} response: {result}");
                        else
                            _logger?.LogDebug($"Received {nameof(GetStateOnOff)} response can't be Deserialized: {content}");
                    }
                    else
                        _logger?.LogDebug($"Received Response for {nameof(GetStateOnOff)}: {response}");
                }
                catch (HttpRequestException he)
                {
                    _logger?.LogDebug(he, string.Empty);
                }
                catch (Exception e)
                {
                    _logger?.LogWarning(e, string.Empty);
                }
            }
            return result;
        }
        public static async Task<bool?> SetStateOnOff(string ip, string port, string auth_token, bool value)
        {
            bool? result = null;
            string address = createUrl(ip, port, auth_token, "state");
            string contentString = value ? "{\"on\" : {\"value\": true}}" : "{\"on\" : {\"value\": false}}";

            try
            {
                _logger?.LogDebug($"Request {nameof(SetStateOnOff)} State for \"{ip}\"");
                var response = await put(address, contentString);
                result = response?.StatusCode == HttpStatusCode.NoContent;

                if (result == true)
                    _logger?.LogDebug($"Received {nameof(SetStateOnOff)} response: successfull");
            }
            catch (Exception e)
            {
                _logger?.LogWarning(e, string.Empty);
            }
            return result;
        }
        #endregion
        #region Brightness
        public static async Task<ushort?> GetStateBrightness(string ip, string port, string auth_token)
        {
            ushort? result = null;
            string address = createUrl(ip, port, auth_token, "state/brightness");
            using (HttpClient hc = new HttpClient())
            {
                try
                {
                    _logger?.LogDebug($"Request {nameof(GetStateBrightness)} for \"{ip}\"");
                    var response = await hc.GetAsync(address);
                    if (response?.StatusCode == HttpStatusCode.OK)
                    {
                        string content = await response.Content.ReadAsStringAsync();
                        result = JsonConvert.DeserializeObject<StateInfo>(content)?.Value;
                        if (result != null)
                            _logger?.LogDebug($"Received {nameof(GetStateBrightness)}: {result}");
                        else
                            _logger?.LogDebug($"Received {nameof(GetStateBrightness)} response can't be Deserialized: {content}");
                    }
                    else
                        _logger?.LogDebug($"Received Response for {nameof(GetStateBrightness)}: {response}");
                }
                catch (HttpRequestException he)
                {
                    _logger?.LogDebug(he, string.Empty);
                }
                catch (Exception e)
                {
                    _logger?.LogWarning(e, string.Empty);
                }
            }
            return result;
        }
        public static async Task<bool?> SetStateBrightness(string ip, string port, string auth_token, ushort value, ushort duration = 0)
        {
            bool? result = null;
            string address = createUrl(ip, port, auth_token, "state");
            string? contentString = null;
            if (duration == 0)
                contentString = "{\"brightness\": {\"value\": " + value + "}}";
            else
                contentString = "{\"brightness\": {\"value\": " + value + ", \"duration\": " + duration + "}}";
            try
            {
                _logger?.LogDebug($"Request {nameof(SetStateBrightness)} for \"{ip}\"");
                var response = await put(address, contentString);
                result = response?.StatusCode == HttpStatusCode.NoContent;

                if (result == true)
                    _logger?.LogDebug($"Received {nameof(SetStateBrightness)} response: successfull");
            }
            catch (Exception e)
            {
                _logger?.LogWarning(e, string.Empty);
            }
            return result;
        }
        public static async Task<bool?> SetStateBrightnessIncrement(string ip, string port, string auth_token, short value)
        {
            bool? result = null;
            string address = createUrl(ip, port, auth_token, "state");
            string contentString = "{\"brightness\": {\"increment\": " + value + "}}";
            try
            {
                _logger?.LogDebug($"Request {nameof(SetStateBrightnessIncrement)} for \"{ip}\"");
                var response = await put(address, contentString);
                result = response?.StatusCode == HttpStatusCode.NoContent;

                if (result == true)
                    _logger?.LogDebug($"Received {nameof(SetStateBrightnessIncrement)} response: successfull");
            }
            catch (Exception e)
            {
                _logger?.LogWarning(e, string.Empty);
            }
            return result;
        }
        #endregion
        #region Hue
        public static async Task<ushort?> GetStateHue(string ip, string port, string auth_token)
        {
            ushort? result = null;
            string address = createUrl(ip, port, auth_token, "state/hue");
            using (HttpClient hc = new HttpClient())
            {
                try
                {
                    _logger?.LogDebug($"Request {nameof(GetStateHue)} for \"{ip}\"");
                    var response = await hc.GetAsync(address);
                    if (response?.StatusCode == HttpStatusCode.OK)
                    {
                        string content = await response.Content.ReadAsStringAsync();
                        result = JsonConvert.DeserializeObject<StateInfo>(content)?.Value;
                        if (result != null)
                            _logger?.LogDebug($"Received {nameof(GetStateHue)}: {result}");
                        else
                            _logger?.LogDebug($"Received {nameof(GetStateHue)} response can't be Deserialized: {content}");
                    }
                    else
                        _logger?.LogDebug($"Received Response for {nameof(GetStateHue)}: {response}");
                }
                catch (HttpRequestException he)
                {
                    _logger?.LogDebug(he, string.Empty);
                }
                catch (Exception e)
                {
                    _logger?.LogWarning(e, string.Empty);
                }
            }
            return result;
        }
        public static async Task<bool?> SetStateHue(string ip, string port, string auth_token, ushort value)
        {
            bool? result = null;
            string address = createUrl(ip, port, auth_token, "state");
            string contentString = "{\"hue\" : {\"value\": " + value + "}}";
            try
            {
                _logger?.LogDebug($"Request {nameof(SetStateHue)} for \"{ip}\"");
                var response = await put(address, contentString);
                result = response?.StatusCode == HttpStatusCode.NoContent;

                if (result == true)
                    _logger?.LogDebug($"Received {nameof(SetStateHue)} response: successfull");
            }
            catch (Exception e)
            {
                _logger?.LogWarning(e, string.Empty);
            }
            return result;
        }
        public static async Task<bool?> SetStateHueIncrement(string ip, string port, string auth_token, short value)
        {
            bool? result = null;
            string address = createUrl(ip, port, auth_token, "state");
            string contentString = "{\"hue\": {\"increment\": " + value + "}}";
            try
            {
                _logger?.LogDebug($"Request {nameof(SetStateHueIncrement)} for \"{ip}\"");
                var response = await put(address, contentString);
                result = response?.StatusCode == HttpStatusCode.NoContent;

                if (result == true)
                    _logger?.LogDebug($"Received {nameof(SetStateHueIncrement)} response: successfull");
            }
            catch (Exception e)
            {
                _logger?.LogWarning(e, string.Empty);
            }
            return result;
        }
        #endregion
        #region Saturation
        public static async Task<ushort?> GetStateSaturation(string ip, string port, string auth_token)
        {
            ushort? result = null;
            string address = createUrl(ip, port, auth_token, "state/sat");
            using (HttpClient hc = new HttpClient())
            {
                try
                {
                    _logger?.LogDebug($"Request {nameof(GetStateSaturation)} for \"{ip}\"");
                    var response = await hc.GetAsync(address);
                    if (response?.StatusCode == HttpStatusCode.OK)
                    {
                        string content = await response.Content.ReadAsStringAsync();
                        result = JsonConvert.DeserializeObject<StateInfo>(content)?.Value;
                        if (result != null)
                            _logger?.LogDebug($"Received {nameof(GetStateSaturation)}: {result}");
                        else
                            _logger?.LogDebug($"Received {nameof(GetStateSaturation)} response can't be Deserialized: {content}");
                    }
                    else
                        _logger?.LogDebug($"Received Response for {nameof(GetStateSaturation)}: {response}");
                }
                catch (HttpRequestException he)
                {
                    _logger?.LogDebug(he, string.Empty);
                }
                catch (Exception e)
                {
                    _logger?.LogWarning(e, string.Empty);
                }
            }
            return result;
        }
        public static async Task<bool?> SetStateSaturation(string ip, string port, string auth_token, ushort value)
        {
            bool? result = null;
            string address = createUrl(ip, port, auth_token, "state");
            string contentString = "{\"sat\" : {\"value\": " + value + "}}";
            try
            {
                _logger?.LogDebug($"Request {nameof(SetStateSaturation)} for \"{ip}\"");
                var response = await put(address, contentString);
                result = response?.StatusCode == HttpStatusCode.NoContent;

                if (result == true)
                    _logger?.LogDebug($"Received {nameof(SetStateSaturation)} response: successfull");
            }
            catch (Exception e)
            {
                _logger?.LogWarning(e, string.Empty);
            }
            return result;
        }
        public static async Task<bool?> SetStateSaturationIncrement(string ip, string port, string auth_token, short value)
        {
            bool? result = null;
            string address = createUrl(ip, port, auth_token, "state");
            string contentString = "{\"sat\": {\"increment\": " + value + "}}";
            try
            {
                _logger?.LogDebug($"Request {nameof(SetStateSaturationIncrement)} for \"{ip}\"");
                var response = await put(address, contentString);
                result = response?.StatusCode == HttpStatusCode.NoContent;

                if (result == true)
                    _logger?.LogDebug($"Received {nameof(SetStateSaturationIncrement)} response: successfull");
            }
            catch (Exception e)
            {
                _logger?.LogWarning(e, string.Empty);
            }
            return result;
        }
        #endregion
        #region ColorTemperature
        public static async Task<ushort?> GetStateColorTemperature(string ip, string port, string auth_token)
        {
            ushort? result = null;
            string address = createUrl(ip, port, auth_token, "state/ct");
            using (HttpClient hc = new HttpClient())
            {
                try
                {
                    _logger?.LogDebug($"Request {nameof(GetStateColorTemperature)} for \"{ip}\"");
                    var response = await hc.GetAsync(address);
                    if (response?.StatusCode == HttpStatusCode.OK)
                    {
                        string content = await response.Content.ReadAsStringAsync();
                        result = JsonConvert.DeserializeObject<StateInfo>(content)?.Value;
                        if (result != null)
                            _logger?.LogDebug($"Received {nameof(GetStateColorTemperature)}: {result}");
                        else
                            _logger?.LogDebug($"Received {nameof(GetStateColorTemperature)} response can't be Deserialized: {content}");
                    }
                    else
                        _logger?.LogDebug($"Received Response for {nameof(GetStateColorTemperature)}: {response}");
                }
                catch (HttpRequestException he)
                {
                    _logger?.LogDebug(he, string.Empty);
                }
                catch (Exception e)
                {
                    _logger?.LogWarning(e, string.Empty);
                }
            }
            return result;
        }
        public static async Task<bool?> SetStateColorTemperature(string ip, string port, string auth_token, ushort value)
        {
            bool? result = null;
            string address = createUrl(ip, port, auth_token, "state");
            string contentString = "{\"ct\" : {\"value\": " + value + "}}";
            try
            {
                _logger?.LogDebug($"Request {nameof(SetStateColorTemperature)} for \"{ip}\"");
                var response = await put(address, contentString);
                result = response?.StatusCode == HttpStatusCode.NoContent;

                if (result == true)
                    _logger?.LogDebug($"Received {nameof(SetStateColorTemperature)} response: successfull");
            }
            catch (Exception e)
            {
                _logger?.LogWarning(e, string.Empty);
            }
            return result;
        }
        public static async Task<bool?> SetStateColorTemperatureIncrement(string ip, string port, string auth_token, short value)
        {
            bool? result = null;
            string address = createUrl(ip, port, auth_token, "state");
            string contentString = "{\"ct\": {\"increment\": " + value + "}}";
            try
            {
                _logger?.LogDebug($"Request {nameof(SetStateColorTemperatureIncrement)} for \"{ip}\"");
                var response = await put(address, contentString);
                result = response?.StatusCode == HttpStatusCode.NoContent;

                if (result == true)
                    _logger?.LogDebug($"Received {nameof(SetStateColorTemperatureIncrement)} response: successfull");
            }
            catch (Exception e)
            {
                _logger?.LogWarning(e, string.Empty);
            }
            return result;
        }
        #endregion
        #region ColorMode
        public static async Task<string?> GetColorMode(string ip, string port, string auth_token)
        {
            string? result = null;
            string address = createUrl(ip, port, auth_token, "state/colorMode");
            using (HttpClient hc = new HttpClient())
            {
                try
                {
                    var response = await hc.GetAsync(address);
                    if (response?.StatusCode == HttpStatusCode.OK)
                    {
                        result = await response.Content.ReadAsStringAsync();
                        if (result != null)
                        {
                            result = result.Replace("\"", "");
                            _logger?.LogDebug($"Received {nameof(GetStateSaturation)}: {result}");
                        }
                    }
                    else
                        _logger?.LogDebug($"Received Response for {nameof(GetStateSaturation)}: {response}");
                }
                catch (HttpRequestException he)
                {
                    _logger?.LogDebug(he, string.Empty);
                }
                catch (Exception e)
                {
                    _logger?.LogWarning(e, string.Empty);
                }
            }
            return result;
        }

        public static async Task<bool?> SetColorMode(string ip, string port, string auth_token, string value)
        {
            bool? result = null;
            string address = createUrl(ip, port, auth_token, "state/colorMode");
            string contentString = "{" + $"\"select\": \"{value}\"" + "}";

            try
            {
                _logger?.LogDebug($"Request {nameof(SetColorMode)} for \"{ip}\"");
                var response = await put(address, contentString);
                result = response?.StatusCode == HttpStatusCode.NoContent;

                if (result == true)
                    _logger?.LogDebug($"Received {nameof(SetStateColorTemperatureIncrement)} response: successfull");
            }
            catch (Exception e)
            {
                _logger?.LogWarning(e, string.Empty);
            }
            return result;
        }
        #endregion
        #endregion

        #region Effects
        public static async Task<string?> GetSelectedEffect(string ip, string port, string auth_token)
        {
            string? result = null;
            string address = createUrl(ip, port, auth_token, "effects/select");
            using (HttpClient hc = new HttpClient())
            {
                try
                {
                    _logger?.LogDebug($"Request {nameof(GetSelectedEffect)} for \"{ip}\"");
                    var response = await hc.GetAsync(address);
                    if (response?.StatusCode == HttpStatusCode.OK)
                    {
                        result = await response.Content.ReadAsStringAsync();
                        if (result != null)
                        {
                            result = result.Replace("\"", "");
                            _logger?.LogDebug($"Received {nameof(GetStateSaturation)}: {result}");
                        }
                    }
                    else
                        _logger?.LogDebug($"Received Response for {nameof(GetSelectedEffect)}: {response}");
                }
                catch (HttpRequestException he)
                {
                    _logger?.LogDebug(he, string.Empty);
                }
                catch (Exception e)
                {
                    _logger?.LogWarning(e, string.Empty);
                }
            }
            return result;
        }
        public static async Task<bool?> SetSelectedEffect(string ip, string port, string auth_token, string value)
        {
            bool? result = null;
            string address = createUrl(ip, port, auth_token, "effects");
            string contentString = "{" + $"\"select\": \"{value}\"" + "}";
            try
            {
                _logger?.LogDebug($"Request {nameof(SetSelectedEffect)} for \"{ip}\"");
                var response = await put(address, contentString);
                result = response?.StatusCode == HttpStatusCode.NoContent;

                if (result == true)
                    _logger?.LogDebug($"Received {nameof(SetSelectedEffect)} response: successfull");
            }
            catch (Exception e)
            {
                _logger?.LogWarning(e, string.Empty);
            }
            return result;
        }
        public static async Task<string[]?> GetEffectList(string ip, string port, string auth_token)
        {
            string address = createUrl(ip, port, auth_token, "effects/effectsList");
            using (HttpClient hc = new HttpClient())
            {
                try
                {
                    _logger?.LogDebug($"Request {nameof(GetEffectList)} for \"{ip}\"");
                    var response = await hc.GetAsync(address);
                    if (response?.StatusCode == HttpStatusCode.OK)
                    {
                        string content = await response.Content.ReadAsStringAsync();
                        var deserialized = JsonConvert.DeserializeObject<IEnumerable<string>>(content);
                        if (deserialized != null)
                        {
                            _logger?.LogDebug($"Received {nameof(GetEffectList)} response: {content}");
                            return deserialized.ToArray();
                        }
                    }
                    else
                        _logger?.LogDebug($"Received Response for {nameof(GetEffectList)}: {response}");
                }
                catch (HttpRequestException he)
                {
                    _logger?.LogDebug(he, string.Empty);
                }
                catch (Exception e)
                {
                    _logger?.LogWarning(e, string.Empty);
                }
            }
            return null;
        }
        ///TODO 5.4.3. Write
        #endregion

        #region PanelLayout
        public static async Task<ushort?> GetPanelLayoutGlobalOrientation(string ip, string port, string auth_token)
        {
            ushort? result = null;
            string address = createUrl(ip, port, auth_token, "panelLayout/globalOrientation");
            using (HttpClient hc = new HttpClient())
            {
                try
                {
                    _logger?.LogDebug($"Request {nameof(GetPanelLayoutGlobalOrientation)} for \"{ip}\"");
                    var response = await hc.GetAsync(address);
                    if (response?.StatusCode == HttpStatusCode.OK)
                    {
                        string content = await response.Content.ReadAsStringAsync();
                        result = JsonConvert.DeserializeObject<StateInfo>(content)?.Value;
                        if (result != null)
                            _logger?.LogDebug($"Received {nameof(GetPanelLayoutGlobalOrientation)}: {result}");
                        else
                            _logger?.LogDebug($"Received {nameof(GetPanelLayoutGlobalOrientation)} response can't be Deserialized: {content}");
                    }
                    else
                        _logger?.LogDebug($"Received Response for {nameof(GetPanelLayoutGlobalOrientation)}: {response}");
                }
                catch (HttpRequestException he)
                {
                    _logger?.LogDebug(he, string.Empty);
                }
                catch (Exception e)
                {
                    _logger?.LogWarning(e, string.Empty);
                }
            }
            return result;
        }
        public static async Task<bool?> SetPanelLayoutGlobalOrientation(string ip, string port, string auth_token, ushort value)
        {
            bool? result = false;
            string address = createUrl(ip, port, auth_token, "panelLayout");
            string contentString = "{\"globalOrientation\" : {\"value\": " + value + "}}";
            try
            {
                _logger?.LogDebug($"Request {nameof(SetPanelLayoutGlobalOrientation)} for \"{ip}\"");
                var response = await put(address, contentString);
                result = response?.StatusCode == HttpStatusCode.NoContent;

                if (result == true)
                    _logger?.LogDebug($"Received {nameof(SetPanelLayoutGlobalOrientation)} response: successfull");
            }
            catch (Exception e)
            {
                _logger?.LogWarning(e, string.Empty);
            }
            return result;
        }
        public static async Task<Layout?> GetPanelLayoutLayout(string ip, string port, string auth_token)
        {
            Layout? result = null;
            string address = createUrl(ip, port, auth_token, "panelLayout/layout");
            using (HttpClient hc = new HttpClient())
            {
                try
                {
                    _logger?.LogDebug($"Request {nameof(GetPanelLayoutLayout)} for \"{ip}\"");
                    var response = await hc.GetAsync(address);
                    if (response?.StatusCode == HttpStatusCode.OK)
                    {
                        string content = await response.Content.ReadAsStringAsync();
                        result = JsonConvert.DeserializeObject<Layout>(content);
                        if (result != null)
                            _logger?.LogDebug($"Received {nameof(GetPanelLayoutLayout)}: {result}");
                        else
                            _logger?.LogDebug($"Received {nameof(GetPanelLayoutLayout)} response can't be Deserialized: {content}");
                    }
                    else
                        _logger?.LogDebug($"Received Response for {nameof(GetPanelLayoutLayout)}: {response}");
                }
                catch (HttpRequestException he)
                {
                    _logger?.LogDebug(he, string.Empty);
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

        public static async Task<bool?> Identify(string ip, string port, string auth_token)
        {
            bool? result = null;
            string address = createUrl(ip, port, auth_token, "identify");
            try
            {
                _logger?.LogDebug($"Request {nameof(Identify)} for \"{ip}\"");
                var response = await put(address, string.Empty);
                result = response?.StatusCode == HttpStatusCode.NoContent || response?.StatusCode == HttpStatusCode.OK;

                if (result == true)
                    _logger?.LogDebug($"Received {nameof(Identify)} response: successfull");
            }
            catch (HttpRequestException he)
            {
                _logger?.LogDebug(he, string.Empty);
            }
            catch (Exception e)
            {
                _logger?.LogWarning(e, string.Empty);
            }
            return result;
        }
        public static async Task<bool?> IdentifyAndroid(string ip)
        {
            bool? result = null;
            string address = $"http://{ip}:{6517}/identify-android";
            using (HttpClient hc = new HttpClient())
            {
                try
                {
                    StringContent queryString = new StringContent("");
                    _logger?.LogDebug($"Request {nameof(IdentifyAndroid)} for \"{ip}\"");
                    var response = await hc.PostAsync(address, queryString);

                    result = response?.StatusCode == HttpStatusCode.NoContent || response?.StatusCode == HttpStatusCode.OK;

                    if (result == true)
                        _logger?.LogDebug($"Received {nameof(IdentifyAndroid)} response: successfull");

                }
                catch (HttpRequestException he)
                {
                    _logger?.LogDebug(he, string.Empty);
                }
                catch (Exception e)
                {
                    _logger?.LogWarning(e, string.Empty);
                }
            }
            return result;
        }
        #endregion
        #region FirmwareUpgrade
        public static async Task<FirmwareUpgrade?> GetFirmwareUpgrade(string ip, string port, string auth_token)
        {
            FirmwareUpgrade? result = null;
            string address = createUrl(ip, port, auth_token, "firmwareUpgrade");
            using (HttpClient hc = new HttpClient())
            {
                try
                {
                    _logger?.LogDebug($"Request {nameof(GetFirmwareUpgrade)} for \"{ip}\"");
                    var response = await hc.GetAsync(address);
                    if (response?.StatusCode == HttpStatusCode.OK)
                    {
                        string content = await response.Content.ReadAsStringAsync();
                        result = JsonConvert.DeserializeObject<FirmwareUpgrade>(content);
                        if (result != null)
                            _logger?.LogDebug($"Received {nameof(GetFirmwareUpgrade)}: {result}");
                        else
                            _logger?.LogDebug($"Received {nameof(GetFirmwareUpgrade)} response can't be Deserialized: {content}");
                    }
                    else
                        _logger?.LogDebug($"Received Response for {nameof(GetFirmwareUpgrade)}: {response}");
                }
                catch (HttpRequestException he)
                {
                    _logger?.LogDebug(he, string.Empty);
                }
                catch (Exception e)
                {
                    _logger?.LogWarning(e, string.Empty);
                }
            }
            return result;
        }
        #endregion

        #region Commands
        public static async Task<string?> GetRequerstAll(string ip, string port, string auth_token)
        {
            string? result = null;
            string address = createUrl(ip, port, auth_token, "effects");
            string contentString = "{" + "\"write\":{ \"command\":\"requestAll\"} }";
            try
            {
                _logger?.LogDebug($"Request {nameof(GetRequerstAll)} for \"{ip}\"");
                var response = await put(address, contentString);
                if (response?.StatusCode == HttpStatusCode.OK)
                {
                    string? content = response.Content;
                    result = content;//JsonConvert.DeserializeObject<Layout>(content);
                    if (result != null)
                        _logger?.LogDebug($"Received {nameof(GetRequerstAll)}: {result}");
                    else
                        _logger?.LogDebug($"Received {nameof(GetRequerstAll)} response can't be Deserialized: {content}");
                }
                else
                    _logger?.LogDebug($"Received Response for {nameof(GetRequerstAll)}: {response}");
            }
            catch (Exception e)
            {
                _logger?.LogWarning(e, string.Empty);
            }
            return result;
        }
        public static async Task<string?> GetTouchConfig(string ip, string port, string auth_token)
        {
            string? result = null;
            string address = createUrl(ip, port, auth_token, "effects");
            string contentString = "{" + "\"write\":{ \"command\":\"requestTouchConfig\"} }";
            try
            {
                _logger?.LogDebug($"Request {nameof(GetTouchConfig)} for \"{ip}\"");
                var response = await put(address, contentString);
                if (response?.StatusCode == HttpStatusCode.OK)
                {
                    string? content = response.Content;
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
            return result;
        }
        public static async Task<string?> GetTouchKillSwitch(string ip, string port, string auth_token)
        {
            string? result = null;
            string address = createUrl(ip, port, auth_token, "effects");
            string contentString = "{" + "\"write\":{ \"command\":\"getTouchKillSwitch\"} }";
            try
            {
                _logger?.LogDebug($"Request {nameof(GetTouchKillSwitch)} for \"{ip}\"");
                var response = await put(address, contentString);
                if (response?.StatusCode == HttpStatusCode.OK)
                {
                    string? content = response.Content;
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
            return result;
        }
        public static async Task<bool?> SetTouchKillSwitch(string ip, string port, string auth_token, bool enabled)
        {
            bool? result = null;
            string address = createUrl(ip, port, auth_token, "effects");
            string contentString = "{" + "\"write\":{\"command\":\"setTouchKillSwitch\",\"touchKillSwitchOn\":" + enabled + "}}";
            try
            {
                _logger?.LogDebug($"Request {nameof(SetCommandSceneChangeAnimation)} for \"{ip}\"");
                var response = await put(address, contentString);
                result = response?.StatusCode == HttpStatusCode.NoContent;

                if (result == true)
                    _logger?.LogDebug($"Received {nameof(SetCommandSceneChangeAnimation)} response: successfull");
            }
            catch (Exception e)
            {
                _logger?.LogWarning(e, string.Empty);
            }
            return result;
        }
        public static async Task<bool?> SetCommandControllerButtons(string ip, string port, string auth_token, bool enabled)
        {
            bool? result = null;
            string address = createUrl(ip, port, auth_token, "effects");
            string contentString = enabled ? "{" + "\"write\":{ \"command\":\"enableAllControllerButtons\"} }" : "{" + "\"write\":{ \"command\":\"disableAllControllerButtons\"} }";
            try
            {
                _logger?.LogDebug($"Request {nameof(SetCommandControllerButtons)} for \"{ip}\"");
                var response = await put(address, contentString);
                result = response?.StatusCode == HttpStatusCode.NoContent;

                if (result == true)
                    _logger?.LogDebug($"Received {nameof(SetCommandControllerButtons)} response: successfull");
            }
            catch (Exception e)
            {
                _logger?.LogWarning(e, string.Empty);
            }
            return result;
        }
        public static async Task<bool?> SetCommandSceneChangeAnimation(string ip, string port, string auth_token, bool enabled)
        {
            bool? result = null;
            string address = createUrl(ip, port, auth_token, "effects");
            string contentString = enabled ? "{\"write\":{ \"command\":\"enableSceneChangeAnimation\"} }" : "{\"write\":{ \"command\":\"disableSceneChangeAnimation\"} }";
            try
            {
                _logger?.LogDebug($"Request {nameof(SetCommandSceneChangeAnimation)} for \"{ip}\"");
                var response = await put(address, contentString);
                result = response?.StatusCode == HttpStatusCode.NoContent;

                if (result == true)
                    _logger?.LogDebug($"Received {nameof(SetCommandSceneChangeAnimation)} response: successfull");
            }
            catch (Exception e)
            {
                _logger?.LogWarning(e, string.Empty);
            }
            return result;
        }
        public static async Task<bool?> SetCommandConfigureTouch(string ip, string port, string auth_token, bool enabled)
        {
            bool? result = null;
            string address = createUrl(ip, port, auth_token, "effects");
            string contentString = "{\"write\":{\"command\":\"configureTouch\",\"touchConfig\":{\"userSystemConfig\":{\"enabled\":" + enabled + "}}}}";
            try
            {
                _logger?.LogDebug($"Request {nameof(SetCommandSceneChangeAnimation)} for \"{ip}\"");
                var response = await put(address, contentString);
                result = response?.StatusCode == HttpStatusCode.NoContent;

                if (result == true)
                    _logger?.LogDebug($"Received {nameof(SetCommandSceneChangeAnimation)} response: successfull");
            }
            catch (Exception e)
            {
                _logger?.LogWarning(e, string.Empty);
            }
            return result;
        }
        #endregion

        #region External Control (Streaming)
        public static async Task<ExternalControlConnectionInfo?> SetExternalControlStreaming(string ip, string port, string auth_token, EDeviceType deviceType)
        {
            ExternalControlConnectionInfo? result = null;
            string address = createUrl(ip, port,auth_token,"effects");
            string contentString = "{\"write\": {\"command\": \"display\", \"animType\": \"extControl\", \"extControlVersion\": \"v2\"}}";
            try
            {
                _logger?.LogDebug($"Request {nameof(SetExternalControlStreaming)} for \"{ip}\"");
                var response = await put(address, contentString);

                switch (deviceType)
                {
                    case EDeviceType.LightPanles:
                        if (response?.StatusCode == HttpStatusCode.OK)
                        {
                            if (!string.IsNullOrWhiteSpace(response.Content))
                                result = JsonConvert.DeserializeObject<ExternalControlConnectionInfo>(response.Content);
                        }
                        else
                            _logger?.LogDebug($"Received Response for {nameof(SetExternalControlStreaming)}: {response}");
                        break;

                    case EDeviceType.Shapes:
                    case EDeviceType.Canvas:
                    case EDeviceType.Elements:
                    case EDeviceType.Lines:
                    case EDeviceType.Essentials:
                    default:
                        if (response?.StatusCode == HttpStatusCode.NoContent)
                        {
                            result = new ExternalControlConnectionInfo() { StreamIPAddress = ip, StreamPort = 60222, StreamProtocol = "udp" };
                        }
                        else
                            _logger?.LogDebug($"Received Response for {nameof(SetExternalControlStreaming)}: {response}");
                        break;
                }

                if (result != null)
                    _logger?.LogDebug($"Received {nameof(SetExternalControlStreaming)}: {result}");
                else
                    _logger?.LogDebug($"Received {nameof(SetExternalControlStreaming)} response can't be Deserialized: {response?.Content}");
            }
            catch (Exception e)
            {
                _logger?.LogWarning(e, string.Empty);
            }
            return result;
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
                if (_externalControlConnectionInfo == null)
                    return;

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

            if (string.IsNullOrWhiteSpace(path))
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
        private static Dictionary<string, TouchEvent> lastTouchEvent = new Dictionary<string, TouchEvent>();
        private static Thread? eventCleanLoop = null;
        private static bool eventListenerThreadRunning = false;
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

            if (eventListenerThread != null)
                return;

            eventListenerThread = new Thread(new ParameterizedThreadStart(async (o) =>
            {
                eventListenerThreadRunning = true;
                try
                {
                    using (var client = new UdpClient(NextFreePort()))
                    {
                        var endpoint = (IPEndPoint?)client.Client.LocalEndPoint;
                        if(endpoint == null)
                        {
                            _logger?.LogError("Endpoint in EventlistenerThread is null");
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
            eventListenerThread = null;
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
                    var stateEvents = JsonConvert.DeserializeObject<StateEvents>(eventData);
                    if (stateEvents == null)
                    {
                        _logger?.LogWarning($"Can't Deserialize {nameof(StateEvents)} Event-Data: {eventData}");
                        break;
                    }
                    StaticOnStateEvent?.InvokeFailSafe(ip, new StateEventArgs(ip, (StateEvents)stateEvents));
                    break;
                case 2:
                    var layoutEvent = JsonConvert.DeserializeObject<LayoutEvent>(eventData, LayoutEventConverter.Instance);
                    if (layoutEvent == null)
                    {
                        _logger?.LogWarning($"Can't Deserialize {nameof(LayoutEvent)} Event-Data: {eventData}");
                        break;
                    }
                    StaticOnLayoutEvent?.InvokeFailSafe(ip, new LayoutEventArgs(ip, layoutEvent));
                    break;
                case 3:
                    var effectEvent = JsonConvert.DeserializeObject<EffectEvents>(eventData);
                    if (effectEvent == null)
                    {
                        _logger?.LogWarning($"Can't Deserialize {nameof(EffectEvents)} Event-Data: {eventData}");
                        break;
                    }
                    StaticOnEffectEvent?.InvokeFailSafe(ip, new EffectEventArgs(ip, effectEvent));
                    break;
                case 4:
                    var gestureEvents = JsonConvert.DeserializeObject<GestureEvents>(eventData);
                    if (gestureEvents == null)
                    {
                        _logger?.LogWarning($"Can't Deserialize {nameof(GestureEvents)} Event-Data: {eventData}");
                        break;
                    }
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
            discoverSSDPTask = null;
            discoverySSDPTaskRunning = false;

            eventListenerThread = null;
            eventCleanLoop = null;

            tokenSource = new CancellationTokenSource();
            token = tokenSource.Token;
            _logger?.LogDebug($"Shutdown done!");
        }
#if DEBUG //Tests
        public static void Restart()
        {
            shutdown = false;
        }
#endif
    }
}
