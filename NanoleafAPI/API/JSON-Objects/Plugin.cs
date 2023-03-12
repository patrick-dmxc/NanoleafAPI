using System.Text.Json;
using System.Text.Json.Serialization;

namespace NanoleafAPI
{
    public readonly struct Plugin
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("uuid")]
        public readonly string UUID { get; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("name")]
        public readonly string Name { get; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("description")]
        public readonly string Description { get; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("author")]
        public readonly string Author { get; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("type")]
        public readonly string Type { get; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("tags")]
        public readonly IReadOnlyList<string>? Tags { get; } = null;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("features")]
        public readonly IReadOnlyList<string>? Features { get; } = null;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("pluginOptions")]
        public readonly IReadOnlyList<PluginOption>? PluginOptions { get; } = null;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("palette")]
        public readonly IReadOnlyList<PaletteData>? Palette { get; } = null;

        [JsonConstructor]
        public Plugin(
            string uuid,
            string name,
            string description,
            string author,
            string type,
            IReadOnlyList<string>? tags,
            IReadOnlyList<string>? features,
            IReadOnlyList<PluginOption>? pluginOptions,
            IReadOnlyList<PaletteData>? palette) => (
            UUID,
            Name,
            Description,
            Author,
            Type,
            Tags,
            Features,
            PluginOptions,
            Palette) = (
            uuid,
            name,
            description,
            author,
            type,
            tags,
            features,
            pluginOptions,
            palette);
        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
