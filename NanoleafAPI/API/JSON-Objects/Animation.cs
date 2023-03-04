using System.Text.Json;
using System.Text.Json.Serialization;

namespace NanoleafAPI
{
    public readonly struct Animation
    {
        [JsonPropertyName("loop")]
        public readonly bool? Loop { get; }
        [JsonPropertyName("version")]
        public readonly string? Version { get; }
        [JsonPropertyName("animName")]
        public readonly string? Name { get; }
        [JsonPropertyName("animType")]
        public readonly string? Type { get; }
        [JsonPropertyName("animData")]
        public readonly string? Data { get; }

        [JsonPropertyName("palette")]
        public readonly IReadOnlyList<PaletteData>? Palette { get; }
        [JsonPropertyName("brightnessRange")]
        public readonly Range? BrightnessRange { get; }
        [JsonPropertyName("transTime")]
        public readonly Range? TransitionTime { get; }
        [JsonPropertyName("delayTime")]
        public readonly Range? DelayTime { get; }

        [JsonPropertyName("extControlVersion")]
        public readonly string? ExternalControlVersion { get; }
        [JsonPropertyName("clientIpAddress")]
        public readonly string? ClientIpAddress { get; }
        [JsonPropertyName("clientUdpPort")]
        public readonly string? ClientUdpPort { get; }
        [JsonPropertyName("logicalPanelsEnabled")]
        public readonly bool? LogicalPanelsEnabled { get; }
        [JsonPropertyName("hasOverlay")]
        public readonly bool? HasOverlay { get; }
        [JsonPropertyName("pluginType")]
        public readonly string? PluginType { get; }
        [JsonPropertyName("pluginUuid")]
        public readonly string? PluginUuid { get; }
        [JsonPropertyName("pluginOptions")]
        public readonly IReadOnlyList<PluginOption>? PluginOptions { get; }

        [JsonConstructor]
        public Animation(
            bool? loop,
            string? version,
            string? name,
            string? type,
            string? data,
            IReadOnlyList<PaletteData>? palette,
            Range? brightnessRange,
            Range? transitionTime,
            Range? delayTime,
            string? externalControlVersion,
            string? clientIpAddress,
            string? clientUdpPort,
            bool? logicalPanelsEnabled,
            bool? hasOverlay,
            string? pluginType,
            string? pluginUuid,
            IReadOnlyList<PluginOption>? pluginOptions) => (
            Loop,
            Version,
            Name,
            Type,
            Data,
            Palette,
            BrightnessRange,
            TransitionTime,
            DelayTime,
            ExternalControlVersion,
            ClientIpAddress,
            ClientUdpPort,
            LogicalPanelsEnabled,
            HasOverlay,
            PluginType,
            PluginUuid,
            PluginOptions) = (
            loop,
            version,
            name,
            type,
            data,
            palette,
            brightnessRange,
            transitionTime,
            delayTime,
            externalControlVersion,
            clientIpAddress,
            clientUdpPort,
            logicalPanelsEnabled,
            hasOverlay,
            pluginType,
            pluginUuid,
            pluginOptions);
        public readonly struct PaletteData
        {
            [JsonPropertyName("hue")]
            public readonly float Hue { get; }
            [JsonPropertyName("saturation")]
            public readonly float Saturation { get; }
            [JsonPropertyName("brightness")]
            public readonly float Brightness { get; }
            [JsonPropertyName("probability")]
            public readonly float? Probability { get; }

            [JsonConstructor]
            public PaletteData(float hue, float saturation, float brightness, float? probability) => (Hue, Saturation, Brightness, Probability) = (hue, saturation, brightness, probability);
        }
        public readonly struct Range
        {
            [JsonPropertyName("hue")]
            public readonly float Hue { get; }
            [JsonPropertyName("saturation")]
            public readonly float Saturation { get; }
            [JsonPropertyName("brightness")]
            public readonly float Brightness { get; }
            [JsonPropertyName("probability")]
            public readonly float Probability { get; }

            [JsonConstructor]
            public Range(float hue, float saturation, float brightness, float probability) => (Hue, Saturation, Brightness, Probability) = (hue, saturation, brightness, probability);
        }
        public readonly struct PluginOption
        {
            [JsonPropertyName("name")]
            public readonly string Name { get; }
            [JsonPropertyName("value")]
            public readonly object Value { get; }

            public readonly bool? Bool { get; } = null;
            public readonly double? Number { get; } = null;
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
}