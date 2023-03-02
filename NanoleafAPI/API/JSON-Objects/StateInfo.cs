using System.Text.Json.Serialization;

namespace NanoleafAPI
{
    public struct StateInfo
    {
        [JsonPropertyName("value")]
        public float Value { get; }

        [JsonPropertyName("min")]
        public float Min { get; }

        [JsonPropertyName("max")]
        public float Max { get; }

        [JsonConstructor]
        public StateInfo(float value, float min, float max) => (Value, Min, Max) = (value,min,max);

        public override string ToString()
        {
            return $"Value: {Value} Min: {Min} Max: {Max}";
        }
    }
}