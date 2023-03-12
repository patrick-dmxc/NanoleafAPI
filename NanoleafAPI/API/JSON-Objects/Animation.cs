using System.Text.Json.Serialization;

namespace NanoleafAPI
{
    public struct Animation
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("loop")]
        public readonly bool Loop { get; } = false;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("version")]
        public readonly string Version { get; } = "2.0";

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("animName")]
        public readonly string Name { get; } = string.Empty;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("animType")]
        public readonly string Type { get; } = string.Empty;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("animData")]
        public readonly string? Data { get; } = null;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("colorType")]
        public readonly string ColorType { get; } = string.Empty;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("palette")]
        public readonly IReadOnlyList<PaletteData>? Palette { get; } = null;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("brightnessRange")]
        public readonly Range? BrightnessRange { get; } = null;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("transTime")]
        public readonly Range? TransitionTime { get; } = null;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("delayTime")]
        public readonly Range? DelayTime { get; } = null;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("extControlVersion")]
        public readonly string? ExternalControlVersion { get; } = null;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("clientIpAddress")]
        public readonly string? ClientIpAddress { get; } = null;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("clientUdpPort")]
        public readonly string? ClientUdpPort { get; } = null;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("logicalPanelsEnabled")]
        public readonly bool? LogicalPanelsEnabled { get; } = null;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("hasOverlay")]
        public readonly bool? HasOverlay { get; } = null;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("pluginType")]
        public readonly string? PluginType { get; } = null;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("pluginUuid")]
        public readonly string? PluginUuid { get; } = null;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("pluginOptions")]
        public readonly IReadOnlyList<PluginOption>? PluginOptions { get; } = null;

        [JsonConstructor]
        public Animation(
            bool loop,
            string version,
            string name,
            string type,
            string? data,
            string colorType,
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
            ColorType,
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
            colorType,
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

        public Animation(string name, string pluginUuid, string colorType = "HSB", string animType = "plugin", string pluginType = "rhythm", string version = "2.0", IReadOnlyList<PaletteData>? palette = null, IReadOnlyList<PluginOption>? pluginOptions = null)
        {
            this.Name = name;
            this.PluginUuid = pluginUuid;
            this.ColorType = colorType;
            this.Type = animType;
            this.Version = version;
            this.Palette = palette;
            this.PluginOptions = pluginOptions;
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
    }
}