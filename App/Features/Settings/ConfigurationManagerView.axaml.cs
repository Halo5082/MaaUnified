using System.IO;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MAAUnified.App.Features.Dialogs;
using MAAUnified.App.ViewModels.Settings;
using MAAUnified.Application.Models;

namespace MAAUnified.App.Features.Settings;

public partial class ConfigurationManagerView : UserControl
{
    public ConfigurationManagerView()
    {
        InitializeComponent();
    }

    private SettingsPageViewModel? VM => DataContext as SettingsPageViewModel;
    private string T(string key) => VM?.RootTexts[key] ?? key;

    private async void OnConfigurationProfileSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (VM is not null)
        {
            await VM.SwitchConfigurationProfileAsync();
        }
    }

    private async void OnCreateProfileClick(object? sender, RoutedEventArgs e)
    {
        if (VM is not null)
        {
            await VM.AddConfigurationProfileAsync();
        }
    }

    private async void OnDeleteCurrentProfileClick(object? sender, RoutedEventArgs e)
    {
        var vm = VM;
        if (vm is null)
        {
            return;
        }

        var target = vm.ConfigurationManagerSelectedProfile?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(target))
        {
            return;
        }

        var owner = TopLevel.GetTopLevel(this) as Window;
        var confirmed = await ShowWarningDialogAsync(
            owner,
            vm.Language,
            T("Settings.ConfigurationManager.Dialog.DeleteTitle"),
            string.Format(
                CultureInfo.CurrentCulture,
                T("Settings.ConfigurationManager.Dialog.DeleteMessage"),
                target),
            confirmText: T("Settings.Action.Delete"),
            cancelText: T("Settings.Action.Cancel"));
        if (!confirmed)
        {
            return;
        }

        await vm.DeleteConfigurationProfileAsync();
    }

    private async void OnExportAllProfilesClick(object? sender, RoutedEventArgs e)
    {
        var vm = VM;
        if (vm is null)
        {
            return;
        }

        var savePath = await PickSavePathAsync(
            T("Settings.ConfigurationManager.Dialog.ExportAllTitle"),
            BuildSuggestedFileName(
                T("Settings.ConfigurationManager.ExportAll.FileNamePrefix"),
                profileName: null));
        if (string.IsNullOrWhiteSpace(savePath))
        {
            return;
        }

        await vm.ExportAllConfigurationsAsync(savePath);
    }

    private async void OnExportCurrentProfileClick(object? sender, RoutedEventArgs e)
    {
        var vm = VM;
        if (vm is null)
        {
            return;
        }

        var savePath = await PickSavePathAsync(
            T("Settings.ConfigurationManager.Dialog.ExportCurrentTitle"),
            BuildSuggestedFileName(
                T("Settings.ConfigurationManager.ExportCurrent.FileNamePrefix"),
                vm.ConfigurationManagerSelectedProfile));
        if (string.IsNullOrWhiteSpace(savePath))
        {
            return;
        }

        await vm.ExportCurrentConfigurationAsync(savePath);
    }

    private async void OnImportProfilesClick(object? sender, RoutedEventArgs e)
    {
        var vm = VM;
        if (vm is null)
        {
            return;
        }
        var owner = TopLevel.GetTopLevel(this) as Window;
        var language = vm.Language;
        Func<string, string> text = key => vm.RootTexts[key];

        while (true)
        {
            var importPaths = await PickImportPathsAsync(T("Settings.ConfigurationManager.Dialog.ImportTitle"));
            if (importPaths.Count == 0)
            {
                return;
            }

            var analysis = ConfigurationImportSelectionAnalyzer.Analyze(importPaths, text);
            switch (analysis.Kind)
            {
                case ConfigurationImportSelectionKind.UnifiedConfig:
                    await vm.ImportConfigurationsAsync(analysis.UnifiedConfigPath!);
                    return;

                case ConfigurationImportSelectionKind.LegacyReady:
                    if (await TryRunLegacyImportAsync(vm, analysis, owner, language, text))
                    {
                        return;
                    }

                    continue;

                case ConfigurationImportSelectionKind.LegacyPartial:
                {
                    var forceImport = await ShowWarningDialogAsync(
                        owner,
                        language,
                        text("Settings.ConfigurationManager.Dialog.ImportLegacyTitle"),
                        analysis.Message,
                        confirmText: text("Settings.Action.ImportAnyway"),
                        cancelText: text("Settings.Action.ChooseAgain"));
                    if (!forceImport)
                    {
                        continue;
                    }

                    if (await TryRunLegacyImportAsync(vm, analysis, owner, language, text, allowPartialImport: true))
                    {
                        return;
                    }

                    continue;
                }

                default:
                {
                    var close = await ShowWarningDialogAsync(
                        owner,
                        language,
                        text("Settings.ConfigurationManager.Dialog.ImportConfigTitle"),
                        analysis.Message,
                        confirmText: text("Settings.Action.Close"),
                        cancelText: text("Settings.Action.ChooseAgain"));
                    if (!close)
                    {
                        continue;
                    }

                    return;
                }
            }
        }
    }

    private async Task<string?> PickSavePathAsync(string title, string suggestedFileName)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider is not { CanSave: true } storageProvider)
        {
            return null;
        }

        var file = await storageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = title,
                SuggestedFileName = suggestedFileName,
                DefaultExtension = "json",
                ShowOverwritePrompt = true,
                FileTypeChoices =
                [
                    CreateJsonFileType(),
                ],
            });
        return file?.TryGetLocalPath();
    }

    private async Task<IReadOnlyList<string>> PickImportPathsAsync(string title)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider is not { CanOpen: true } storageProvider)
        {
            return [];
        }

        var files = await storageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = title,
                AllowMultiple = true,
                FileTypeFilter =
                [
                    CreateJsonFileType(),
                ],
            });
        return files
            .Select(file => file.TryGetLocalPath())
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Cast<string>()
            .ToArray();
    }

    private static async Task<bool> TryRunLegacyImportAsync(
        SettingsPageViewModel vm,
        ConfigurationImportSelectionAnalysis analysis,
        Window? owner,
        string? language,
        Func<string, string> text,
        bool allowPartialImport = false)
    {
        var request = new LegacyImportRequest(
            LegacyConfigSnapshot.FromPaths(analysis.GuiNewPath, analysis.GuiPath),
            analysis.LegacyImportSource,
            ManualImport: true,
            AllowPartialImport: allowPartialImport);
        var report = await vm.ImportLegacyConfigurationsAsync(request);
        if (report.AppliedConfig)
        {
            return true;
        }

        if (report.DamagedFiles.Count > 0 && report.ImportedFiles.Count > 0 && !allowPartialImport)
        {
            var importUsable = await ShowWarningDialogAsync(
                owner,
                language,
                text("Settings.ConfigurationManager.Dialog.ImportLegacyTitle"),
                string.Format(
                    CultureInfo.CurrentCulture,
                    text("Settings.ConfigurationManager.Dialog.DamagedFiles"),
                    string.Join(", ", report.DamagedFiles)),
                confirmText: text("Settings.Action.ImportValidContent"),
                cancelText: text("Settings.Action.ChooseAgain"));
            if (!importUsable)
            {
                return false;
            }

            var retriedReport = await vm.ImportLegacyConfigurationsAsync(request with { AllowPartialImport = true });
            if (retriedReport.AppliedConfig)
            {
                return true;
            }

            if (retriedReport.DamagedFiles.Count > 0)
            {
                var close = await ShowWarningDialogAsync(
                    owner,
                    language,
                    text("Settings.ConfigurationManager.Dialog.ImportLegacyTitle"),
                    string.Format(
                        CultureInfo.CurrentCulture,
                        text("Settings.ConfigurationManager.Dialog.DamagedFilesNoImport"),
                        string.Join(", ", retriedReport.DamagedFiles)),
                    confirmText: text("Settings.Action.Close"),
                    cancelText: text("Settings.Action.ChooseAgain"));
                return close;
            }

            return true;
        }

        if (report.DamagedFiles.Count > 0)
        {
            var close = await ShowWarningDialogAsync(
                owner,
                language,
                text("Settings.ConfigurationManager.Dialog.ImportLegacyTitle"),
                string.Format(
                    CultureInfo.CurrentCulture,
                    text("Settings.ConfigurationManager.Dialog.DamagedFilesNoImport"),
                    string.Join(", ", report.DamagedFiles)),
                confirmText: text("Settings.Action.Close"),
                cancelText: text("Settings.Action.ChooseAgain"));
            return close;
        }

        return true;
    }

    private static async Task<bool> ShowWarningDialogAsync(
        Window? owner,
        string? language,
        string title,
        string message,
        string confirmText,
        string cancelText)
    {
        if (owner is null)
        {
            return false;
        }

        var dialog = new WarningConfirmDialogView();
        dialog.ApplyRequest(title, message, confirmText, cancelText, language);
        return await dialog.ShowDialog<bool>(owner);
    }

    private FilePickerFileType CreateJsonFileType() => new(T("Settings.ConfigurationManager.FileType.Json"))
    {
        Patterns = ["*.json"],
        MimeTypes = ["application/json"],
    };

    private static string BuildSuggestedFileName(string prefix, string? profileName)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var safePrefix = SanitizeFileNameSegment(prefix);
        var safeProfileName = SanitizeFileNameSegment(profileName);

        var baseName = string.Join(
            "-",
            new[] { "maa", safePrefix, safeProfileName, timestamp }
                .Where(segment => !string.IsNullOrWhiteSpace(segment)));
        return $"{baseName}.json";
    }

    private static string SanitizeFileNameSegment(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var invalidChars = Path.GetInvalidFileNameChars();
        return new string(value
                .Trim()
                .Select(c => invalidChars.Contains(c) ? '_' : c)
                .ToArray())
            .Trim();
    }
}
