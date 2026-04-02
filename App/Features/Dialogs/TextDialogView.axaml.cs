using Avalonia.Controls;
using Avalonia.Interactivity;
using MAAUnified.App.Infrastructure;
using MAAUnified.App.ViewModels.Infrastructure;
using MAAUnified.Application.Models;

namespace MAAUnified.App.Features.Dialogs;

public partial class TextDialogView : Window, IDialogChromeAware
{
    private string _promptSnapshot = string.Empty;

    public TextDialogView()
    {
        InitializeComponent();
        WindowVisuals.ApplyDefaultIcon(this);
        Opened += (_, _) =>
        {
            InputBox.Focus();
            InputBox.SelectAll();
        };
    }

    public void ApplyRequest(TextDialogRequest request)
    {
        Title = request.Title;
        _promptSnapshot = request.Prompt;
        PromptText.Text = _promptSnapshot;
        InputBox.Text = request.DefaultText;
        InputBox.AcceptsReturn = request.MultiLine;
        DialogTitleText.Text = Title;
        ConfirmButton.Content = request.ConfirmText;
        CancelButton.Content = request.CancelText;
    }

    public TextDialogPayload BuildPayload()
    {
        return new TextDialogPayload(InputBox.Text ?? string.Empty);
    }

    private void OnConfirmClick(object? sender, RoutedEventArgs e)
    {
        Close(DialogReturnSemantic.Confirm);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(DialogReturnSemantic.Cancel);
    }

    public void ApplyDialogChrome(DialogChromeSnapshot chrome)
    {
        Title = chrome.Title;
        DialogTitleText.Text = chrome.GetNamedTextOrDefault(DialogTextCatalog.ChromeKeys.SectionTitle, chrome.Title);
        PromptText.Text = chrome.GetNamedTextOrDefault(DialogTextCatalog.ChromeKeys.Prompt, _promptSnapshot);
        ConfirmButton.Content = chrome.ConfirmText ?? ConfirmButton.Content;
        CancelButton.Content = chrome.CancelText ?? CancelButton.Content;
    }
}
