namespace MAAUnified.Application.Services.Localization;

public interface IUiLocalizer
{
    string Language { get; set; }

    string this[string key] { get; }

    string GetText(
        string key,
        string scope,
        Action<LocalizationFallbackInfo>? fallbackReporter = null);

    string GetTextForLanguage(
        string language,
        string key,
        string scope,
        Action<LocalizationFallbackInfo>? fallbackReporter = null);

    string GetOrDefault(
        string key,
        string fallback,
        string scope,
        Action<LocalizationFallbackInfo>? fallbackReporter = null);

    string GetOrDefaultForLanguage(
        string language,
        string key,
        string fallback,
        string scope,
        Action<LocalizationFallbackInfo>? fallbackReporter = null);
}
