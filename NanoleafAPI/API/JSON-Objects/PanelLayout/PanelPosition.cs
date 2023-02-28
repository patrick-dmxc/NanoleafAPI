﻿using Newtonsoft.Json;

namespace NanoleafAPI
{
    public class PanelPosition
    {
        public enum EShapeType
        {
            Triangle = 0,
            Rhythm = 1,
            Square = 2,
            ControlSquarePrimary = 3,
            ContolSquarePassive = 4,
            PowerSupply = 5,
            Hexagon_Shapes = 7,
            Triangle_Shapes = 8,
            MiniTriangle_Shapes = 9,
            ShapesController = 12,
            ElementsHexagons = 14,
            ElementsHexagonsCorner = 15,
            LinesConnector = 16,
            LightLines = 17,
            LightLinesSingleZone = 18,
            ControllerCap = 19,
            PowerConnector = 20
        }
        [JsonProperty("panelId")]
        public int PanelId { get; set; }

        [JsonProperty("x")]
        public int X { get; set; }

        [JsonProperty("Y")]
        public int Y { get; set; }

        [JsonProperty("o")]
        public int Orientation { get; set; }

        [JsonProperty("shapeType")]
        public EShapeType ShapeType { get; set; }

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
                    case EShapeType.Triangle:
                        return 150;
                    case EShapeType.Rhythm:
                        return 0;
                    case EShapeType.Square:
                    case EShapeType.ContolSquarePassive:
                    case EShapeType.ControlSquarePrimary:
                        return 100;
                    case EShapeType.Hexagon_Shapes:
                    case EShapeType.ShapesController:
                    case EShapeType.MiniTriangle_Shapes:
                        return 67;
                    case EShapeType.Triangle_Shapes:
                    case EShapeType.ElementsHexagons:
                        return 134;
                    case EShapeType.ElementsHexagonsCorner:
                        return 33.5; //58
                    case EShapeType.LightLines:
                        return 154;
                    case EShapeType.LightLinesSingleZone:
                        return 77;
                    case EShapeType.LinesConnector:
                    case EShapeType.ControllerCap:
                    case EShapeType.PowerConnector:
                        return 11;
                }
                return 0;
            }
        }
    }
}
