using NanoleafAPI.API;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NanoleafAPI
{
    public readonly struct TouchConfig
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("supportedFeatures")]
        public readonly SupportedFeatures? SupportedFeatures { get; } = null;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("defaultSystemConfig")]
        public readonly DefaultSystemConfig? DefaultSystemConfig { get; } = null;

        [JsonPropertyName("userSystemConfig")]
        public readonly UserSystemConfig UserSystemConfig { get; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("userPanelConfigs")]
        public readonly IReadOnlyList<JsonElement>? UserPanelConfigs { get; } = null;
        [JsonConstructor]
        public TouchConfig(
            SupportedFeatures? supportedFeatures,
            DefaultSystemConfig? defaultSystemConfig,
            UserSystemConfig userSystemConfig,
            IReadOnlyList<JsonElement>? userPanelConfigs
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
        public TouchConfig(UserSystemConfig userSystemConfig) => (UserSystemConfig) = (userSystemConfig);
        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
    public readonly struct SupportedFeatures
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("systemGestures")]
        public readonly IReadOnlyList<EGesture> Gestures { get; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("systemActions")]
        public readonly IReadOnlyList<EAction> Actions { get; }

        [JsonConstructor]
        public SupportedFeatures(IReadOnlyList<EGesture> gestures, IReadOnlyList<EAction> actions) => (Gestures, Actions) = (gestures, actions);

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
        public readonly IReadOnlyList<JsonElement>? Subscribers { get; } = default;

        [JsonConstructor]
        public UserSystemConfig(bool enabled, IReadOnlyList<GestureAction> gestureActions, IReadOnlyList<JsonElement> subscribers) => (Enabled, GestureActions, Subscribers) = (enabled, gestureActions, subscribers);
        public UserSystemConfig(bool enabled, params GestureAction[] gestureActions)
        {
            this.Enabled = enabled;
            this.GestureActions = gestureActions;
        }
        public override string ToString()
        {
            return JsonSerializer.Serialize(this); 
        }
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is UserSystemConfig usc)
            {
                if (!bool.Equals(this.Enabled, usc.Enabled))
                    return false;
                if (!this.GestureActions.All(usc.GestureActions.Contains))
                    return false;
                if (this.Subscribers == null && usc.Subscribers == null)
                    return true;
                if (this.Subscribers == null || usc.Subscribers == null)
                    return false;
                if (!this.Subscribers.All(usc.Subscribers.Contains))
                    return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Enabled, GestureActions, Subscribers);
        }

        public static bool operator ==(UserSystemConfig left, UserSystemConfig right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(UserSystemConfig left, UserSystemConfig right)
        {
            return !(left == right);
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
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is GestureAction ga)
                return object.Equals(this.Gesture, ga.Gesture) && object.Equals(this.Action, ga.Action);
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Gesture, Action);
        }

        public static bool operator ==(GestureAction left, GestureAction right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GestureAction left, GestureAction right)
        {
            return !(left == right);
        }
    }
}