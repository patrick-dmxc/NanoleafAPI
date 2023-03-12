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
        BrightnessDown,
        NextColorScene,
        NextRythmScene,
        NextRandomScene,
        PreviousColorScenee,
        PreviousRythmScene
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
                case "ncs":
                    return EAction.NextColorScene;
                case "nrs":
                    return EAction.NextRythmScene;
                case "nrdms":
                    return EAction.NextRandomScene;
                case "pcs":
                    return EAction.PreviousColorScenee;
                case "prs":
                    return EAction.PreviousRythmScene;
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
                case EAction.NextColorScene:
                    val = "ncs";
                    break;
                case EAction.NextRythmScene:
                    val = "nrs";
                    break;
                case EAction.NextRandomScene:
                    val = "nrdms";
                    break;
                case EAction.PreviousColorScenee:
                    val = "pcs";
                    break;
                case EAction.PreviousRythmScene:
                    val = "prs";
                    break;
            }

            writer.WriteStringValue(val);
        }
    }
}