using Newtonsoft.Json;

namespace NanoleafAPI
{
    public class States
    {
#pragma warning disable CS8618
        [JsonProperty("on")]
        public StateOnOff On { get; set; }
        [JsonProperty("brightness")]
        public StateInfo Brightness { get; set; }

        [JsonProperty("hue")]
        public StateInfo Hue { get; set; }

        [JsonProperty("sat")]
        public StateInfo Saturation { get; set; }

        [JsonProperty("ct")]
        public StateInfo ColorTemprature { get; set; }

        [JsonProperty("colorMode")]
        public string ColorMode { get; set; }
#pragma warning restore CS8618
    }
}