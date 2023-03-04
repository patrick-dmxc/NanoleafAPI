using System.Text.Json.Serialization;

namespace NanoleafAPI
{
    public readonly struct ExternalControlConnectionInfo
    {
        [JsonPropertyName("streamControlIpAddr")]
        public readonly string StreamIPAddress { get;}

        [JsonPropertyName("streamControlPort")]
        public readonly int StreamPort { get; }

        [JsonPropertyName("streamControlProtocol")]
        public readonly string StreamProtocol { get; }

        [JsonConstructor]
        public ExternalControlConnectionInfo(string streamIPAddress, int streamPort, string streamProtocol) => (StreamIPAddress, StreamPort, StreamProtocol) = (streamIPAddress, streamPort, streamProtocol);
    }
}
