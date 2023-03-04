using System.Text.Json.Serialization;

namespace NanoleafAPI
{
    public readonly struct PanelLayout
    {
        [JsonPropertyName("globalOrientation")]
        public readonly StateInfo GlobalOrientation { get; }

        [JsonPropertyName("layout")]
        public readonly Layout Layout { get; }

        [JsonConstructor]
        public PanelLayout(StateInfo globalOrientation, Layout layout) => (GlobalOrientation, Layout) = (globalOrientation, layout);
    }
}