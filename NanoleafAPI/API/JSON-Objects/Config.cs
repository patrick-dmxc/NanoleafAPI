using System.Text.Json;
using System.Text.Json.Serialization;

namespace NanoleafAPI
{
    public readonly struct Config
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("touchConfig")]
        public readonly TouchConfig? TouchConfig { get; } = null;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("touchKillSwitchOn")]
        public readonly bool? TouchKillSwitchOn { get; } = null;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("brightnessSensorConfig")]
        public readonly BrightnessSensorConfig? BrightnessSensorConfig { get; } = null;

        [JsonConstructor]
        public Config(TouchConfig? touchConfig, bool? touchKillSwitchOn, BrightnessSensorConfig? brightnessSensorConfig) => (TouchConfig, TouchKillSwitchOn, BrightnessSensorConfig) = (touchConfig, touchKillSwitchOn, brightnessSensorConfig);
        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}