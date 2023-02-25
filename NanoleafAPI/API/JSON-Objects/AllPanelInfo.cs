﻿using Newtonsoft.Json;

namespace NanoleafAPI
{
    public class AllPanelInfo
    {
#pragma warning disable CS8618
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("serialNo")]
        public string SerialNumber { get; set; }

        [JsonProperty("manufacturer")]
        public string Manufacturer { get; set; }

        [JsonProperty("firmwareVersion")]
        public string FirmwareVersion { get; set; }

        [JsonProperty("hardwareVersion")]
        public string HardwareVersion { get; set; }

        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("effects")]
        public Effects Effects { get; set; }

        [JsonProperty("panelLayout")]
        public PanelLayout PanelLayout { get; set; }

        [JsonProperty("state")]
        public States State { get; set; }
#pragma warning restore CS8618

        public override string ToString()
        {
            return $"Name: {Name} Model: {Model} SN: {SerialNumber} FW: {FirmwareVersion} HW: {HardwareVersion} Manufacturer: {Manufacturer}";
        }
    }
}
