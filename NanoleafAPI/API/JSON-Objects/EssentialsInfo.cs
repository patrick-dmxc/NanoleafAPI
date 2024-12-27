using System.Text.Json.Serialization;

namespace NanoleafAPI
{
    public readonly struct EssentialsInfo : IDeviceInfo
    {
        [JsonPropertyName("name")]
        public readonly string Name { get; }

        [JsonPropertyName("serialNo")]
        public readonly string SerialNumber { get; }

        [JsonPropertyName("manufacturer")]
        public readonly string Manufacturer { get; }

        [JsonPropertyName("firmwareVersion")]
        public readonly string FirmwareVersion { get; }

        [JsonPropertyName("hardwareVersion")]
        public readonly string HardwareVersion { get; }

        [JsonPropertyName("model")]
        public readonly string Model { get; }

        [JsonConstructor]
        public EssentialsInfo(string name, string serialNumber, string manufacturer, string firmwareVersion, string hardwareVersion, string model) => (Name, SerialNumber, Manufacturer, FirmwareVersion, HardwareVersion, Model) = (name, serialNumber, manufacturer, firmwareVersion, hardwareVersion, model);

        public override string ToString()
        {
            return $"Name: {Name} Model: {Model} SN: {SerialNumber} FW: {FirmwareVersion} HW: {HardwareVersion} Manufacturer: {Manufacturer}";
        }
    }
}