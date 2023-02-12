using Newtonsoft.Json;

namespace NanoleafAPI
{
    public class StateInfo
    {
        [JsonProperty("value")]
        public ushort Value { get; set; }

        [JsonProperty("min")]
        public int Min { get; set; }

        [JsonProperty("max")]
        public int Max { get; set; }
    }
}