using System.Text.Json.Serialization;

namespace NanoleafAPI
{
    public struct StateOnOff
    {
        [JsonPropertyName("value")]
        public bool On { get; }
        [JsonConstructor]
        public StateOnOff(bool on) => (On) = (on);
        public override string ToString()
        {
            if (On)
                return "On";
            return "Off";
        }
    }
}
