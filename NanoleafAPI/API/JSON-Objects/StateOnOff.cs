using Newtonsoft.Json;

namespace NanoleafAPI
{
    public class StateOnOff
    {
        [JsonProperty("value")]
        public bool On { get; set; }
    }
}
