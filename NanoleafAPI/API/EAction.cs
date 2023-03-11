using System.Text.Json;
using System.Text.Json.Serialization;

namespace NanoleafAPI.API
{
    [JsonConverter(typeof(EActionConverter))]
    public enum EAction
    {
        UNKNOWN,
        Power,
        BrightnessUp,
        BrightnessDown

        ///ToDo Missing SystemActions
        //"ncs"
        //"nrs"
        //"nrdms"
        //"pcs"
        //"prs"
    }
    public class EActionConverter : JsonConverter<EAction>
    {

        public override bool CanConvert(Type objectType)
        {
            if (objectType == typeof(EAction))
                return true;
            if (objectType == typeof(string))
                return true;

            return false;
        }

        public override EAction Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var val = reader.GetString();
            switch (val)
            {
                case "pwr":
                    return EAction.Power;
                case "bu":
                    return EAction.BrightnessUp;
                case "bd":
                    return EAction.BrightnessDown;

                ///ToDo 
                case "ncs":
                    return EAction.UNKNOWN;
                case "nrs":
                    return EAction.UNKNOWN;
                case "nrdms":
                    return EAction.UNKNOWN;
                case "pcs":
                    return EAction.UNKNOWN;
                case "prs":
                    return EAction.UNKNOWN;


            }

            return EAction.UNKNOWN;
        }

        public override void Write(Utf8JsonWriter writer, EAction value, JsonSerializerOptions options)
        {
            string val = string.Empty;
            switch (value)
            {
                case EAction.Power:
                    val = "pwr";
                    break;
                case EAction.BrightnessUp:
                    val = "bu";
                    break;
                case EAction.BrightnessDown:
                    val = "bd";
                    break;

                ///ToDo 
                //case EAction.UNKNOWN:
                //    val = "ncs";
                //    break;
                //case EAction.UNKNOWN:
                //    val = "nrs";
                //    break;
                //case EAction.UNKNOWN:
                //    val = "nrdms";
                //    break;
                //case EAction.UNKNOWN:
                //    val = "pcs";
                //    break;
                //case EAction.UNKNOWN:
                //    val = "prs";
                //    break;
            }

            writer.WriteStringValue(val);
        }
    }
}