using System.Text.Json.Serialization;

namespace NanoleafAPI
{
    public readonly struct Plugins
    {
        [JsonPropertyName("plugins")]
        public readonly IReadOnlyList<Plugin> List { get; }

        [JsonConstructor]
        public Plugins(IReadOnlyList<Plugin> list) => (List) = (list);
    }
}
