using System.Text.Json.Serialization;

namespace NanoleafAPI
{
    public readonly struct FirmwareUpgrade
    {
        [JsonPropertyName("firmwareAvailability")]
        public readonly bool FirmwareAvailability { get; }

        [JsonPropertyName("newFirmwareVersion")]
        public readonly string? NewFirmwareVersion { get; }

        [JsonConstructor]
        public FirmwareUpgrade(bool firmwareAvailability, string? newFirmwareVersion) => (FirmwareAvailability, NewFirmwareVersion) = (firmwareAvailability, newFirmwareVersion);

        public override string ToString()
        {
            return $"FirmwareAvailability: {FirmwareAvailability} NewFirmwareVersion: {NewFirmwareVersion}";
        }
    }
}