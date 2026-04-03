using System.Collections;
using System.Collections.Concurrent;
using System.Globalization;
using System.Resources;
using System.Text.RegularExpressions;

namespace MAAUnified.Application.Services.Localization;

internal readonly record struct UiLocalizationLookupResult(
    string Value,
    string RequestedLanguage,
    string FallbackSource)
{
    public bool IsFallback => !string.Equals(FallbackSource, "none", StringComparison.OrdinalIgnoreCase);
}

public sealed class UiLocalizationResourceManager
{
    private const string ResourceBaseName = "MAAUnified.Application.Resources.Localization.UiTexts";
    private static readonly Regex InlineKeyPattern = new(@"\{key=([^}]+)\}", RegexOptions.Compiled);
    private static readonly IReadOnlyDictionary<string, CultureInfo> CultureMap =
        new Dictionary<string, CultureInfo>(StringComparer.OrdinalIgnoreCase)
        {
            ["zh-cn"] = CultureInfo.InvariantCulture,
            ["en-us"] = CultureInfo.GetCultureInfo("en-US"),
            ["zh-tw"] = CultureInfo.GetCultureInfo("zh-TW"),
            ["ja-jp"] = CultureInfo.GetCultureInfo("ja-JP"),
            ["ko-kr"] = CultureInfo.GetCultureInfo("ko-KR"),
            ["pallas"] = CultureInfo.GetCultureInfo("qps-Ploc"),
        };

    private readonly ResourceManager _resourceManager;
    private readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, string>> _resourcesByLanguage =
        new(StringComparer.OrdinalIgnoreCase);

    public UiLocalizationResourceManager()
        : this(new ResourceManager(ResourceBaseName, typeof(UiLocalizationResourceManager).Assembly))
    {
    }

    internal UiLocalizationResourceManager(ResourceManager resourceManager)
    {
        _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
        _resourceManager.IgnoreCase = true;
    }

    public static UiLocalizationResourceManager Shared { get; } = new();

    internal UiLocalizationLookupResult Lookup(string language, string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var requestedLanguage = UiLanguageCatalog.Normalize(language);
        return LookupCore(requestedLanguage, key, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { key });
    }

    private UiLocalizationLookupResult LookupCore(string requestedLanguage, string key, HashSet<string> resolvingKeys)
    {
        foreach (var candidate in GetLookupChain(requestedLanguage))
        {
            var resourceSet = GetResourceSet(candidate);
            if (!resourceSet.TryGetValue(key, out var rawValue))
            {
                continue;
            }

            return new UiLocalizationLookupResult(
                ResolveInlineKeys(rawValue, requestedLanguage, resolvingKeys),
                requestedLanguage,
                string.Equals(candidate, requestedLanguage, StringComparison.OrdinalIgnoreCase) ? "none" : candidate);
        }

        return new UiLocalizationLookupResult(key, requestedLanguage, "key");
    }

    private string ResolveInlineKeys(string value, string requestedLanguage, HashSet<string> resolvingKeys)
    {
        if (string.IsNullOrEmpty(value) || !value.Contains("{key=", StringComparison.Ordinal))
        {
            return value;
        }

        return InlineKeyPattern.Replace(
            value,
            match =>
            {
                var referencedKey = match.Groups[1].Value.Trim();
                if (string.IsNullOrWhiteSpace(referencedKey) || !resolvingKeys.Add(referencedKey))
                {
                    return match.Value;
                }

                try
                {
                    return LookupCore(requestedLanguage, referencedKey, resolvingKeys).Value;
                }
                finally
                {
                    resolvingKeys.Remove(referencedKey);
                }
            });
    }

    private IReadOnlyDictionary<string, string> GetResourceSet(string language)
    {
        return _resourcesByLanguage.GetOrAdd(language, CreateResourceSet);
    }

    private IReadOnlyDictionary<string, string> CreateResourceSet(string language)
    {
        if (!CultureMap.TryGetValue(language, out var culture))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var resourceSet = _resourceManager.GetResourceSet(culture, createIfNotExists: true, tryParents: false);
        if (resourceSet is null)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (DictionaryEntry entry in resourceSet)
        {
            if (entry.Key is string key && entry.Value is string value)
            {
                result[key] = value;
            }
        }

        return result;
    }

    private static IReadOnlyList<string> GetLookupChain(string requestedLanguage)
    {
        var chain = new List<string>(3);
        AddDistinct(chain, requestedLanguage);
        AddDistinct(chain, UiLanguageCatalog.FallbackLanguage);
        AddDistinct(chain, UiLanguageCatalog.DefaultLanguage);
        return chain;
    }

    private static void AddDistinct(ICollection<string> chain, string language)
    {
        if (chain.Contains(language, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        chain.Add(UiLanguageCatalog.Normalize(language));
    }
}
