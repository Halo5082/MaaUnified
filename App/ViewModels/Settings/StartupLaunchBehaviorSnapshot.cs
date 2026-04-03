using System.Globalization;
using System.Text.Json.Nodes;
using MAAUnified.Application.Models;
using MAAUnified.Compat.Constants;

namespace MAAUnified.App.ViewModels.Settings;

internal sealed record StartupLaunchBehaviorSnapshot(
    bool RunDirectly,
    bool MinimizeDirectly,
    bool OpenEmulatorAfterLaunch)
{
    public static StartupLaunchBehaviorSnapshot Default { get; } = new(
        RunDirectly: false,
        MinimizeDirectly: false,
        OpenEmulatorAfterLaunch: false);

    public static StartupLaunchBehaviorSnapshot FromConfig(UnifiedConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        return new StartupLaunchBehaviorSnapshot(
            RunDirectly: ReadCurrentProfileBool(config, ConfigurationKeys.RunDirectly, false),
            MinimizeDirectly: ReadGlobalBool(config, ConfigurationKeys.MinimizeDirectly, false),
            OpenEmulatorAfterLaunch: ReadCurrentProfileBool(config, ConfigurationKeys.StartEmulator, false));
    }

    private static bool ReadCurrentProfileBool(UnifiedConfig config, string key, bool fallback)
    {
        if (!config.Profiles.TryGetValue(config.CurrentProfile, out var profile))
        {
            return fallback;
        }

        return ReadBool(profile.Values, key, fallback);
    }

    private static bool ReadGlobalBool(UnifiedConfig config, string key, bool fallback)
        => ReadBool(config.GlobalValues, key, fallback);

    private static bool ReadBool(IDictionary<string, JsonNode?> values, string key, bool fallback)
    {
        if (!values.TryGetValue(key, out var node) || node is null)
        {
            return fallback;
        }

        if (node is JsonValue value)
        {
            if (value.TryGetValue<bool>(out var boolValue))
            {
                return boolValue;
            }

            if (value.TryGetValue<int>(out var intValue))
            {
                return intValue != 0;
            }

            if (value.TryGetValue<string>(out var stringValue))
            {
                if (bool.TryParse(stringValue, out var parsedBool))
                {
                    return parsedBool;
                }

                if (int.TryParse(stringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedInt))
                {
                    return parsedInt != 0;
                }
            }
        }

        return bool.TryParse(node.ToString(), out var parsedFallback) ? parsedFallback : fallback;
    }
}
