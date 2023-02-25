using Microsoft.Extensions.Logging;

namespace NanoleafAPI
{
    public static class Tools
    {
        public static ILoggerFactory LoggerFactory = new LoggerFactory();
        public static EDeviceType ModelStringToEnum(string? val)
        {
            EDeviceType type = EDeviceType.UNKNOWN;
            switch (val?.ToLower())
            {
                case "nl22":
                case "light":
                    type = EDeviceType.LightPanles;
                    break;
                case "nl29":
                    type = EDeviceType.Canvas;
                    break;
                case "nl42":
                    type = EDeviceType.Shapes;
                    break;
                case "nl45":
                    type = EDeviceType.Essentials;
                    break;
                case "nl52":
                    type = EDeviceType.Elements;
                    break;
                case "nl59":
                    type = EDeviceType.Lines;
                    break;
            }
            return type;
        }
    }
}
