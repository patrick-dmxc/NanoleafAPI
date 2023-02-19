using Newtonsoft.Json;

namespace NanoleafAPI
{
    public class StateOnOff
    {
        [JsonProperty("value")]
        public bool On { get; set; }
        public override string ToString()
        {
            if (On)
                return "On";
            return "Off";
        }
    }
}
