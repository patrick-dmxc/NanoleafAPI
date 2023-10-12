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
        [JsonPropertyName("schedules")]
        public readonly Schedules Schedules { get; }
        [JsonPropertyName("firmwareUpgrade")]
        public readonly FirmwareUpgrade FirmwareUpgrade { get; }
        [JsonPropertyName("discovery")]
        public readonly object Discovery { get; }
        [JsonPropertyName("qkihnokomhartlnp")]
        public readonly object Qkihnokomhartlnp { get; }

        [JsonConstructor]
        public AllPanelInfo(string name, string serialNumber, string manufacturer, string firmwareVersion, string hardwareVersion, string model, Effects effects, PanelLayout panelLayout, States state, Schedules schedules, FirmwareUpgrade firmwareUpgrade, object discovery, object qkihnokomhartlnp) => (Name, SerialNumber, Manufacturer, FirmwareVersion, HardwareVersion, Model, Effects, PanelLayout, State, Schedules, FirmwareUpgrade, Discovery, Qkihnokomhartlnp) = (name, serialNumber, manufacturer, firmwareVersion, hardwareVersion, model, effects, panelLayout, state, schedules, firmwareUpgrade, discovery, qkihnokomhartlnp);

        public override string ToString()
        {
            return $"Name: {Name} Model: {Model} SN: {SerialNumber} FW: {FirmwareVersion} HW: {HardwareVersion} Manufacturer: {Manufacturer}";
        }
    }
}