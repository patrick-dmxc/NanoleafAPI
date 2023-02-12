using System.Collections.Generic;
using Newtonsoft.Json;

namespace NanoleafAPI
{
    public class GestureEvent
    {
        [JsonProperty("gesture")]
        public EGesture Gesture { get; set; }
        [JsonProperty("panelId")]
        public int PanelID { get; set; }
        public enum EGesture
        {
            SingleTap,
            DoubleTap,
            SwipeUp,
            SwipeDown,
            SwipeLeft,
            SwipeRight
        }
        public override string ToString()
        {
            return $"PanelID: {PanelID} Gesture: {Gesture}";
        }
    }
    public class GestureEvents
    {
        [JsonProperty("events")]
        public IEnumerable<GestureEvent> Events { get; set; }
    }
}
