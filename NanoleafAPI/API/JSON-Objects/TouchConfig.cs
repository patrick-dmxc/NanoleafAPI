using NanoleafAPI.API;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NanoleafAPI
{
    public readonly struct TouchConfig
    {
        [JsonPropertyName("supportedFeatures")]
        public readonly SupportedFeatures SupportedFeatures { get; }
        [JsonPropertyName("defaultSystemConfig")]
        public readonly DefaultSystemConfig DefaultSystemConfig { get; }
        [JsonPropertyName("userSystemConfig")]
        public readonly UserSystemConfig UserSystemConfig { get; }
        [JsonPropertyName("userPanelConfigs")]
        public readonly IReadOnlyList<JsonElement> UserPanelConfigs { get; }
        [JsonConstructor]
        public TouchConfig(
            SupportedFeatures supportedFeatures,
            DefaultSystemConfig defaultSystemConfig,
            UserSystemConfig userSystemConfig,
            IReadOnlyList<JsonElement> userPanelConfigs
            ) => (
            SupportedFeatures,
            DefaultSystemConfig,
            UserSystemConfig,
            UserPanelConfigs
            ) = (
            supportedFeatures,
            defaultSystemConfig,
            userSystemConfig,
            userPanelConfigs
            );
        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
    public readonly struct SupportedFeatures
    {
        [JsonPropertyName("systemGestures")]
        public readonly IReadOnlyList<EGesture> Gestures { get; }
        [JsonPropertyName("systemActions")]
        public readonly IReadOnlyList<EAction> Actions { get; }

        [JsonConstructor]
        public SupportedFeatures(IReadOnlyList<EGesture> gestures, IReadOnlyList<EAction> actions) => (Gestures, Actions) = (gestures, actions);

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
    public readonly struct SystemActions
    {
        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
    public readonly struct DefaultSystemConfig
    {
        [JsonPropertyName("gestureActions")]
        public IReadOnlyList<GestureAction> GestureActions { get; }
        [JsonConstructor]
        public DefaultSystemConfig(IReadOnlyList<GestureAction> gestureActions) => (GestureActions) = (gestureActions);
        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
    public readonly struct UserSystemConfig
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; }
        [JsonPropertyName("gestureActions")]
        public IReadOnlyList<GestureAction> GestureActions { get; }
        [JsonPropertyName("subscribers")]
        public readonly IReadOnlyList<JsonElement> UserPanelConfigs { get; }

        [JsonConstructor]
        public UserSystemConfig(bool enabled, IReadOnlyList<GestureAction> gestureActions, IReadOnlyList<JsonElement> userPanelConfigs) => (Enabled, GestureActions, UserPanelConfigs) = (enabled, gestureActions, userPanelConfigs);
        public override string ToString()
        {
            return JsonSerializer.Serialize(this); 
        }
    }
    public readonly struct GestureAction
    {
        [JsonPropertyName("gesture")]
        public readonly EGesture Gesture { get; }
        [JsonPropertyName("action")]
        public readonly EAction? Action { get; }

        [JsonConstructor]
        public GestureAction(EGesture gesture, EAction? action) => (Gesture, Action) = (gesture, action);
        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}