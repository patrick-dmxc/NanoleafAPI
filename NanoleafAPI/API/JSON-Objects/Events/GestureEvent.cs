using NanoleafAPI.API;
using System.Text.Json.Serialization;

namespace NanoleafAPI
{
    public readonly struct GestureEvent
    {
        [JsonPropertyName("gesture")]
        public EGesture Gesture { get; } = EGesture.UNKNOWN;
        [JsonPropertyName("panelId")]
        public int PanelID { get; } = -2;
        

        [JsonConstructor]
        public GestureEvent(EGesture gesture, int panelID) => (Gesture, PanelID) = (gesture, panelID);
        public override string ToString()
        {
            return $"PanelID: {PanelID} Gesture: {Gesture}";
        }
    }

    public struct GestureEvents
    {
        [JsonPropertyName("events")]
        public IReadOnlyList<GestureEvent> Events { get; }

        [JsonConstructor]
        public GestureEvents(IReadOnlyList<GestureEvent> events) => (Events) = (events);
    }
}
