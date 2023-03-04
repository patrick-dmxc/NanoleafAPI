using System.Text.Json;
using System.Text.Json.Serialization;

namespace NanoleafAPI
{
    public readonly struct StateEvent
    {
        [JsonPropertyName("attr")]
        public readonly EAttribute Attribute { get; }
        [JsonPropertyName("value")]
        public readonly JsonElement Value { get; }
        public readonly bool? On { get; } = null;
        public readonly float? Brightness { get; } = null;
        public readonly float? Hue { get; } = null;
        public readonly float? Saturation { get; } = null;
        public readonly float? CCT { get; } = null;
        public readonly string? ColorMode { get; } = null;
        public enum EAttribute
        {
            UNKNOWN,
            On,
            Brightness,
            Hue,
            Saturation,
            CCT,
            ColorMode
        }

        [JsonConstructor]
        public StateEvent(EAttribute attribute, JsonElement value)
        {
            Attribute = attribute;
            Value = value;
            switch (Attribute)
            {
                case EAttribute.On:
                    On = value.Deserialize<bool>();
                    break;
                case EAttribute.Brightness:
                    Brightness = value.Deserialize<float>();
                    break;
                case EAttribute.Hue:
                    Hue = value.Deserialize<float>();
                    break;
                case EAttribute.Saturation:
                    Saturation = value.Deserialize<float>();
                    break;
                case EAttribute.CCT:
                    CCT = value.Deserialize<float>();
                    break;
                case EAttribute.ColorMode:
                    ColorMode = value.Deserialize<string>();
                    break;
            }
        }

        public override string ToString()
        {
            return $"State: {Attribute}: {Value}";
        }
    }

    public struct StateEvents
    {
        [JsonPropertyName("events")]
        public IReadOnlyList<StateEvent> Events { get; }

        [JsonConstructor]
        public StateEvents(IReadOnlyList<StateEvent> events) => (Events) = (events);
    }
}