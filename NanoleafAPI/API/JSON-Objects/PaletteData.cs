using System.Text.Json.Serialization;

namespace NanoleafAPI
{
    public readonly struct PaletteData
    {
        [JsonPropertyName("hue")]
        public readonly float Hue { get; }
        [JsonPropertyName("saturation")]
        public readonly float Saturation { get; }
        [JsonPropertyName("brightness")]
        public readonly float Brightness { get; }
        [JsonPropertyName("probability")]
        public readonly float? Probability { get; }

        [JsonConstructor]
        public PaletteData(float hue, float saturation, float brightness, float? probability) => (Hue, Saturation, Brightness, Probability) = (hue, saturation, brightness, probability);
    }
}