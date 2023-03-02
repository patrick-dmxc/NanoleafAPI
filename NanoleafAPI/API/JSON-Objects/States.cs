using System.Text.Json.Serialization;

namespace NanoleafAPI
{
    public struct States
    {
        [JsonPropertyName("on")]
        public StateOnOff On { get; }
        [JsonPropertyName("brightness")]
        public StateInfo Brightness { get; }

        [JsonPropertyName("hue")]
        public StateInfo Hue { get; }

        [JsonPropertyName("sat")]
        public StateInfo Saturation { get; }

        [JsonPropertyName("ct")]
        public StateInfo ColorTemprature { get; }

        [JsonPropertyName("colorMode")]
        public string ColorMode { get; }

        [JsonConstructor]
        public States(StateOnOff on, StateInfo brightness, StateInfo hue, StateInfo saturation, StateInfo colorTemprature, string colorMode) => (On, Brightness, Hue, Saturation, ColorTemprature, ColorMode) = (on, brightness, hue, saturation, colorTemprature, colorMode);
    }
}