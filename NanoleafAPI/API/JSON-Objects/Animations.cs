using System.Text.Json.Serialization;

namespace NanoleafAPI
{
    public readonly struct Animations
    {
        [JsonPropertyName("animations")]
        public readonly IReadOnlyList<Animation> List { get; }

        [JsonConstructor]
        public Animations(IReadOnlyList<Animation> list) => (List) = (list);
    }
}
