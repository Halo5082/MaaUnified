using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using MAAUnified.App.Infrastructure;
using MAAUnified.Application.Models;

namespace MAAUnified.App.Features.Dialogs;

public partial class ProcessPickerDialogView : Window
{
    private readonly ObservableCollection<ProcessPickerItem> _items = [];
    private ProcessPickerDialogRequest? _request;

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
        ConfirmButton.Content = request.ConfirmText;
        CancelButton.Content = request.CancelText;
        ApplyItems(request.Items, request.SelectedId);
        RefreshButton.IsVisible = request.RefreshItemsAsync is not null;
        RefreshButton.IsEnabled = request.RefreshItemsAsync is not null;
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
        RefreshButton.Content = "Refreshing...";
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
            RefreshButton.Content = "Refresh";
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
}
