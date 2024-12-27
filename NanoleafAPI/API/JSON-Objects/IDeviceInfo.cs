using System.Text.Json.Serialization;

namespace NanoleafAPI
{
    public interface IDeviceInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; }

        [JsonPropertyName("serialNo")]
        public string SerialNumber { get; }

        [JsonPropertyName("manufacturer")]
        public string Manufacturer { get; }

        [JsonPropertyName("firmwareVersion")]
        public string FirmwareVersion { get; }

        [JsonPropertyName("hardwareVersion")]
        public string HardwareVersion { get; }

        [JsonPropertyName("model")]
        public string Model { get; }
    }
}
