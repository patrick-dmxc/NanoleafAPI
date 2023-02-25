using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.RegularExpressions;

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

        public static bool IsIPValid([NotNullWhen(true)] string? ipaddress)
        {
            if (string.IsNullOrWhiteSpace(ipaddress))
                return false;

            IPAddress? ip;
            bool validateIP = IPAddress.TryParse(ipaddress, out ip);
            if (!validateIP)
                return false;

            return true;
        }
        public static bool IsPortValid([NotNullWhen(true)] string? port)
        {
            if (string.IsNullOrWhiteSpace(port))
                return false;


            try
            {
                Convert.ToInt16(port);
            }
            catch(Exception)
            {
                return false;
            }

            return true;
        }
        public static bool IsTokenValid([NotNullWhen(true)] string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;
            if (token.Length != 32)
                return false;
            if (!token.All(Char.IsLetterOrDigit))
                return false;

            return true;
        }
    }
}
