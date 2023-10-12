using System.Text.Json.Serialization;

namespace NanoleafAPI
{
    public struct Schedule
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("id")]
        public readonly string ID { get; } = string.Empty;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("set_id")]
        public readonly string SetID { get; } = string.Empty;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("enabled")]
        public readonly bool Enabled { get; } = false;

        [JsonConstructor]
        public Schedule(
            string id,
            string setID,
            bool enabled) => (
            ID,
            SetID,
            Enabled) = (
            id,
            setID,
            enabled);
    }
}