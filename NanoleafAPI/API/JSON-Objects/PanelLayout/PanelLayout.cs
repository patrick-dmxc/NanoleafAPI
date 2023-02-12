using Newtonsoft.Json;

namespace NanoleafAPI
{
    public class PanelLayout
    {
        [JsonProperty("globalOrientation")]
        public StateInfo GlobalOrientation { get; set; }

        [JsonProperty("layout")]
        public Layout Layout { get; set; }
    }
}
