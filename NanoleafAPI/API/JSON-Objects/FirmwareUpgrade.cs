using System.Text.Json.Serialization;

namespace NanoleafAPI
{
    public struct FirmwareUpgrade
    {
        [JsonPropertyName("firmwareAvailability")]
        public bool FirmwareAvailability { get; }

        [JsonPropertyName("newFirmwareVersion")]
        public string? NewFirmwareVersion { get; }

        [JsonConstructor]
        public FirmwareUpgrade(bool firmwareAvailability, string? newFirmwareVersion) => (FirmwareAvailability, NewFirmwareVersion) = (firmwareAvailability, newFirmwareVersion);

        public override string ToString()
        {
            return $"FirmwareAvailability: {FirmwareAvailability} NewFirmwareVersion: {NewFirmwareVersion}";
        }
    }
}