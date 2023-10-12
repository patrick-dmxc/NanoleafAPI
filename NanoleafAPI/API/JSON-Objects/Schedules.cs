using System.Text.Json.Serialization;

namespace NanoleafAPI
{
    public readonly struct Schedules
    {
        [JsonPropertyName("schedules")]
        public readonly IReadOnlyList<Schedule> List { get; }

        [JsonConstructor]
        public Schedules(IReadOnlyList<Schedule> list) => (List) = (list);
    }
}
