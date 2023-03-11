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

        [JsonConstructor]
        public Config(TouchConfig? touchConfig, bool? touchKillSwitchOn) => (TouchConfig, TouchKillSwitchOn) = (touchConfig, touchKillSwitchOn);
        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}