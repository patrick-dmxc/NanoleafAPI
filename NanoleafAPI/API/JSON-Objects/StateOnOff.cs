using System.Text.Json.Serialization;

namespace NanoleafAPI
{
    public readonly struct StateOnOff
    {
        [JsonPropertyName("value")]
        public readonly bool On { get; }
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
