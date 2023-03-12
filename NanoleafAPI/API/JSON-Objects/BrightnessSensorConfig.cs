using System.Text.Json;
using System.Text.Json.Serialization;

namespace NanoleafAPI
{
    public readonly struct BrightnessSensorConfig
    {
        [JsonPropertyName("enabled")]
        public readonly bool Enabled { get; }

        [JsonPropertyName("brightnessSensorMode")]
        public readonly byte BrightnessSensorMode { get; }

        [JsonPropertyName("userMaxBrightness")]
        public readonly float UserMaxBrightness { get; }

        [JsonPropertyName("userMinBrightness")]
        public readonly float UserMinBrightness { get; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("isCalibrated")]
        public readonly bool? IsCalibrated { get; } = null;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("isCalibrating")]
        public readonly bool? IsCalibrating { get; } = null;
        [JsonConstructor]
        public BrightnessSensorConfig(
            bool enabled,
            byte brightnessSensorMode,
            float userMaxBrightness,
            float userMinBrightness,
            bool? isCalibrated,
            bool? isCalibrating
            ) => (
            Enabled,
            BrightnessSensorMode,
            UserMaxBrightness,
            UserMinBrightness,
            IsCalibrated,
            IsCalibrating
            ) = (
            enabled,
            brightnessSensorMode,
            userMaxBrightness,
            userMinBrightness,
            isCalibrated,
            isCalibrating);
        public BrightnessSensorConfig(
            bool enabled,
            byte brightnessSensorMode,
            float userMaxBrightness,
            float userMinBrightness
            ) => (
            Enabled,
            BrightnessSensorMode,
            UserMaxBrightness,
            UserMinBrightness
            ) = (
            enabled,
            brightnessSensorMode,
            userMaxBrightness,
            userMinBrightness);
        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}