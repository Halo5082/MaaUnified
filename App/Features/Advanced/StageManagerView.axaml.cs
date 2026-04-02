using Avalonia.Controls;
using Avalonia.Interactivity;
using MAAUnified.App.ViewModels.Advanced;

namespace MAAUnified.App.Features.Advanced;

public partial class StageManagerView : UserControl
{
    public StageManagerView()
    {
        InitializeComponent();
    }

    private StageManagerPageViewModel? VM => DataContext as StageManagerPageViewModel;

    private async void OnValidateClick(object? sender, RoutedEventArgs e)
    {
        if (VM is not null)
        {
            await VM.ValidateAsync();
        }
    }

    private async void OnRefreshLocalClick(object? sender, RoutedEventArgs e)
    {
        if (VM is not null)
        {
            await VM.RefreshLocalAsync();
        }
    }

    private async void OnRefreshWebClick(object? sender, RoutedEventArgs e)
    {
        if (VM is not null)
        {
            await VM.RefreshWebAsync();
        }
    }

    private async void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        if (VM is not null)
        {
            await VM.SaveAsync();
        }
    }
}
