using Newtonsoft.Json;

namespace NanoleafAPI
{
    public class Effects
    {
        [JsonProperty("effectsList")]
        public IEnumerable<string> List { get; set; }

        [JsonProperty("select")]
        public string Selected { get; set; }
    }
}
