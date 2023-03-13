using System.Text.Json;
using System.Text.Json.Serialization;

namespace NanoleafAPI
{
    public readonly struct DiscoveredDevice
    {
        public readonly string IP { get; }
        public readonly string Port { get; }
        public readonly string Name { get; }
        public readonly string ID { get; }
        public readonly EDeviceType DeviceType { get; }
        [JsonConstructor]
        public DiscoveredDevice(string ip, string port, string name, string id, EDeviceType deviceType)
        {
            IP = ip;
            Port = port;
            Name = name;
            ID = id;
            DeviceType = deviceType;
        }
        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}