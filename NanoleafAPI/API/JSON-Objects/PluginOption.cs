using System.Text.Json;
using System.Text.Json.Serialization;

namespace NanoleafAPI
{
    public readonly struct PluginOption
    {
        [JsonPropertyName("name")]
        public readonly string Name { get; }
        [JsonPropertyName("value")]
        public readonly object Value { get; }

        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public readonly bool? Bool { get; } = null;
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public readonly double? Number { get; } = null;
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public readonly string? String { get; } = null;

        [JsonConstructor]
        public PluginOption(string name, object value)
        {
            Name = name;
            Value = value;
            if (value is JsonElement json)
                switch (json.ValueKind)
                {
                    case JsonValueKind.Number:
                        double number;
                        json.TryGetDouble(out number);
                        Number = number;
                        break;

                    case JsonValueKind.False:
                    case JsonValueKind.True:
                        Bool = json.GetBoolean();
                        break;

                    case JsonValueKind.String:
                        String = json.GetString();
                        break;
                }
        }
    }
}