using System.Text.Json.Serialization;

namespace NanoleafAPI
{
    public struct Layout
    {
        [JsonPropertyName("numPanels")]
        public uint NumberOfPanels { get; }

        [JsonPropertyName("positionData")]
        public IReadOnlyList<PanelPosition> PanelPositions { get; }

        [JsonConstructor]
        public Layout(uint numberOfPanels, IReadOnlyList<PanelPosition> panelPositions) => (NumberOfPanels, PanelPositions) = (numberOfPanels, panelPositions);

        public override string ToString()
        {
            return $"Layout of {NumberOfPanels} Panels";
        }
    }
}