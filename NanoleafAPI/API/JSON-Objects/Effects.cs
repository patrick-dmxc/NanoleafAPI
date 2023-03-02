using System.Text.Json.Serialization;

namespace NanoleafAPI
{
    public struct Effects
    {
        [JsonPropertyName("effectsList")]
        public IReadOnlyList<string> List { get; }

        [JsonPropertyName("select")]
        public string Selected { get; }

        [JsonConstructor]
        public Effects(string selected, IReadOnlyList<string> list) => (Selected, List) = (selected, list);
    }
}
