using Newtonsoft.Json;

namespace NanoleafAPI
{
    public class ExternalControlConnectionInfo
    {
        [JsonProperty("streamControlIpAddr")]
#pragma warning disable CS8618
        public string StreamIPAddress { get; set; }

        [JsonProperty("streamControlPort")]
        public int StreamPort { get; set; }

        [JsonProperty("streamControlProtocol")]
        public string StreamProtocol { get; set; }
#pragma warning restore CS8618
    }
}
