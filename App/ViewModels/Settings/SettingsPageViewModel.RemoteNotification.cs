using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Avalonia.Threading;
using LegacyConfigurationKeys = MAAUnified.Compat.Constants.ConfigurationKeys;
using MAAUnified.App.Features.Dialogs;
using MAAUnified.App.ViewModels.Infrastructure;
using MAAUnified.Application.Configuration;
using MAAUnified.Application.Models;
using MAAUnified.Application.Services;
using MAAUnified.Application.Services.Localization;
using MAAUnified.Compat.Constants;
using MAAUnified.CoreBridge;
using MAAUnified.Platform;

namespace MAAUnified.App.ViewModels.Settings;

public sealed partial class SettingsPageViewModel
{
    public async Task SaveRemoteControlAsync(CancellationToken cancellationToken = default)
    {
        ClearRemoteControlStatus();
        var normalizedUserIdentity = (RemoteUserIdentity ?? string.Empty).Trim();
        var normalizedDeviceIdentity = (RemoteDeviceIdentity ?? string.Empty).Trim();
        if (ContainsInvalidRemoteIdentity(normalizedUserIdentity) || ContainsInvalidRemoteIdentity(normalizedDeviceIdentity))
        {
            var validation = UiOperationResult.Fail(
                UiErrorCode.RemoteControlInvalidParameters,
                "Remote user/device identity cannot contain control characters.");
            RemoteControlErrorMessage = FormatRemoteControlMessage(validation.Error?.Code, validation.Message);
            RemoteControlWarningMessage = string.Empty;
            RemoteControlStatusMessage = RootTexts.GetOrDefault(
                "Settings.RemoteControl.Status.SaveFailed",
                "Failed to save remote control settings.");
            LastErrorMessage = RemoteControlErrorMessage;
            StatusMessage = RemoteControlStatusMessage;
            await RecordFailedResultAsync(
                "Settings.RemoteControl.Save.Validation",
                validation,
                cancellationToken);
            return;
        }

        RemoteUserIdentity = normalizedUserIdentity;
        RemoteDeviceIdentity = normalizedDeviceIdentity;
        var updates = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [ConfigurationKeys.RemoteControlGetTaskEndpointUri] = RemoteGetTaskEndpoint,
            [ConfigurationKeys.RemoteControlReportStatusUri] = RemoteReportEndpoint,
            [ConfigurationKeys.RemoteControlUserIdentity] = normalizedUserIdentity,
            [ConfigurationKeys.RemoteControlDeviceIdentity] = normalizedDeviceIdentity,
            [ConfigurationKeys.RemoteControlPollIntervalMs] = RemotePollInterval.ToString(),
        };

        var result = await SaveScopedSettingsAsync(
            profileUpdates: updates,
            successScope: "Settings.RemoteControl.Save",
            cancellationToken: cancellationToken);
        if (await ApplyResultAsync(result, "Settings.RemoteControl.Save", cancellationToken))
        {
            RemoteControlStatusMessage = RootTexts.GetOrDefault(
                "Settings.RemoteControl.Status.SaveSucceeded",
                "Remote control settings saved.");
            RemoteControlErrorMessage = string.Empty;
            RemoteControlWarningMessage = string.Empty;
            return;
        }

        RemoteControlErrorMessage = FormatRemoteControlMessage(result.Error?.Code, result.Message);
        RemoteControlWarningMessage = string.Empty;
        RemoteControlStatusMessage = RootTexts.GetOrDefault(
            "Settings.RemoteControl.Status.SaveFailed",
            "Failed to save remote control settings.");
    }

    public async Task TestRemoteControlConnectivityAsync(CancellationToken cancellationToken = default)
    {
        ClearRemoteControlStatus();
        var request = new RemoteControlConnectivityRequest(
            RemoteGetTaskEndpoint,
            RemoteReportEndpoint,
            RemotePollInterval);
        var result = await Runtime.RemoteControlFeatureService.TestConnectivityAsync(request, cancellationToken);
        if (result.Success)
        {
            var summary = BuildRemoteConnectivitySummary(result.Value);
            RemoteControlStatusMessage = string.Format(
                RootTexts.GetOrDefault("Settings.RemoteControl.Status.TestSucceeded", "Connectivity test succeeded. {0}"),
                summary);
            StatusMessage = RemoteControlStatusMessage;
            LastErrorMessage = string.Empty;
            await RecordEventAsync(
                "Settings.RemoteControl.Test",
                RemoteControlStatusMessage,
                cancellationToken);
            return;
        }

        var message = FormatRemoteControlMessage(result.Error?.Code, result.Message);
        var detailsSummary = BuildRemoteConnectivitySummary(ParseRemoteConnectivityDetails(result.Error?.Details));
        if (!string.IsNullOrWhiteSpace(detailsSummary))
        {
            message = $"{message} {detailsSummary}";
        }

        if (string.Equals(result.Error?.Code, UiErrorCode.RemoteControlUnsupported, StringComparison.Ordinal))
        {
            RemoteControlWarningMessage = message;
            RemoteControlErrorMessage = string.Empty;
        }
        else
        {
            RemoteControlErrorMessage = message;
            RemoteControlWarningMessage = string.Empty;
        }

        RemoteControlStatusMessage = RootTexts.GetOrDefault(
            "Settings.RemoteControl.Status.TestFailed",
            "Connectivity test failed.");
        LastErrorMessage = message;
        await RecordFailedResultAsync(
            "Settings.RemoteControl.Test",
            UiOperationResult.Fail(result.Error?.Code ?? UiErrorCode.RemoteControlConnectivityFailed, message, result.Error?.Details),
            cancellationToken);
    }

    public async Task ValidateExternalNotificationParametersAsync(CancellationToken cancellationToken = default)
    {
        ClearExternalNotificationStatus();
        PersistCurrentProviderParameters();
        if (!ExternalNotificationEnabled)
        {
            LastErrorMessage = string.Empty;
            return;
        }

        var provider = SelectedNotificationProvider;
        var result = await Runtime.NotificationProviderFeatureService.ValidateProviderParametersAsync(
            new NotificationProviderRequest(provider, NotificationProviderParametersText),
            cancellationToken);
        if (result.Success)
        {
            ExternalNotificationStatusMessage = string.Format(
                RootTexts.GetOrDefault(
                    "Settings.ExternalNotification.Status.ValidateSucceeded",
                    "Provider `{0}` parameter validation succeeded."),
                provider);
            StatusMessage = ExternalNotificationStatusMessage;
            LastErrorMessage = string.Empty;
            await RecordEventAsync(
                "Settings.ExternalNotification.Validate",
                ExternalNotificationStatusMessage,
                cancellationToken);
            return;
        }

        await ApplyExternalNotificationFailure(result, "Settings.ExternalNotification.Validate", cancellationToken);
    }

    public async Task TestExternalNotificationAsync(CancellationToken cancellationToken = default)
    {
        ClearExternalNotificationStatus();
        PersistCurrentProviderParameters();
        if (!ExternalNotificationEnabled)
        {
            LastErrorMessage = string.Empty;
            return;
        }

        var provider = SelectedNotificationProvider;
        var result = await Runtime.NotificationProviderFeatureService.SendTestAsync(
            new NotificationProviderTestRequest(
                provider,
                NotificationProviderParametersText,
                NotificationTitle,
                NotificationMessage),
            cancellationToken);
        if (result.Success)
        {
            ExternalNotificationStatusMessage = string.Format(
                RootTexts.GetOrDefault(
                    "Settings.ExternalNotification.Status.TestSucceeded",
                    "Provider `{0}` test notification sent."),
                provider);
            StatusMessage = ExternalNotificationStatusMessage;
            LastErrorMessage = string.Empty;
            await RecordEventAsync(
                "Settings.ExternalNotification.TestSend",
                ExternalNotificationStatusMessage,
                cancellationToken);
            return;
        }

        await ApplyExternalNotificationFailure(result, "Settings.ExternalNotification.TestSend", cancellationToken);
    }

    public async Task SaveExternalNotificationAsync(CancellationToken cancellationToken = default)
    {
        ClearExternalNotificationStatus();
        PersistCurrentProviderParameters();

        var updates = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [ConfigurationKeys.ExternalNotificationEnabled] = ExternalNotificationEnabled.ToString(),
            [ConfigurationKeys.ExternalNotificationSendWhenComplete] = ExternalNotificationSendWhenComplete.ToString(),
            [ConfigurationKeys.ExternalNotificationSendWhenError] = ExternalNotificationSendWhenError.ToString(),
            [ConfigurationKeys.ExternalNotificationSendWhenTimeout] = ExternalNotificationSendWhenTimeout.ToString(),
            [ConfigurationKeys.ExternalNotificationEnableDetails] = ExternalNotificationEnableDetails.ToString(),
        };

        var applyProviderResult = await PopulateExternalNotificationProviderUpdatesAsync(
            updates,
            validateParameters: ExternalNotificationEnabled,
            cancellationToken);
        if (!applyProviderResult.Success)
        {
            await ApplyExternalNotificationFailure(
                applyProviderResult,
                ExternalNotificationEnabled
                    ? "Settings.ExternalNotification.Save.Validate"
                    : "Settings.ExternalNotification.Save.Disabled",
                cancellationToken);
            return;
        }

        var saveResult = await SaveScopedSettingsAsync(
            profileUpdates: updates,
            successScope: "Settings.ExternalNotification.Save",
            cancellationToken: cancellationToken);
        if (!saveResult.Success)
        {
            await ApplyExternalNotificationFailure(saveResult, "Settings.ExternalNotification.Save", cancellationToken);
            return;
        }

        ExternalNotificationStatusMessage = RootTexts.GetOrDefault(
            "Settings.ExternalNotification.Status.SaveSucceeded",
            "External notification settings saved.");
        ExternalNotificationErrorMessage = string.Empty;
        ExternalNotificationWarningMessage = string.Empty;
        StatusMessage = ExternalNotificationStatusMessage;
        LastErrorMessage = string.Empty;
        await RecordEventAsync(
            "Settings.ExternalNotification.Save",
            ExternalNotificationStatusMessage,
            cancellationToken);
    }

    private string NormalizeNotificationProvider(string? provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            return AvailableNotificationProviders.Count > 0
                ? AvailableNotificationProviders[0]
                : DefaultNotificationProviders[0];
        }

        var normalized = provider.Trim();
        foreach (var candidate in AvailableNotificationProviders)
        {
            if (string.Equals(candidate, normalized, StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }
        }

        return AvailableNotificationProviders.Count > 0
            ? AvailableNotificationProviders[0]
            : DefaultNotificationProviders[0];
    }

    private async Task EnsureNotificationProvidersLoadedAsync(CancellationToken cancellationToken)
    {
        string[] providers;
        try
        {
            providers = await Runtime.NotificationProviderFeatureService.GetAvailableProvidersAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            providers = DefaultNotificationProviders;
            await RecordUnhandledExceptionAsync(
                "Settings.ExternalNotification.Providers",
                ex,
                UiErrorCode.NotificationProviderFailed,
                $"Failed to load provider list. Falling back to defaults: {ex.Message}",
                cancellationToken);
        }

        var normalizedProviders = providers
            .Where(static p => !string.IsNullOrWhiteSpace(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (normalizedProviders.Count == 0)
        {
            normalizedProviders = [.. DefaultNotificationProviders];
        }

        AvailableNotificationProviders.Clear();
        foreach (var provider in normalizedProviders)
        {
            AvailableNotificationProviders.Add(provider);
            if (!_notificationProviderParameters.ContainsKey(provider))
            {
                _notificationProviderParameters[provider] = string.Empty;
            }
        }

        _selectedNotificationProvider = NormalizeNotificationProvider(_selectedNotificationProvider);
        NotificationProviderParametersText = _notificationProviderParameters.TryGetValue(_selectedNotificationProvider, out var current)
            ? current
            : string.Empty;
        OnPropertyChanged(nameof(SelectedNotificationProvider));
    }

    private void PersistCurrentProviderParameters()
    {
        if (!string.IsNullOrWhiteSpace(SelectedNotificationProvider))
        {
            _notificationProviderParameters[SelectedNotificationProvider] = NotificationProviderParametersText;
        }
    }

    private void LoadExternalNotificationProviderParametersFromConfig(UnifiedConfig config)
    {
        _notificationProviderParameters.Clear();
        foreach (var provider in AvailableNotificationProviders)
        {
            _notificationProviderParameters[provider] = BuildProviderParameterTextFromConfig(provider, config);
        }

        var selected = NormalizeNotificationProvider(_selectedNotificationProvider);
        _selectedNotificationProvider = selected;
        NotificationProviderParametersText = _notificationProviderParameters.TryGetValue(selected, out var text)
            ? text
            : string.Empty;
        OnPropertyChanged(nameof(SelectedNotificationProvider));
    }

    private static string BuildProviderParameterTextFromConfig(string provider, UnifiedConfig config)
    {
        if (!ProviderConfigKeyMap.TryGetValue(provider, out var keyMap))
        {
            return string.Empty;
        }

        var lines = new List<string>();
        foreach (var (parameterKey, configKey) in keyMap)
        {
            if (!TryGetConfigNode(config, configKey, ConfigValuePreference.ProfileFirst, out var node) || node is null)
            {
                continue;
            }

            var value = node is JsonValue jsonValue
                ? jsonValue.ToString()
                : node.ToString();
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            lines.Add($"{parameterKey}={value.Trim()}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private bool TryParseProviderParameterText(
        string? text,
        out Dictionary<string, string> parameters,
        out string? error)
    {
        parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        error = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n', StringSplitOptions.TrimEntries);
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (line.Length == 0)
            {
                continue;
            }

            var separator = line.IndexOf('=');
            if (separator <= 0)
            {
                error = string.Format(
                    RootTexts.GetOrDefault(
                        "Settings.ExternalNotification.Error.ParameterFormatLine",
                        "Invalid parameter format (line {0}). Expected key=value: `{1}`"),
                    i + 1,
                    line);
                return false;
            }

            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim();
            if (key.Length == 0)
            {
                error = string.Format(
                    RootTexts.GetOrDefault(
                        "Settings.ExternalNotification.Error.ParameterKeyEmpty",
                        "Invalid parameter format (line {0}). Key cannot be empty."),
                    i + 1);
                return false;
            }

            parameters[key] = value;
        }

        return true;
    }

    private async Task<UiOperationResult> PopulateExternalNotificationProviderUpdatesAsync(
        Dictionary<string, string> updates,
        bool validateParameters,
        CancellationToken cancellationToken)
    {
        foreach (var provider in AvailableNotificationProviders)
        {
            if (!ProviderConfigKeyMap.TryGetValue(provider, out var keyMap))
            {
                continue;
            }

            var parameterText = _notificationProviderParameters.TryGetValue(provider, out var stored)
                ? stored
                : string.Empty;

            if (string.IsNullOrWhiteSpace(parameterText))
            {
                foreach (var (_, configKey) in keyMap)
                {
                    updates[configKey] = string.Empty;
                }

                continue;
            }

            if (validateParameters)
            {
                var validate = await Runtime.NotificationProviderFeatureService.ValidateProviderParametersAsync(
                    new NotificationProviderRequest(provider, parameterText),
                    cancellationToken);
                if (!validate.Success)
                {
                    return validate;
                }
            }

            if (!TryParseProviderParameterText(parameterText, out var parsed, out var parseError))
            {
                if (!validateParameters)
                {
                    continue;
                }

                return UiOperationResult.Fail(
                    UiErrorCode.NotificationProviderInvalidParameters,
                    parseError
                    ?? RootTexts.GetOrDefault(
                        "Settings.ExternalNotification.Error.ParseFailed",
                        "Failed to parse provider parameters."));
            }

            foreach (var (parameterKey, configKey) in keyMap)
            {
                updates[configKey] = parsed.TryGetValue(parameterKey, out var value)
                    ? value
                    : string.Empty;
            }
        }

        return UiOperationResult.Ok(
            RootTexts.GetOrDefault(
                "Settings.ExternalNotification.Status.PreparedUpdates",
                "Prepared external notification provider updates."));
    }

    private string FormatRemoteControlMessage(string? code, string fallbackMessage)
    {
        return code switch
        {
            UiErrorCode.RemoteControlInvalidParameters => string.Format(
                RootTexts.GetOrDefault(
                    "Settings.RemoteControl.Error.InvalidParameters",
                    "Remote control parameter error: {0} ({1})"),
                fallbackMessage,
                UiErrorCode.RemoteControlInvalidParameters),
            UiErrorCode.RemoteControlNetworkFailure => string.Format(
                RootTexts.GetOrDefault(
                    "Settings.RemoteControl.Error.NetworkFailure",
                    "Remote control connectivity failed: {0} ({1})"),
                fallbackMessage,
                UiErrorCode.RemoteControlNetworkFailure),
            UiErrorCode.RemoteControlUnsupported => string.Format(
                RootTexts.GetOrDefault(
                    "Settings.RemoteControl.Error.Unsupported",
                    "Remote control connectivity test is unsupported in this environment: {0} ({1})"),
                fallbackMessage,
                UiErrorCode.RemoteControlUnsupported),
            _ => fallbackMessage,
        };
    }

    private static bool ContainsInvalidRemoteIdentity(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        foreach (var ch in value)
        {
            if (char.IsControl(ch))
            {
                return true;
            }
        }

        return false;
    }

    private static RemoteControlConnectivityResult? ParseRemoteConnectivityDetails(string? details)
    {
        if (string.IsNullOrWhiteSpace(details))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<RemoteControlConnectivityResult>(details);
        }
        catch
        {
            return null;
        }
    }

    private static string BuildRemoteConnectivitySummary(RemoteControlConnectivityResult? result)
    {
        if (result is null)
        {
            return string.Empty;
        }

        return $"GetTask={result.GetTaskProbe.Message}; Report={result.ReportProbe.Message}; Poll={result.PollIntervalMs}ms";
    }

    private string FormatExternalNotificationMessage(string? code, string fallbackMessage)
    {
        return code switch
        {
            UiErrorCode.NotificationProviderInvalidParameters
                => string.Format(
                    RootTexts.GetOrDefault(
                        "Settings.ExternalNotification.Error.InvalidParameters",
                        "External notification parameter error: {0} ({1})"),
                    fallbackMessage,
                    UiErrorCode.NotificationProviderInvalidParameters),
            UiErrorCode.NotificationProviderNetworkFailure
                => string.Format(
                    RootTexts.GetOrDefault(
                        "Settings.ExternalNotification.Error.NetworkFailure",
                        "External notification network failure: {0} ({1})"),
                    fallbackMessage,
                    UiErrorCode.NotificationProviderNetworkFailure),
            UiErrorCode.NotificationProviderUnsupported
                => string.Format(
                    RootTexts.GetOrDefault(
                        "Settings.ExternalNotification.Error.Unsupported",
                        "External notification is unsupported in this environment: {0} ({1})"),
                    fallbackMessage,
                    UiErrorCode.NotificationProviderUnsupported),
            _ => fallbackMessage,
        };
    }

    private async Task ApplyExternalNotificationFailure(
        UiOperationResult result,
        string scope,
        CancellationToken cancellationToken)
    {
        var message = FormatExternalNotificationMessage(result.Error?.Code, result.Message);
        if (string.Equals(result.Error?.Code, UiErrorCode.NotificationProviderUnsupported, StringComparison.Ordinal))
        {
            ExternalNotificationWarningMessage = message;
            ExternalNotificationErrorMessage = string.Empty;
        }
        else
        {
            ExternalNotificationErrorMessage = message;
            ExternalNotificationWarningMessage = string.Empty;
        }

        ExternalNotificationStatusMessage = RootTexts.GetOrDefault(
            "Settings.ExternalNotification.Status.OperationFailed",
            "External notification operation failed.");
        LastErrorMessage = message;
        await RecordFailedResultAsync(
            scope,
            UiOperationResult.Fail(result.Error?.Code ?? UiErrorCode.NotificationProviderFailed, message, result.Error?.Details),
            cancellationToken);
    }

}
