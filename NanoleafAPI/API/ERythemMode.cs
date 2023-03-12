using System.Text.Json;
using System.Text.Json.Serialization;

namespace NanoleafAPI.API
{
    [JsonConverter(typeof(EGestureConverter))]
    public enum ERythemMode: byte
    {
        Microphone=0,
        AuxCable=1
    }
    public class ERythemModeConverter : JsonConverter<ERythemMode>
    {

        public override bool CanConvert(Type objectType)
        {
            if (objectType == typeof(ERythemMode))
                return true;
            if (objectType == typeof(byte))
                return true;
            if (objectType == typeof(int))
                return true;

            return false;
        }

        public override ERythemMode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var val = reader.GetByte();
            return (ERythemMode)val;
        }

        public override void Write(Utf8JsonWriter writer, ERythemMode value, JsonSerializerOptions options)
        {
            byte val = 0;
            switch (value)
            {
                case ERythemMode.Microphone:
                    val = 0;
                    break;
                case ERythemMode.AuxCable:
                    val = 1;
                    break;
            }

            writer.WriteNumberValue(val);
        }
    }
}