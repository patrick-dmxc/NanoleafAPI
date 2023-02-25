using Newtonsoft.Json;

namespace NanoleafAPI
{
    public class Effects
    {
#pragma warning disable CS8618
        [JsonProperty("effectsList")]
        public IEnumerable<string> List { get; set; }

        [JsonProperty("select")]
        public string Selected { get; set; }
#pragma warning restore CS8618
    }
}
