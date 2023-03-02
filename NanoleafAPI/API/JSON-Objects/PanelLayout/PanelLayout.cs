using System.Text.Json.Serialization;

namespace NanoleafAPI
{
    public struct PanelLayout
    {
        [JsonPropertyName("globalOrientation")]
        public StateInfo GlobalOrientation { get; }

        [JsonPropertyName("layout")]
        public Layout Layout { get; }

        [JsonConstructor]
        public PanelLayout(StateInfo globalOrientation, Layout layout) => (GlobalOrientation, Layout) = (globalOrientation, layout);
    }
}