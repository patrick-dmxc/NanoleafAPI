using System.Text.Json.Serialization;

namespace NanoleafAPI
{
    public readonly struct Length
    {
        [JsonPropertyName("numLEDs")]
        public readonly uint NumLEDs { get; }

        [JsonConstructor]
        public Length(uint numLEDs) => (NumLEDs) = (numLEDs);
        public override string ToString()
        {
            return $"NumLEDs: {NumLEDs}";
        }
    }
}
