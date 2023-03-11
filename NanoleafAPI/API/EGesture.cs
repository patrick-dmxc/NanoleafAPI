using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NanoleafAPI.API
{
    [JsonConverter(typeof(EGestureConverter))]
    public enum EGesture
    {
        UNKNOWN,
        SingleTap,
        DoubleTap,
        SwipeUp,
        SwipeDown,
        SwipeLeft,
        SwipeRight
    }
    public class EGestureConverter : JsonConverter<EGesture>
    {

        public override bool CanConvert(Type objectType)
        {
            if (objectType == typeof(EGesture))
                return true;
            if (objectType == typeof(string))
                return true;

            return false;
        }

        public override EGesture Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var val = reader.GetString();
            switch (val)
            {
                case "st":
                    return EGesture.SingleTap;
                case "dt":
                    return EGesture.DoubleTap;

                case "su":
                    return EGesture.SwipeUp;
                case "sd":
                    return EGesture.SwipeDown;
                case "sl":
                    return EGesture.SwipeLeft;
                case "sr":
                    return EGesture.SwipeRight;

            }

            return EGesture.UNKNOWN;
        }

        public override void Write(Utf8JsonWriter writer, EGesture value, JsonSerializerOptions options)
        {
            string val = string.Empty;
            switch (value)
            {
                case EGesture.SingleTap:
                    val = "st";
                    break;
                case EGesture.DoubleTap:
                    val = "dt";
                    break;

                case EGesture.SwipeUp:
                    val = "su";
                    break;
                case EGesture.SwipeDown:
                    val = "sd";
                    break;
                case EGesture.SwipeLeft:
                    val = "sl";
                    break;
                case EGesture.SwipeRight:
                    val = "sr";
                    break;
            }

            writer.WriteStringValue(val);
        }
    }
}