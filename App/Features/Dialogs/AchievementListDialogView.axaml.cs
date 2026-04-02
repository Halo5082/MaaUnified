using System.Collections.ObjectModel;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Interactivity;
using MAAUnified.App.Infrastructure;
using MAAUnified.App.ViewModels.Infrastructure;
using MAAUnified.Application.Models;

namespace MAAUnified.App.Features.Dialogs;

public partial class AchievementListDialogView : Window, IDialogChromeAware
{
    private const string ProgressFormatKey = "Achievement.ProgressFormat";
    private const string NewBadgeTextKey = "Achievement.NewBadgeText";

    private readonly RootLocalizationTextMap _texts = new("Root.Localization.Dialog.AchievementList");
    private readonly ObservableCollection<AchievementListDisplayItem> _visibleItems = [];
    private IReadOnlyList<AchievementListItem> _allItems = [];
    private string _filterWatermarkSnapshot = "Filter";
    private string _progressFormat = "Progress: {0}";
    private string _newBadgeText = "NEW";

    public AchievementListDialogView()
    {
        InitializeComponent();
        WindowVisuals.ApplyDefaultIcon(this);
        AchievementList.ItemsSource = _visibleItems;
    }

    public void ApplyRequest(AchievementListDialogRequest request)
    {
        Title = request.Title;
        DialogTitleText.Text = request.Title;
        ConfirmButton.Content = request.ConfirmText;
        CancelButton.Content = request.CancelText;
        _allItems = request.Items;
        _filterWatermarkSnapshot = request.FilterWatermark;
        _progressFormat = Text("Settings.Achievement.Dialog.ProgressFormat", "Progress: {0}");
        FilterInput.Watermark = _filterWatermarkSnapshot;
        FilterInput.Text = request.InitialFilter ?? string.Empty;
        ApplyFilter(FilterInput.Text ?? string.Empty, preserveSelection: false);
    }

    public AchievementListDialogPayload BuildPayload()
    {
        var selectedIds = (AchievementList.SelectedItems ?? Array.Empty<object>())
            .OfType<AchievementListDisplayItem>()
            .Select(item => item.Id)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        return new AchievementListDialogPayload(FilterInput.Text ?? string.Empty, selectedIds);
    }

    public void ApplyDialogChrome(DialogChromeSnapshot chrome)
    {
        Title = chrome.Title;
        DialogTitleText.Text = chrome.GetNamedTextOrDefault(DialogTextCatalog.ChromeKeys.SectionTitle, chrome.Title);
        ConfirmButton.Content = chrome.ConfirmText ?? ConfirmButton.Content;
        CancelButton.Content = chrome.CancelText ?? CancelButton.Content;
        FilterInput.Watermark = chrome.GetNamedTextOrDefault(DialogTextCatalog.ChromeKeys.FilterWatermark, _filterWatermarkSnapshot);
        _progressFormat = chrome.GetNamedTextOrDefault(
            ProgressFormatKey,
            Text("Settings.Achievement.Dialog.ProgressFormat", "Progress: {0}"));
        _newBadgeText = chrome.GetNamedTextOrDefault(NewBadgeTextKey, "NEW");
        ApplyFilter(FilterInput.Text ?? string.Empty, preserveSelection: true);
    }

    private void ApplyFilter(string filter, bool preserveSelection)
    {
        var selectedIds = preserveSelection
            ? (AchievementList.SelectedItems ?? Array.Empty<object>())
                .OfType<AchievementListDisplayItem>()
                .Select(item => item.Id)
                .ToHashSet(StringComparer.Ordinal)
            : null;

        _visibleItems.Clear();
        var normalized = (filter ?? string.Empty).Trim();
        IEnumerable<AchievementListItem> filtered = _allItems;
        if (normalized.Length > 0)
        {
            filtered = filtered.Where(item =>
                item.Title.Contains(normalized, StringComparison.OrdinalIgnoreCase)
                || item.Description.Contains(normalized, StringComparison.OrdinalIgnoreCase)
                || item.Status.Contains(normalized, StringComparison.OrdinalIgnoreCase)
                || item.Conditions.Contains(normalized, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var item in filtered)
        {
            _visibleItems.Add(new AchievementListDisplayItem(item, _newBadgeText, _progressFormat));
        }

        if (selectedIds is null || AchievementList.SelectedItems is not { } selectedItems)
        {
            return;
        }

        selectedItems.Clear();
        foreach (var item in _visibleItems.Where(display => selectedIds.Contains(display.Id)))
        {
            selectedItems.Add(item);
        }
    }

    private void OnFilterChanged(object? sender, TextChangedEventArgs e)
    {
        ApplyFilter(FilterInput.Text ?? string.Empty, preserveSelection: true);
    }

    private void OnConfirmClick(object? sender, RoutedEventArgs e)
    {
        Close(DialogReturnSemantic.Confirm);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(DialogReturnSemantic.Cancel);
    }

    private string Text(string key, string fallback)
    {
        _texts.Language = App.Runtime.UiLanguageCoordinator.CurrentLanguage;
        return _texts.GetOrDefault(key, fallback);
    }

    private sealed class AchievementListDisplayItem
    {
        public AchievementListDisplayItem(AchievementListItem source, string newBadgeText, string progressFormat)
        {
            Source = source;
            NewBadgeText = newBadgeText;
            ProgressText = string.Format(CultureInfo.CurrentCulture, progressFormat, source.Progress);
        }

        public AchievementListItem Source { get; }

        public string Id => Source.Id;

        public string Title => Source.Title;

        public string Description => Source.Description;

        public string Status => Source.Status;

        public string Conditions => Source.Conditions;

        public bool IsUnlocked => Source.IsUnlocked;

        public bool IsHidden => Source.IsHidden;

        public bool IsProgressive => Source.IsProgressive;

        public bool ShowProgress => Source.ShowProgress;

        public int Progress => Source.Progress;

        public int Target => Source.Target;

        public string MedalColor => Source.MedalColor;

        public string UnlockedAtText => Source.UnlockedAtText;

        public bool IsNewUnlock => Source.IsNewUnlock;

        public bool CanShow => Source.CanShow;

        public int SortCategory => Source.SortCategory;

        public string SortGroup => Source.SortGroup;

        public int SortGroupIndex => Source.SortGroupIndex;

        public string NewBadgeText { get; }

        public string ProgressText { get; }
    }
}
