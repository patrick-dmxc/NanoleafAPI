using System.Text.Json.Serialization;

namespace NanoleafAPI
{
    public readonly partial struct PanelPosition
    {
        [JsonPropertyName("panelId")]
        public readonly int PanelId { get; }

        [JsonPropertyName("x")]
        public readonly float X { get; }

        [JsonPropertyName("y")]
        public readonly float Y { get; }

        [JsonPropertyName("o")]
        public readonly float Orientation { get; }

        [JsonPropertyName("shapeType")]
        public readonly EPanelType ShapeType { get; }

        [JsonConstructor]
        public PanelPosition(int panelId, float x, float y, float orientation, EPanelType shapeType) => (PanelId, X, Y, Orientation, ShapeType) = (panelId, x, y, orientation, shapeType);

        public override string ToString()
        {
            return $"PanelID: {PanelId} X: {X} Y: {Y} Orentation: {Orientation} Shape: {ShapeType}";
        }

        public double SideLength
        {
            get
            {
                switch (this.ShapeType)
                {
                    case EPanelType.Triangle:
                        return 150;
                    case EPanelType.Rhythm:
                        return 0;
                    case EPanelType.Square:
                    case EPanelType.ContolSquarePassive:
                    case EPanelType.ControlSquarePrimary:
                        return 100;
                    case EPanelType.Hexagon_Shapes:
                    case EPanelType.ShapesController:
                    case EPanelType.MiniTriangle_Shapes:
                        return 67;
                    case EPanelType.Triangle_Shapes:
                    case EPanelType.ElementsHexagons:
                        return 134;
                    case EPanelType.ElementsHexagonsCorner:
                        return 33.5; //58
                    case EPanelType.LightLines:
                        return 154;
                    case EPanelType.LightLinesSingleZone:
                        return 77;
                    case EPanelType.LinesConnector:
                    case EPanelType.ControllerCap:
                    case EPanelType.PowerConnector:
                        return 11;
                }
                return 0;
            }
        }
    }
}