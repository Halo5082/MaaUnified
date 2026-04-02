using MAAUnified.App.ViewModels.Infrastructure;
using MAAUnified.App.ViewModels.Toolbox;
using MAAUnified.Application.Services;
using MAAUnified.Application.Services.Localization;
using MAAUnified.Platform;

namespace MAAUnified.App.ViewModels.Advanced;

public sealed class TrayIntegrationPageViewModel : PageViewModelBase
{
    private readonly ToolboxLocalizationTextMap _texts = new();
    private string _trayMessageTitleTemplate = string.Empty;
    private string _trayMessageBodyTemplate = string.Empty;
    private string _capabilityProvider = string.Empty;
    private bool? _capabilitySupported;
    private string? _capabilityFallbackMode;
    private bool _trayVisible = true;
    private string _trayMessageTitle = string.Empty;
    private string _trayMessageBody = string.Empty;
    private string _capabilitySummary = string.Empty;

    public TrayIntegrationPageViewModel(MAAUnifiedRuntime runtime)
        : base(runtime)
    {
        RefreshLocalizedUiState();
    }

    public ToolboxLocalizationTextMap Texts => _texts;

    public bool TrayVisible
    {
        get => _trayVisible;
        set => SetProperty(ref _trayVisible, value);
    }

    public string TrayMessageTitle
    {
        get => _trayMessageTitle;
        set => SetProperty(ref _trayMessageTitle, value ?? string.Empty);
    }

    public string TrayMessageBody
    {
        get => _trayMessageBody;
        set => SetProperty(ref _trayMessageBody, value ?? string.Empty);
    }

    public string CapabilitySummary
    {
        get => _capabilitySummary;
        private set => SetProperty(ref _capabilitySummary, value);
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await RefreshCapabilitySummaryAsync(cancellationToken);
    }

    public async Task RefreshCapabilitySummaryAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await ApplyResultAsync(
            await Runtime.PlatformCapabilityService.GetSnapshotAsync(cancellationToken),
            "Advanced.TrayIntegration.QueryCapability",
            cancellationToken);
        if (snapshot is null)
        {
            return;
        }

        var tray = snapshot.Tray;
        UpdateCapabilitySummary(tray.Provider, tray.Supported, tray.FallbackMode);
    }

    public async Task ApplyTrayVisibilityAsync(CancellationToken cancellationToken = default)
    {
        await ApplyResultAsync(
            await Runtime.PlatformCapabilityService.SetTrayVisibleAsync(TrayVisible, cancellationToken),
            "Advanced.TrayIntegration.SetVisible",
            cancellationToken);
    }

    public async Task SendTrayMessageAsync(CancellationToken cancellationToken = default)
    {
        await ApplyResultAsync(
            await Runtime.PlatformCapabilityService.ShowTrayMessageAsync(TrayMessageTitle, TrayMessageBody, cancellationToken),
            "Advanced.TrayIntegration.Notify",
            cancellationToken);
    }

    public async Task SyncMenuStateAsync(CancellationToken cancellationToken = default)
    {
        var state = new TrayMenuState(
            StartEnabled: true,
            StopEnabled: true,
            OverlayEnabled: true,
            ForceShowEnabled: true,
            HideTrayEnabled: true);
        await ApplyResultAsync(
            await Runtime.PlatformCapabilityService.SetTrayMenuStateAsync(state, cancellationToken),
            "Advanced.TrayIntegration.SyncMenu",
            cancellationToken);
    }

    public void SetLanguage(string language)
    {
        var normalized = UiLanguageCatalog.Normalize(language);
        if (string.Equals(_texts.Language, normalized, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _texts.Language = normalized;
        RefreshLocalizedUiState();
    }

    private void RefreshLocalizedUiState()
    {
        OnPropertyChanged(nameof(Texts));

        var previousTitleTemplate = _trayMessageTitleTemplate;
        var previousBodyTemplate = _trayMessageBodyTemplate;
        _trayMessageTitleTemplate = T("Toolbox.Advanced.Tray.DefaultMessageTitle", "MAAUnified");
        _trayMessageBodyTemplate = T("Toolbox.Advanced.Tray.DefaultMessageBody", "Tray integration test.");

        if (string.IsNullOrWhiteSpace(TrayMessageTitle)
            || string.Equals(TrayMessageTitle, previousTitleTemplate, StringComparison.Ordinal))
        {
            TrayMessageTitle = _trayMessageTitleTemplate;
        }

        if (string.IsNullOrWhiteSpace(TrayMessageBody)
            || string.Equals(TrayMessageBody, previousBodyTemplate, StringComparison.Ordinal))
        {
            TrayMessageBody = _trayMessageBodyTemplate;
        }

        if (_capabilitySupported.HasValue)
        {
            CapabilitySummary = string.Format(
                T("Toolbox.Advanced.Capability.Summary", "Provider: {0}; Supported: {1}; Fallback: {2}"),
                string.IsNullOrWhiteSpace(_capabilityProvider)
                    ? T("Toolbox.Advanced.Capability.Provider.Unknown", "unknown")
                    : _capabilityProvider,
                _capabilitySupported.Value
                    ? T("Toolbox.Advanced.Capability.Supported.True", "Yes")
                    : T("Toolbox.Advanced.Capability.Supported.False", "No"),
                string.IsNullOrWhiteSpace(_capabilityFallbackMode)
                    ? T("Toolbox.Advanced.Capability.Fallback.None", "None")
                    : _capabilityFallbackMode);
        }
    }

    private void UpdateCapabilitySummary(string provider, bool supported, string? fallbackMode)
    {
        _capabilityProvider = provider ?? string.Empty;
        _capabilitySupported = supported;
        _capabilityFallbackMode = fallbackMode;
        RefreshLocalizedUiState();
    }

    private string T(string key, string fallback)
    {
        return _texts.GetOrDefault(key, fallback);
    }
}
