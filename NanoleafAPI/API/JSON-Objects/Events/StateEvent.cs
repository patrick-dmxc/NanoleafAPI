using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace NanoleafAPI
{
    public struct StateEvent
    {
        [JsonPropertyName("attr")]
        public EAttribute Attribute { get; }
        [JsonPropertyName("value")]
        public object Value { get; private set; }
        public bool? On { get; } = null;
        public float? Brightness { get; } = null;
        public float? Hue { get; } = null;
        public float? Saturation { get; } = null;
        public float? CCT { get; } = null;
        public string? ColorMode { get; } = null;
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
        public StateEvent(EAttribute attribute, object value)
        {
            Attribute = attribute;
            
            if (value is JsonElement element)
            switch (Attribute)
            {
                case EAttribute.On:
                        Value = On =  element.Deserialize<bool>();
                        return;
                case EAttribute.Brightness:
                        Value = Brightness = element.Deserialize<float>();
                        return;
                case EAttribute.Hue:
                        Value = Hue = element.Deserialize<float>();
                        return;
                case EAttribute.Saturation:
                        Value = Saturation = element.Deserialize<float>();
                        return;
                case EAttribute.CCT:
                        Value = CCT = element.Deserialize<float>();
                        return;
                case EAttribute.ColorMode:
                        Value = ColorMode = element.Deserialize<string>()!;
                    return;
            }
            Value = string.Empty;
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