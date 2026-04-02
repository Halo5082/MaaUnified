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
    public async Task SaveVersionUpdateSettingsAsync(CancellationToken cancellationToken = default)
    {
        if (IsVersionUpdateActionRunning)
        {
            return;
        }

        IsVersionUpdateActionRunning = true;
        try
        {
            await SaveVersionUpdateChannelAsync(cancellationToken);
            if (HasVersionUpdateErrorMessage)
            {
                return;
            }

            await SaveVersionUpdateProxyAsync(cancellationToken);
            RefreshVersionUpdateSchedulerState();
        }
        finally
        {
            IsVersionUpdateActionRunning = false;
        }
    }

    public async Task SaveVersionUpdateChannelAsync(CancellationToken cancellationToken = default)
    {
        VersionUpdateStatusMessage = string.Empty;
        VersionUpdateErrorMessage = string.Empty;

        var policy = BuildVersionUpdatePolicy();
        var saveResult = await Runtime.VersionUpdateFeatureService.SaveChannelAsync(policy, cancellationToken);
        if (!await ApplyResultAsync(saveResult, "Settings.VersionUpdate.Channel.Save", cancellationToken))
        {
            VersionUpdateErrorMessage = saveResult.Message;
            VersionUpdateStatusMessage = RootTexts.GetOrDefault(
                "Settings.VersionUpdate.Status.SaveChannelFailed",
                "Failed to save update channel settings.");
            return;
        }

        VersionUpdateStatusMessage = RootTexts.GetOrDefault(
            "Settings.VersionUpdate.Status.SaveChannelSucceeded",
            "Update channel settings saved.");
        VersionUpdateErrorMessage = string.Empty;
    }

    public async Task SaveVersionUpdateProxyAsync(CancellationToken cancellationToken = default)
    {
        VersionUpdateStatusMessage = string.Empty;
        VersionUpdateErrorMessage = string.Empty;

        var policy = BuildVersionUpdatePolicy();
        var saveResult = await Runtime.VersionUpdateFeatureService.SaveProxyAsync(policy, cancellationToken);
        if (!await ApplyResultAsync(saveResult, "Settings.VersionUpdate.Proxy.Save", cancellationToken))
        {
            VersionUpdateErrorMessage = saveResult.Message;
            VersionUpdateStatusMessage = RootTexts.GetOrDefault(
                "Settings.VersionUpdate.Status.SaveProxyFailed",
                "Failed to save update proxy settings.");
            return;
        }

        VersionUpdateStatusMessage = RootTexts.GetOrDefault(
            "Settings.VersionUpdate.Status.SaveProxySucceeded",
            "Update proxy settings saved.");
        VersionUpdateErrorMessage = string.Empty;
    }

    public async Task CheckVersionUpdateAsync(CancellationToken cancellationToken = default)
    {
        if (IsVersionUpdateActionRunning)
        {
            return;
        }

        IsVersionUpdateActionRunning = true;
        VersionUpdateStatusMessage = string.Empty;
        VersionUpdateErrorMessage = string.Empty;

        try
        {
            var checkOperation = await ExecuteVersionUpdateCheckAsync("Settings.VersionUpdate.Check", cancellationToken);
            var checkResult = await ApplyResultNoDialogAsync(checkOperation, "Settings.VersionUpdate.Check", cancellationToken);
            if (checkResult is null)
            {
                VersionUpdateStatusMessage = RootTexts.GetOrDefault(
                    "Settings.VersionUpdate.Status.CheckFailed",
                    "Update check failed.");
                VersionUpdateErrorMessage = checkOperation.Message;
                return;
            }

            await ApplyVersionUpdateCheckResultAsync(checkResult, cancellationToken);
            if (!HasVersionUpdateErrorMessage)
            {
                VersionUpdateStatusMessage = checkOperation.Message;
                VersionUpdateErrorMessage = string.Empty;
            }
        }
        finally
        {
            IsVersionUpdateActionRunning = false;
        }
    }

    public async Task CheckVersionUpdateWithDialogAsync(CancellationToken cancellationToken = default)
    {
        if (IsVersionUpdateActionRunning)
        {
            return;
        }

        IsVersionUpdateActionRunning = true;
        VersionUpdateStatusMessage = string.Empty;
        VersionUpdateErrorMessage = string.Empty;

        try
        {
            var checkOperation = await ExecuteVersionUpdateCheckAsync("Settings.VersionUpdate.Check", cancellationToken);
            var checkResult = await ApplyResultNoDialogAsync(checkOperation, "Settings.VersionUpdate.Check", cancellationToken);
            if (checkResult is null)
            {
                VersionUpdateStatusMessage = RootTexts.GetOrDefault(
                    "Settings.VersionUpdate.Status.CheckFailed",
                    "Update check failed.");
                VersionUpdateErrorMessage = checkOperation.Message;
                return;
            }

            await ApplyVersionUpdateCheckResultAsync(checkResult, cancellationToken);
            var chrome = CreateSettingsDialogChrome(
                texts => new DialogChromeSnapshot(
                    title: texts.GetOrDefault("Settings.VersionUpdate.Dialog.Title", "Version Update"),
                    confirmText: texts.GetOrDefault("Settings.VersionUpdate.Dialog.Confirm", "Confirm"),
                    cancelText: texts.GetOrDefault("Settings.VersionUpdate.Dialog.Cancel", "Later")));
            var chromeSnapshot = chrome.GetSnapshot();
            var request = new VersionUpdateDialogRequest(
                Title: chromeSnapshot.Title,
                CurrentVersion: checkResult.CurrentVersion,
                TargetVersion: string.IsNullOrWhiteSpace(checkResult.ReleaseName)
                    ? checkResult.TargetVersion
                    : checkResult.ReleaseName,
                Summary: checkResult.Summary,
                Body: checkResult.Body,
                ConfirmText: chromeSnapshot.ConfirmText ?? RootTexts.GetOrDefault("Settings.VersionUpdate.Dialog.Confirm", "Confirm"),
                CancelText: chromeSnapshot.CancelText ?? RootTexts.GetOrDefault("Settings.VersionUpdate.Dialog.Cancel", "Later"),
                Chrome: chrome);
            var dialogResult = await _dialogService.ShowVersionUpdateAsync(request, "Settings.VersionUpdate.Dialog", cancellationToken);
            VersionUpdateStatusMessage = dialogResult.Return switch
            {
                DialogReturnSemantic.Confirm => RootTexts.GetOrDefault(
                    "Settings.VersionUpdate.Status.DialogConfirmed",
                    "Version update dialog confirmed."),
                DialogReturnSemantic.Cancel => RootTexts.GetOrDefault(
                    "Settings.VersionUpdate.Status.DialogCancelled",
                    "Version update dialog cancelled."),
                _ => RootTexts.GetOrDefault(
                    "Settings.VersionUpdate.Status.DialogClosed",
                    "Version update dialog closed."),
            };
            VersionUpdateErrorMessage = string.Empty;
        }
        finally
        {
            IsVersionUpdateActionRunning = false;
        }
    }

    public async Task RunStartupVersionUpdateCheckAsync(CancellationToken cancellationToken = default)
    {
        if (_versionUpdateStartupCheckTriggered || !VersionUpdateStartupCheck)
        {
            return;
        }

        _versionUpdateStartupCheckTriggered = true;
        await RunVersionUpdateCheckInternalAsync("Settings.VersionUpdate.StartupCheck", cancellationToken);
    }

    public async Task RunScheduledVersionUpdateCheckAsync(CancellationToken cancellationToken = default)
    {
        if (!VersionUpdateScheduledCheck)
        {
            return;
        }

        await RunVersionUpdateCheckInternalAsync("Settings.VersionUpdate.ScheduledCheck", cancellationToken);
    }

    public async Task ManualUpdateResourceAsync(CancellationToken cancellationToken = default)
    {
        if (IsVersionUpdateActionRunning)
        {
            return;
        }

        IsVersionUpdateActionRunning = true;
        VersionUpdateStatusMessage = string.Empty;
        VersionUpdateErrorMessage = string.Empty;

        try
        {
            var policy = BuildVersionUpdatePolicy();
            var updateResult = await Runtime.VersionUpdateFeatureService.UpdateResourceAsync(
                policy,
                ConnectionGameSharedState.ClientType,
                cancellationToken);
            var payload = await ApplyResultNoDialogAsync(updateResult, "Settings.VersionUpdate.Resource.Update", cancellationToken);
            if (payload is null)
            {
                VersionUpdateErrorMessage = updateResult.Message;
                VersionUpdateStatusMessage = RootTexts.GetOrDefault(
                    "Settings.VersionUpdate.Status.ResourceUpdateFailed",
                    "Resource update failed.");
                return;
            }

            VersionUpdateStatusMessage = payload;
            VersionUpdateErrorMessage = string.Empty;
            await RefreshVersionUpdateResourceInfoAsync(cancellationToken);
            ResourceVersionUpdated?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            IsVersionUpdateActionRunning = false;
        }
    }

    public async Task RefreshVersionUpdateResourceInfoAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await Runtime.VersionUpdateFeatureService.LoadResourceVersionInfoAsync(
                ConnectionGameSharedState.ClientType,
                cancellationToken);
            var info = await ApplyResultNoDialogAsync(result, "Settings.VersionUpdate.ResourceInfo.Load", cancellationToken);
            if (info is null)
            {
                UpdatePanelResourceVersion = string.Empty;
                UpdatePanelResourceTime = string.Empty;
                return;
            }

            UpdatePanelResourceVersion = info.VersionName;
            UpdatePanelResourceTime = info.LastUpdatedAt == DateTime.MinValue
                ? string.Empty
                : info.LastUpdatedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }
        catch
        {
            UpdatePanelResourceVersion = string.Empty;
            UpdatePanelResourceTime = string.Empty;
        }
    }

    public void RefreshVersionUpdateSchedulerState()
    {
        if (VersionUpdateScheduledCheck)
        {
            if (!_versionUpdateSchedulerTimer.IsEnabled)
            {
                _versionUpdateSchedulerTimer.Start();
            }

            return;
        }

        if (_versionUpdateSchedulerTimer.IsEnabled)
        {
            _versionUpdateSchedulerTimer.Stop();
        }
    }

    private void OnVersionUpdateSchedulerTick(object? sender, EventArgs e)
    {
        _ = RunScheduledVersionUpdateCheckAsync();
    }

    public async Task OpenVersionUpdateChangelogAsync(CancellationToken cancellationToken = default)
    {
        var result = await _openExternalTargetAsync(VersionUpdateChangelogUrl, cancellationToken);
        if (!await ApplyResultAsync(result, "Settings.VersionUpdate.OpenChangelog", cancellationToken))
        {
            VersionUpdateErrorMessage = result.Message;
            VersionUpdateStatusMessage = RootTexts.GetOrDefault(
                "Settings.VersionUpdate.Status.OpenChangelogFailed",
                "Failed to open changelog.");
            return;
        }

        VersionUpdateStatusMessage = RootTexts.GetOrDefault(
            "Settings.VersionUpdate.Status.OpenChangelogSucceeded",
            "Changelog opened.");
        VersionUpdateErrorMessage = string.Empty;
    }

    public async Task OpenVersionUpdateResourceRepositoryAsync(CancellationToken cancellationToken = default)
    {
        var result = await _openExternalTargetAsync(VersionUpdateResourceRepositoryUrl, cancellationToken);
        if (!await ApplyResultAsync(result, "Settings.VersionUpdate.OpenResourceRepository", cancellationToken))
        {
            VersionUpdateErrorMessage = result.Message;
            VersionUpdateStatusMessage = RootTexts.GetOrDefault(
                "Settings.VersionUpdate.Status.OpenResourceRepositoryFailed",
                "Failed to open resource repository.");
            return;
        }

        VersionUpdateStatusMessage = RootTexts.GetOrDefault(
            "Settings.VersionUpdate.Status.OpenResourceRepositorySucceeded",
            "Resource repository opened.");
        VersionUpdateErrorMessage = string.Empty;
    }

    public async Task OpenVersionUpdateMirrorChyanAsync(CancellationToken cancellationToken = default)
    {
        var result = await _openExternalTargetAsync(VersionUpdateMirrorChyanUrl, cancellationToken);
        if (!await ApplyResultAsync(result, "Settings.VersionUpdate.OpenMirrorChyan", cancellationToken))
        {
            VersionUpdateErrorMessage = result.Message;
            VersionUpdateStatusMessage = RootTexts.GetOrDefault(
                "Settings.VersionUpdate.Status.OpenMirrorChyanFailed",
                "Failed to open MirrorChyan.");
            return;
        }

        VersionUpdateStatusMessage = RootTexts.GetOrDefault(
            "Settings.VersionUpdate.Status.OpenMirrorChyanSucceeded",
            "MirrorChyan opened.");
        VersionUpdateErrorMessage = string.Empty;
    }

    private VersionUpdatePolicy BuildVersionUpdatePolicy()
    {
        return new VersionUpdatePolicy(
            Proxy: VersionUpdateProxy,
            ProxyType: VersionUpdateProxyType,
            VersionType: VersionUpdateVersionType,
            ResourceUpdateSource: VersionUpdateResourceSource,
            ForceGithubGlobalSource: VersionUpdateForceGithubSource,
            MirrorChyanCdk: VersionUpdateMirrorChyanCdk,
            MirrorChyanCdkExpired: VersionUpdateMirrorChyanCdkExpired,
            StartupUpdateCheck: VersionUpdateStartupCheck,
            ScheduledUpdateCheck: VersionUpdateScheduledCheck,
            ResourceApi: VersionUpdateResourceApi,
            AllowNightlyUpdates: VersionUpdateAllowNightly,
            HasAcknowledgedNightlyWarning: VersionUpdateAcknowledgedNightlyWarning,
            UseAria2: VersionUpdateUseAria2,
            AutoDownloadUpdatePackage: VersionUpdateAutoDownload,
            AutoInstallUpdatePackage: VersionUpdateAutoInstall,
            VersionName: VersionUpdateName,
            VersionBody: VersionUpdateBody,
            IsFirstBoot: VersionUpdateIsFirstBoot,
            VersionPackage: VersionUpdatePackage,
            DoNotShowUpdate: VersionUpdateDoNotShow);
    }

    private void ApplyVersionUpdatePolicy(VersionUpdatePolicy policy)
    {
        RunWithSuppressedSettingsBackfill(() =>
        {
            VersionUpdateProxy = policy.Proxy;
            VersionUpdateProxyType = policy.ProxyType;
            VersionUpdateVersionType = policy.VersionType;
            VersionUpdateResourceSource = policy.ResourceUpdateSource;
            VersionUpdateForceGithubSource = policy.ForceGithubGlobalSource;
            VersionUpdateMirrorChyanCdk = policy.MirrorChyanCdk;
            VersionUpdateMirrorChyanCdkExpired = policy.MirrorChyanCdkExpired;
            VersionUpdateStartupCheck = policy.StartupUpdateCheck;
            VersionUpdateScheduledCheck = policy.ScheduledUpdateCheck;
            VersionUpdateResourceApi = policy.ResourceApi;
            VersionUpdateAllowNightly = policy.AllowNightlyUpdates;
            VersionUpdateAcknowledgedNightlyWarning = policy.HasAcknowledgedNightlyWarning;
            VersionUpdateUseAria2 = policy.UseAria2;
            VersionUpdateAutoDownload = policy.AutoDownloadUpdatePackage;
            VersionUpdateAutoInstall = policy.AutoInstallUpdatePackage;
            VersionUpdateName = policy.VersionName;
            VersionUpdateBody = policy.VersionBody;
            VersionUpdateIsFirstBoot = policy.IsFirstBoot;
            VersionUpdatePackage = policy.VersionPackage;
            VersionUpdateDoNotShow = policy.DoNotShowUpdate;
        });
    }

    private async Task RunVersionUpdateCheckInternalAsync(string scope, CancellationToken cancellationToken)
    {
        if (IsVersionUpdateActionRunning)
        {
            return;
        }

        IsVersionUpdateActionRunning = true;
        VersionUpdateStatusMessage = string.Empty;
        VersionUpdateErrorMessage = string.Empty;

        try
        {
            var checkOperation = await ExecuteVersionUpdateCheckAsync(scope, cancellationToken);
            var checkResult = await ApplyResultNoDialogAsync(checkOperation, scope, cancellationToken);
            if (checkResult is null)
            {
                VersionUpdateStatusMessage = RootTexts.GetOrDefault(
                    "Settings.VersionUpdate.Status.CheckFailed",
                    "Update check failed.");
                VersionUpdateErrorMessage = checkOperation.Message;
                return;
            }

            await ApplyVersionUpdateCheckResultAsync(checkResult, cancellationToken);
            if (!HasVersionUpdateErrorMessage)
            {
                VersionUpdateStatusMessage = checkOperation.Message;
            }
        }
        finally
        {
            IsVersionUpdateActionRunning = false;
        }
    }

    private async Task<UiOperationResult<VersionUpdateCheckResult>> ExecuteVersionUpdateCheckAsync(
        string scope,
        CancellationToken cancellationToken)
    {
        var policy = BuildVersionUpdatePolicy();
        return await Runtime.VersionUpdateFeatureService.CheckForUpdatesAsync(
            policy,
            UpdatePanelUiVersion,
            cancellationToken);
    }

    private async Task ApplyVersionUpdateCheckResultAsync(
        VersionUpdateCheckResult checkResult,
        CancellationToken cancellationToken)
    {
        var resolvedName = string.IsNullOrWhiteSpace(checkResult.ReleaseName)
            ? checkResult.TargetVersion
            : checkResult.ReleaseName;
        VersionUpdateName = resolvedName;
        VersionUpdateBody = checkResult.Body;
        VersionUpdatePackage = checkResult.PackageName ?? string.Empty;
        VersionUpdateDoNotShow = !checkResult.IsNewVersion;
        VersionUpdateIsFirstBoot = false;

        var persistResult = await Runtime.VersionUpdateFeatureService.SavePolicyAsync(
            BuildVersionUpdatePolicy(),
            cancellationToken);
        if (!persistResult.Success)
        {
            VersionUpdateStatusMessage = string.IsNullOrWhiteSpace(VersionUpdateStatusMessage)
                ? RootTexts.GetOrDefault(
                    "Settings.VersionUpdate.Status.PersistFailed",
                    "Update result was refreshed, but failed to save into configuration.")
                : $"{VersionUpdateStatusMessage} ({RootTexts.GetOrDefault("Settings.VersionUpdate.Status.PersistFailedSuffix", "failed to save result")})";
            VersionUpdateErrorMessage = persistResult.Message;
        }
    }

}
