using System.Text.Json.Serialization;

namespace NanoleafAPI
{
    public struct User
    {
        [JsonPropertyName("auth_token")]
        public string AuthToken { get; }
        [JsonConstructor]
        public User(string authToken) => (AuthToken) = (authToken);
        public override string ToString()
        {
            return $"AuthToken: {AuthToken}";
        }
    }
}
