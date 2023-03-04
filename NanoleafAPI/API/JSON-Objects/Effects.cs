using System.Text.Json.Serialization;

namespace NanoleafAPI
{
    public readonly struct Effects
    {
        [JsonPropertyName("effectsList")]
        public readonly IReadOnlyList<string> List { get; }

        [JsonPropertyName("select")]
        public readonly string Selected { get; }

        [JsonConstructor]
        public Effects(string selected, IReadOnlyList<string> list) => (Selected, List) = (selected, list);
    }
}
