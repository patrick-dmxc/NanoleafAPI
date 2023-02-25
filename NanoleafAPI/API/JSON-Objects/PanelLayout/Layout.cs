using Newtonsoft.Json;

namespace NanoleafAPI
{
    public class Layout
    {
#pragma warning disable CS8618
        [JsonProperty("numPanels")]
        public uint NumberOfPanels { get; set; }

        [JsonProperty("positionData")]
        public IEnumerable<PanelPosition> PanelPositions { get; set; }
#pragma warning restore CS8618

        public override string ToString()
        {
            return $"Layout of {NumberOfPanels} Panels";
        }
    }
}
