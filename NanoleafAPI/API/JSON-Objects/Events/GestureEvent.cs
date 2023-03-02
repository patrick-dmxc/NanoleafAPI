using System.Text.Json.Serialization;

namespace NanoleafAPI
{
    public struct GestureEvent
    {
        [JsonPropertyName("gesture")]
        public EGesture Gesture { get; }
        [JsonPropertyName("panelId")]
        public int PanelID { get; }
        public enum EGesture
        {
            SingleTap,
            DoubleTap,
            SwipeUp,
            SwipeDown,
            SwipeLeft,
            SwipeRight
        }

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
