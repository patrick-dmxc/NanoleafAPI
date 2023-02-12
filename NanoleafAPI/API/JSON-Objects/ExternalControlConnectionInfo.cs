using Newtonsoft.Json;

namespace NanoleafAPI
{
    public class ExternalControlConnectionInfo
    {
        [JsonProperty("streamControlIpAddr")]
        public string StreamIPAddress { get; set; }

        [JsonProperty("streamControlPort")]
        public int StreamPort { get; set; }

        [JsonProperty("streamControlProtocol")]
        public string StreamProtocol { get; set; }
    }
}
