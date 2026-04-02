using System.Text.Json.Nodes;
using MAAUnified.Application.Configuration;
using MAAUnified.Application.Models;
using MAAUnified.Compat.Constants;

namespace MAAUnified.Application.Services.Localization;

public interface IUiLanguageCoordinator
{
    string CurrentLanguage { get; }

    event EventHandler<UiLanguageChangedEventArgs>? LanguageChanged;

    Task<UiOperationResult<string>> ChangeLanguageAsync(string targetLanguage, CancellationToken cancellationToken = default);
}

public sealed class UiLanguageChangedEventArgs : EventArgs
{
    public UiLanguageChangedEventArgs(string previousLanguage, string currentLanguage)
    {
        PreviousLanguage = UiLanguageCatalog.Normalize(previousLanguage);
        CurrentLanguage = UiLanguageCatalog.Normalize(currentLanguage);
    }

    public string PreviousLanguage { get; }

    public string CurrentLanguage { get; }
}

public sealed class UiLanguageCoordinator : IUiLanguageCoordinator
{
    private readonly UnifiedConfigurationService _configurationService;
    private readonly SemaphoreSlim _changeSemaphore = new(1, 1);
    private string _currentLanguage;

    public UiLanguageCoordinator(UnifiedConfigurationService configurationService)
    {
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _currentLanguage = ReadPersistedLanguage(configurationService.CurrentConfig);
    }

    public string CurrentLanguage => _currentLanguage;

    public event EventHandler<UiLanguageChangedEventArgs>? LanguageChanged;

    public async Task<UiOperationResult<string>> ChangeLanguageAsync(string targetLanguage, CancellationToken cancellationToken = default)
    {
        var normalizedTarget = UiLanguageCatalog.Normalize(targetLanguage);
        await _changeSemaphore.WaitAsync(cancellationToken);
        try
        {
            var previousLanguage = _currentLanguage;
            if (string.Equals(previousLanguage, normalizedTarget, StringComparison.OrdinalIgnoreCase))
            {
                return UiOperationResult<string>.Ok(previousLanguage, $"Language already set to {previousLanguage}.");
            }

            var config = _configurationService.CurrentConfig;
            var persistedLanguage = ReadPersistedLanguage(config);
            if (string.Equals(persistedLanguage, normalizedTarget, StringComparison.OrdinalIgnoreCase))
            {
                _currentLanguage = normalizedTarget;
                LanguageChanged?.Invoke(this, new UiLanguageChangedEventArgs(previousLanguage, normalizedTarget));
                return UiOperationResult<string>.Ok(normalizedTarget, $"Language synchronized to persisted setting {normalizedTarget}.");
            }

            var hadPreviousNode = config.GlobalValues.TryGetValue(ConfigurationKeys.Localization, out var previousNode);
            try
            {
                config.GlobalValues[ConfigurationKeys.Localization] = JsonValue.Create(normalizedTarget);
                await _configurationService.SaveAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                if (hadPreviousNode)
                {
                    config.GlobalValues[ConfigurationKeys.Localization] = previousNode?.DeepClone();
                }
                else
                {
                    config.GlobalValues.Remove(ConfigurationKeys.Localization);
                }

                _configurationService.RevalidateCurrentConfig(logIssues: false);
                return UiOperationResult<string>.Fail(
                    UiErrorCode.SettingsSaveFailed,
                    $"Failed to save language setting: {ex.Message}",
                    ex.ToString());
            }

            _currentLanguage = normalizedTarget;
            LanguageChanged?.Invoke(this, new UiLanguageChangedEventArgs(previousLanguage, normalizedTarget));
            return UiOperationResult<string>.Ok(normalizedTarget, $"Language switched to {normalizedTarget}.");
        }
        finally
        {
            _changeSemaphore.Release();
        }
    }

    private static string ReadPersistedLanguage(UnifiedConfig config)
    {
        if (config.GlobalValues.TryGetValue(ConfigurationKeys.Localization, out var node)
            && node is JsonValue value
            && value.TryGetValue(out string? language)
            && !string.IsNullOrWhiteSpace(language))
        {
            return UiLanguageCatalog.Normalize(language);
        }

        return UiLanguageCatalog.DefaultLanguage;
    }
}
