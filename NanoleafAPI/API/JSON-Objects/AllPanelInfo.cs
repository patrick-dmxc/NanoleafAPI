using System.Text.Json.Serialization;

namespace NanoleafAPI
{
    public readonly struct AllPanelInfo
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

        [JsonPropertyName("effects")]
        public readonly Effects Effects { get; }

        [JsonPropertyName("panelLayout")]
        public readonly PanelLayout PanelLayout { get; }

        [JsonPropertyName("state")]
        public readonly States State { get; }

        [JsonConstructor]
        public AllPanelInfo(string name, string serialNumber, string manufacturer, string firmwareVersion, string hardwareVersion, string model, Effects effects, PanelLayout panelLayout, States state) => (Name, SerialNumber, Manufacturer, FirmwareVersion, HardwareVersion, Model, Effects, PanelLayout, State) = (name, serialNumber, manufacturer, firmwareVersion, hardwareVersion, model, effects, panelLayout, state);

        public override string ToString()
        {
            return $"Name: {Name} Model: {Model} SN: {SerialNumber} FW: {FirmwareVersion} HW: {HardwareVersion} Manufacturer: {Manufacturer}";
        }
    }
}