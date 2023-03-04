using System.Text.Json.Serialization;

namespace NanoleafAPI
{
    public readonly struct StateInfo
    {
        [JsonPropertyName("value")]
        public readonly float Value { get; }

        [JsonPropertyName("min")]
        public readonly float Min { get; }

        [JsonPropertyName("max")]
        public readonly float Max { get; }

        [JsonConstructor]
        public StateInfo(float value, float min, float max) => (Value, Min, Max) = (value,min,max);

        public override string ToString()
        {
            return $"Value: {Value} Min: {Min} Max: {Max}";
        }
    }
}