using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using MAAUnified.App.Infrastructure;
using MAAUnified.App.ViewModels.Infrastructure;
using MAAUnified.Application.Models;

namespace MAAUnified.App.Features.Dialogs;

public partial class ProcessPickerDialogView : Window, IDialogChromeAware
{
    private readonly ObservableCollection<ProcessPickerItem> _items = [];
    private ProcessPickerDialogRequest? _request;
    private bool _isRefreshing;
    private string _refreshButtonText = "Refresh";
    private string _refreshingButtonText = "Refreshing...";

    public ProcessPickerDialogView()
    {
        InitializeComponent();
        WindowVisuals.ApplyDefaultIcon(this);
        ProcessList.ItemsSource = _items;
    }

    public void ApplyRequest(ProcessPickerDialogRequest request)
    {
        _request = request;
        Title = request.Title;
        DialogTitleText.Text = request.Title;
        ConfirmButton.Content = request.ConfirmText;
        CancelButton.Content = request.CancelText;
        _refreshButtonText = RefreshButton.Content?.ToString() ?? "Refresh";
        _refreshingButtonText = _refreshButtonText;
        ApplyItems(request.Items, request.SelectedId);
        RefreshButton.IsVisible = request.RefreshItemsAsync is not null;
        RefreshButton.IsEnabled = request.RefreshItemsAsync is not null;
        _isRefreshing = false;
        RefreshButton.Content = _refreshButtonText;
    }

    public ProcessPickerDialogPayload? BuildPayload()
    {
        if (ProcessList.SelectedItem is not ProcessPickerItem selected)
        {
            return null;
        }

        return new ProcessPickerDialogPayload(selected.Id, selected.DisplayName);
    }

    private async void OnRefreshClick(object? sender, RoutedEventArgs e)
    {
        if (_request?.RefreshItemsAsync is not { } refreshItemsAsync)
        {
            return;
        }

        var selectedId = (ProcessList.SelectedItem as ProcessPickerItem)?.Id;
        RefreshButton.IsEnabled = false;
        _isRefreshing = true;
        RefreshButton.Content = _refreshingButtonText;
        try
        {
            var refreshedItems = await refreshItemsAsync(CancellationToken.None);
            ApplyItems(refreshedItems, selectedId);
        }
        catch
        {
            // Keep existing items/selection when refresh fails.
        }
        finally
        {
            _isRefreshing = false;
            RefreshButton.Content = _refreshButtonText;
            RefreshButton.IsEnabled = true;
        }
    }

    private void OnConfirmClick(object? sender, RoutedEventArgs e)
    {
        if (BuildPayload() is null)
        {
            return;
        }

        Close(DialogReturnSemantic.Confirm);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(DialogReturnSemantic.Cancel);
    }

    private void ApplyItems(IReadOnlyList<ProcessPickerItem> items, string? selectedId)
    {
        _items.Clear();
        foreach (var item in items.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.DisplayName, StringComparer.OrdinalIgnoreCase))
        {
            _items.Add(item);
        }

        if (!string.IsNullOrWhiteSpace(selectedId))
        {
            ProcessList.SelectedItem = _items.FirstOrDefault(i => string.Equals(i.Id, selectedId, StringComparison.Ordinal));
        }

        ProcessList.SelectedItem ??= _items.FirstOrDefault();
        ConfirmButton.IsEnabled = _items.Count > 0;
    }

    public void ApplyDialogChrome(DialogChromeSnapshot chrome)
    {
        Title = chrome.Title;
        DialogTitleText.Text = chrome.GetNamedTextOrDefault(DialogTextCatalog.ChromeKeys.SectionTitle, chrome.Title);
        ConfirmButton.Content = chrome.ConfirmText ?? ConfirmButton.Content;
        CancelButton.Content = chrome.CancelText ?? CancelButton.Content;
        _refreshButtonText = chrome.GetNamedTextOrDefault(DialogTextCatalog.ChromeKeys.RefreshButton, _refreshButtonText);
        _refreshingButtonText = chrome.GetNamedTextOrDefault(DialogTextCatalog.ChromeKeys.RefreshingButton, _refreshButtonText);
        RefreshButton.Content = _isRefreshing ? _refreshingButtonText : _refreshButtonText;
    }
}
