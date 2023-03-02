using System.Text.Json.Serialization;

namespace NanoleafAPI
{
    public struct ExternalControlConnectionInfo
    {
        [JsonPropertyName("streamControlIpAddr")]
        public string StreamIPAddress { get;}

        [JsonPropertyName("streamControlPort")]
        public int StreamPort { get; }

        [JsonPropertyName("streamControlProtocol")]
        public string StreamProtocol { get; }

        [JsonConstructor]
        public ExternalControlConnectionInfo(string streamIPAddress, int streamPort, string streamProtocol) => (StreamIPAddress, StreamPort, StreamProtocol) = (streamIPAddress, streamPort, streamProtocol);
    }
}
