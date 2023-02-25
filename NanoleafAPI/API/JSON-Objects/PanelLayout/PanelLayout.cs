using Newtonsoft.Json;

namespace NanoleafAPI
{
    public class PanelLayout
    {
#pragma warning disable CS8618
        [JsonProperty("globalOrientation")]
        public StateInfo GlobalOrientation { get; set; }

        [JsonProperty("layout")]

        public Layout Layout { get; set; }
#pragma warning restore CS8618
    }
}
