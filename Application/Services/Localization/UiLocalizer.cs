namespace MAAUnified.Application.Services.Localization;

public sealed class UiLocalizer : IUiLocalizer
{
    private readonly UiLocalizationResourceManager _resourceManager;
    private string _language;

    public UiLocalizer()
        : this(UiLocalizationResourceManager.Shared, UiLanguageCatalog.DefaultLanguage)
    {
    }

    public UiLocalizer(string language)
        : this(UiLocalizationResourceManager.Shared, language)
    {
    }

    public UiLocalizer(UiLocalizationResourceManager resourceManager, string language = UiLanguageCatalog.DefaultLanguage)
    {
        _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
        _language = UiLanguageCatalog.Normalize(language);
    }

    public static UiLocalizer Shared { get; } = new();

    public static UiLocalizer Create(string language)
    {
        return new UiLocalizer(UiLocalizationResourceManager.Shared, language);
    }

    public string Language
    {
        get => _language;
        set => _language = UiLanguageCatalog.Normalize(value);
    }

    public string this[string key] => GetText(key, "Ui.Localization");

    public string GetText(
        string key,
        string scope,
        Action<LocalizationFallbackInfo>? fallbackReporter = null)
    {
        return GetTextForLanguage(Language, key, scope, fallbackReporter);
    }

    public string GetTextForLanguage(
        string language,
        string key,
        string scope,
        Action<LocalizationFallbackInfo>? fallbackReporter = null)
    {
        var result = _resourceManager.Lookup(language, key);
        ReportFallbackIfNeeded(scope, key, fallbackReporter, result);
        return result.Value;
    }

    public string GetOrDefault(
        string key,
        string fallback,
        string scope,
        Action<LocalizationFallbackInfo>? fallbackReporter = null)
    {
        return GetOrDefaultForLanguage(Language, key, fallback, scope, fallbackReporter);
    }

    public string GetOrDefaultForLanguage(
        string language,
        string key,
        string fallback,
        string scope,
        Action<LocalizationFallbackInfo>? fallbackReporter = null)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return fallback;
        }

        var value = GetTextForLanguage(language, key, scope, fallbackReporter);
        return string.Equals(value, key, StringComparison.Ordinal) ? fallback : value;
    }

    private static void ReportFallbackIfNeeded(
        string scope,
        string key,
        Action<LocalizationFallbackInfo>? fallbackReporter,
        UiLocalizationLookupResult result)
    {
        if (!result.IsFallback || fallbackReporter is null)
        {
            return;
        }

        fallbackReporter(
            new LocalizationFallbackInfo(
                Scope: scope,
                Language: result.RequestedLanguage,
                Key: key,
                FallbackSource: result.FallbackSource));
    }
}
