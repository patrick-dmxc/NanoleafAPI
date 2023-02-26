using Newtonsoft.Json;

namespace NanoleafAPI
{
    public class FirmwareUpgrade
    {
        [JsonProperty("firmwareAvailability")]
        public bool FirmwareAvailability { get; set; }

        [JsonProperty("newFirmwareVersion")]
        public string? NewFirmwareVersion { get; set; }

        public override string ToString()
        {
            return $"FirmwareAvailability: {FirmwareAvailability} NewFirmwareVersion: {NewFirmwareVersion}";
        }
    }
}