using System.Text.Json.Serialization;

namespace NanoleafAPI
{
    public readonly struct States
    {
        [JsonPropertyName("on")]
        public readonly StateOnOff On { get; }
        [JsonPropertyName("brightness")]
        public readonly StateInfo Brightness { get; }

        [JsonPropertyName("hue")]
        public readonly StateInfo Hue { get; }

        [JsonPropertyName("sat")]
        public readonly StateInfo Saturation { get; }

        [JsonPropertyName("ct")]
        public readonly StateInfo ColorTemprature { get; }

        [JsonPropertyName("colorMode")]
        public readonly string ColorMode { get; }

        [JsonConstructor]
        public States(StateOnOff on, StateInfo brightness, StateInfo hue, StateInfo saturation, StateInfo colorTemprature, string colorMode) => (On, Brightness, Hue, Saturation, ColorTemprature, ColorMode) = (on, brightness, hue, saturation, colorTemprature, colorMode);
    }
}