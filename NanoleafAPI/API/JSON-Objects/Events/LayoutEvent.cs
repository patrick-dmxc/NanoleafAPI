using System.Text.Json;
using System.Text.Json.Serialization;

namespace NanoleafAPI
{
    public readonly struct LayoutEvent
    {
        [JsonPropertyName("attr")]
        public readonly EAttribute Attribute { get; }

        [JsonPropertyName("value")]
        public readonly JsonElement Value { get; }

        public readonly Layout? Layout { get; } = null;

        public readonly float? GlobalOrientation { get; } = null;

        public enum EAttribute
        {
            UNKNOWN,
            Layout,
            GlobalOrientation
        }

        [JsonConstructor]
        public LayoutEvent(EAttribute attribute, JsonElement value)
        {
            Attribute = attribute;
            Value = value;
            switch (Attribute)
            {
                case EAttribute.Layout:
                    Layout = value.Deserialize<Layout>();
                    break;
                case EAttribute.GlobalOrientation:
                    GlobalOrientation = value.Deserialize<float>();
                    break;
            }
        }
    }

    public struct LayoutEvents
    {
        [JsonPropertyName("events")]
        public IReadOnlyList<LayoutEvent> Events { get; }

        [JsonConstructor]
        public LayoutEvents(IReadOnlyList<LayoutEvent> events) => (Events) = (events);
    }
}