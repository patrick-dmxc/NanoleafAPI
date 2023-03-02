using System.Text.Json.Serialization;

namespace NanoleafAPI
{
    public struct AllPanelInfo
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

        [JsonPropertyName("effects")]
        public Effects Effects { get; }

        [JsonPropertyName("panelLayout")]
        public PanelLayout PanelLayout { get; }

        [JsonPropertyName("state")]
        public States State { get; }

        [JsonConstructor]
        public AllPanelInfo(string name, string serialNumber, string manufacturer, string firmwareVersion, string hardwareVersion, string model, Effects effects, PanelLayout panelLayout, States state) => (Name, SerialNumber, Manufacturer, FirmwareVersion, HardwareVersion, Model, Effects, PanelLayout, State) = (name, serialNumber, manufacturer, firmwareVersion, hardwareVersion, model, effects, panelLayout, state);

        public override string ToString()
        {
            return $"Name: {Name} Model: {Model} SN: {SerialNumber} FW: {FirmwareVersion} HW: {HardwareVersion} Manufacturer: {Manufacturer}";
        }
    }
}