using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using System.Globalization;
using MAAUnified.App.Features.Dialogs;
using MAAUnified.App.ViewModels.Infrastructure;
using MAAUnified.Application.Models;
using MAAUnified.Application.Services;
using MAAUnified.Application.Services.Localization;
using MAAUnified.Platform;

namespace MAAUnified.App.Services;

public sealed class AvaloniaPostActionPromptService : IPostActionPromptService
{
    private readonly IClassicDesktopStyleApplicationLifetime _desktopLifetime;

    public AvaloniaPostActionPromptService(IClassicDesktopStyleApplicationLifetime desktopLifetime)
    {
        _desktopLifetime = desktopLifetime;
    }

    public async Task<UiOperationResult> ConfirmPowerActionAsync(
        PostActionPromptRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (Dispatcher.UIThread.CheckAccess())
        {
            return await ShowDialogAsync(request, cancellationToken);
        }

        return await await Dispatcher.UIThread.InvokeAsync(
            () => ShowDialogAsync(request, cancellationToken),
            DispatcherPriority.Normal,
            cancellationToken);
    }

    private async Task<UiOperationResult> ShowDialogAsync(
        PostActionPromptRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var owner = ResolveOwnerWindow();
        if (owner is null)
        {
            return UiOperationResult.Fail(
                UiErrorCode.PostActionExecutionFailed,
                "Unable to show power action confirmation dialog because no desktop window is available.");
        }

        var dialog = new WarningConfirmDialogView();
        var seconds = Math.Max(1, (int)Math.Ceiling(request.Countdown.TotalSeconds));
        dialog.ApplyRequest(
            BuildTitle(request.Action, request.Language),
            BuildMessage(request.Action, seconds, request.Language),
            confirmText: BuildConfirmText(request.Language),
            cancelText: DialogTextCatalog.WarningDialogCancelButton(request.Language),
            language: request.Language,
            countdownSeconds: seconds);

        var confirmed = await dialog.ShowDialog<bool>(owner);
        return confirmed
            ? UiOperationResult.Ok($"{request.Action} confirmed.")
            : UiOperationResult.Cancelled($"{request.Action} cancelled.");
    }

    private Window? ResolveOwnerWindow()
    {
        return _desktopLifetime.Windows.LastOrDefault(window => window.IsActive)
               ?? _desktopLifetime.MainWindow;
    }

    private static string BuildTitle(PostActionType action, string? language)
    {
        var localizer = UiLocalizer.Create(UiLanguageCatalog.Normalize(language));
        return action switch
        {
            PostActionType.Shutdown => localizer.GetOrDefault("PostAction.Dialog.Title.Shutdown", "Confirm Shutdown", "App.PostActionPrompt"),
            PostActionType.Hibernate => localizer.GetOrDefault("PostAction.Dialog.Title.Hibernate", "Confirm Hibernate", "App.PostActionPrompt"),
            PostActionType.Sleep => localizer.GetOrDefault("PostAction.Dialog.Title.Sleep", "Confirm Sleep", "App.PostActionPrompt"),
            _ => DialogTextCatalog.WarningDialogTitle(language),
        };
    }

    private static string BuildMessage(PostActionType action, int seconds, string? language)
    {
        var localizer = UiLocalizer.Create(UiLanguageCatalog.Normalize(language));
        return action switch
        {
            PostActionType.Shutdown => string.Format(
                CultureInfo.CurrentCulture,
                localizer.GetOrDefault("PostAction.Dialog.Message.Shutdown", "MAA will shut down this computer in {0} seconds unless you cancel.", "App.PostActionPrompt"),
                seconds),
            PostActionType.Hibernate => string.Format(
                CultureInfo.CurrentCulture,
                localizer.GetOrDefault("PostAction.Dialog.Message.Hibernate", "MAA will hibernate this computer in {0} seconds unless you cancel.", "App.PostActionPrompt"),
                seconds),
            PostActionType.Sleep => string.Format(
                CultureInfo.CurrentCulture,
                localizer.GetOrDefault("PostAction.Dialog.Message.Sleep", "MAA will put this computer to sleep in {0} seconds unless you cancel.", "App.PostActionPrompt"),
                seconds),
            _ => DialogTextCatalog.WarningDialogPrompt(language),
        };
    }

    private static string BuildConfirmText(string? language)
    {
        return UiLocalizer.Create(UiLanguageCatalog.Normalize(language))
            .GetOrDefault("PostAction.Dialog.ConfirmNow", "Run Now", "App.PostActionPrompt");
    }
}
