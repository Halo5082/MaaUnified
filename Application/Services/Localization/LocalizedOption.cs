namespace MAAUnified.Application.Services.Localization;

public readonly record struct LocalizedOptionSpec<TValue>(TValue Value, string TextKey, string Fallback);

public sealed class LocalizedOption<TValue>
{
    public LocalizedOption(LocalizedOptionSpec<TValue> spec, string display)
    {
        Value = spec.Value;
        TextKey = spec.TextKey;
        Fallback = spec.Fallback;
        Display = display;
    }

    public TValue Value { get; }

    public string TextKey { get; }

    public string Fallback { get; }

    public string Display { get; set; }
}
