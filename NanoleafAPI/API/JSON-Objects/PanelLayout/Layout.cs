using Newtonsoft.Json;

namespace NanoleafAPI
{
    public class Layout
    {
        [JsonProperty("numPanels")]
        public uint NumberOfPanels { get; set; }

        //[JsonProperty("sideLength")]
        //public int SideLength { get; set; }

        [JsonProperty("positionData")]
        public IEnumerable<PanelPosition> PanelPositions { get; set; }

        public override string ToString()
        {
            return $"Layout of {NumberOfPanels} Panels";
        }
    }
}
