using System.Text.Json.Serialization;

namespace NanoleafAPI
{
    public readonly struct RythmPosition
    {

        [JsonPropertyName("x")]
        public readonly float X { get; }

        [JsonPropertyName("Y")]
        public readonly float Y { get; }

        [JsonPropertyName("o")]
        public readonly float Orientation { get; }

        [JsonConstructor]
        public RythmPosition(float x, float y, float orientation) => (X, Y, Orientation) = (x, y, orientation);

        public override string ToString()
        {
            return $"X: {X} Y: {Y} Orentation: {Orientation}";
        }
    }
}