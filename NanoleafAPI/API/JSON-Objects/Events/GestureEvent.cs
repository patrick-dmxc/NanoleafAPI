using NanoleafAPI.API;
using System.Text.Json.Serialization;

namespace NanoleafAPI
{
    public readonly struct GestureEvent
    {
        [JsonPropertyName("gesture")]
        public readonly EGesture Gesture { get; }
        [JsonPropertyName("panelId")]
        public readonly int PanelID { get; }
        

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
