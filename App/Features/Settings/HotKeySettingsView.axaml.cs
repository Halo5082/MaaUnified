using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using MAAUnified.App.ViewModels.Settings;
using MAAUnified.Platform;

namespace MAAUnified.App.Features.Settings;

public partial class HotKeySettingsView : UserControl
{
    public HotKeySettingsView()
    {
        InitializeComponent();
    }

    private SettingsPageViewModel? VM => DataContext as SettingsPageViewModel;

    private async void OnApplyHotkeysClick(object? sender, RoutedEventArgs e)
    {
        if (VM is not null)
        {
            await VM.RegisterHotkeysAsync();
        }
    }

    private void OnBeginCaptureClick(object? sender, RoutedEventArgs e)
    {
        if (VM is null
            || sender is not Button { Tag: string hotkeyName })
        {
            return;
        }

        VM.BeginHotkeyCapture(hotkeyName);
        GetCaptureBox(hotkeyName)?.Focus();
    }

    private void OnClearHotkeyClick(object? sender, RoutedEventArgs e)
    {
        if (VM is null
            || sender is not Button { Tag: string hotkeyName })
        {
            return;
        }

        VM.ClearHotkeyBinding(hotkeyName);
    }

    private void OnHotkeyCaptureKeyDown(object? sender, KeyEventArgs e)
    {
        if (VM is null
            || sender is not TextBox { Tag: string hotkeyName })
        {
            return;
        }

        var capture = HotkeyGestureCodec.Capture(e.Key, e.KeyModifiers);
        VM.HandleHotkeyCapture(hotkeyName, capture);
        e.Handled = true;
    }

    private TextBox? GetCaptureBox(string hotkeyName)
    {
        return string.Equals(hotkeyName, HotkeyConfigurationCodec.ShowGuiHotkeyName, StringComparison.OrdinalIgnoreCase)
            ? ShowGuiCaptureBox
            : string.Equals(hotkeyName, HotkeyConfigurationCodec.LinkStartHotkeyName, StringComparison.OrdinalIgnoreCase)
                ? LinkStartCaptureBox
                : null;
    }
}
