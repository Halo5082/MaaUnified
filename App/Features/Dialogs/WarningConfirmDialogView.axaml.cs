using System.Threading;
using Avalonia.Controls;
using Avalonia.Interactivity;
using MAAUnified.App.Infrastructure;
using MAAUnified.App.ViewModels.Infrastructure;
using MAAUnified.Application.Models;

namespace MAAUnified.App.Features.Dialogs;

public partial class WarningConfirmDialogView : Window, IDialogChromeAware
{
    private CancellationTokenSource? _countdownCts;
    private string _confirmSnapshot = string.Empty;
    private string _titleSnapshot = string.Empty;
    private string _messageSnapshot = string.Empty;
    private string _cancelSnapshot = string.Empty;
    private int _countdownSeconds;
    private int _remainingCountdownSeconds;

    public WarningConfirmDialogView()
    {
        InitializeComponent();
        WindowVisuals.ApplyDefaultIcon(this);
        Opened += OnOpened;
        Closed += OnClosed;
    }

    public void ApplyRequest(
        string title,
        string message,
        string confirmText = "",
        string cancelText = "",
        string? language = null,
        int countdownSeconds = 0)
    {
        StopCountdown();
        var effectiveLanguage = language ?? "en-us";
        _titleSnapshot = string.IsNullOrWhiteSpace(title) ? DialogTextCatalog.WarningDialogTitle(effectiveLanguage) : title.Trim();
        _messageSnapshot = string.IsNullOrWhiteSpace(message) ? DialogTextCatalog.WarningDialogPrompt(effectiveLanguage) : message.Trim();
        _confirmSnapshot = string.IsNullOrWhiteSpace(confirmText)
            ? DialogTextCatalog.WarningDialogConfirmButton(effectiveLanguage)
            : confirmText;
        _cancelSnapshot = string.IsNullOrWhiteSpace(cancelText)
            ? DialogTextCatalog.WarningDialogCancelButton(effectiveLanguage)
            : cancelText;
        _countdownSeconds = Math.Max(0, countdownSeconds);
        _remainingCountdownSeconds = _countdownSeconds;
        Title = _titleSnapshot;
        TitleText.Text = _titleSnapshot;
        PromptText.Text = _messageSnapshot;
        CancelButton.Content = _cancelSnapshot;
        UpdateConfirmButtonText(_remainingCountdownSeconds);
    }

    private void OnConfirmClick(object? sender, RoutedEventArgs e)
    {
        StopCountdown();
        Close(true);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        StopCountdown();
        Close(false);
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        if (_countdownSeconds > 0)
        {
            StartCountdown();
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        StopCountdown();
        Closed -= OnClosed;
    }

    private async void StartCountdown()
    {
        StopCountdown();
        _countdownCts = new CancellationTokenSource();

        try
        {
            for (var remaining = _countdownSeconds; remaining > 0; remaining--)
            {
                _remainingCountdownSeconds = remaining;
                UpdateConfirmButtonText(remaining);
                await Task.Delay(TimeSpan.FromSeconds(1), _countdownCts.Token);
            }

            if (_countdownCts.IsCancellationRequested)
            {
                return;
            }

            _remainingCountdownSeconds = 0;
            UpdateConfirmButtonText(0);
            Close(true);
        }
        catch (OperationCanceledException)
        {
            // Countdown cancelled by user interaction or dialog close.
        }
    }

    private void StopCountdown()
    {
        _remainingCountdownSeconds = 0;
        _countdownCts?.Cancel();
        _countdownCts?.Dispose();
        _countdownCts = null;
    }

    private void UpdateConfirmButtonText(int remainingSeconds)
    {
        var confirmText = _confirmSnapshot;
        ConfirmButton.Content = remainingSeconds > 0
            ? $"{confirmText} ({remainingSeconds}s)"
            : confirmText;
    }

    public void ApplyDialogChrome(DialogChromeSnapshot chrome)
    {
        Title = chrome.Title;
        TitleText.Text = chrome.GetNamedTextOrDefault(DialogTextCatalog.ChromeKeys.SectionTitle, chrome.Title);
        PromptText.Text = chrome.GetNamedTextOrDefault(DialogTextCatalog.ChromeKeys.Prompt, _messageSnapshot);
        _confirmSnapshot = chrome.ConfirmText ?? _confirmSnapshot;
        _cancelSnapshot = chrome.CancelText ?? _cancelSnapshot;
        CancelButton.Content = _cancelSnapshot;
        UpdateConfirmButtonText(_remainingCountdownSeconds);
    }
}
