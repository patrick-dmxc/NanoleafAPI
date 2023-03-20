using System.Text.Json.Serialization;

namespace NanoleafAPI
{
    public readonly struct EffectEvent
    {
        [JsonPropertyName("attr")]
        public int Attribute { get; }
        [JsonPropertyName("value")]
        public string? Value { get; }

        [JsonConstructor]
        public EffectEvent(int attribute, string? value) => (Attribute, Value) = (attribute, value);
        public override string ToString()
        {
            return $"Effect: {Value}";
        }
    }

    public struct EffectEvents
    {
        [JsonPropertyName("events")]
        public IReadOnlyList<EffectEvent> Events { get; }

        [JsonConstructor]
        public EffectEvents(IReadOnlyList<EffectEvent> events) => (Events) = (events);
    }
}