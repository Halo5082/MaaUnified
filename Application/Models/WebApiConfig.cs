namespace MAAUnified.Application.Models;

[Obsolete("Deprecated: WebApi is retained in source only and is no longer a user-visible Unified feature.")]
public sealed record WebApiConfig(
    bool Enabled,
    string Host,
    int Port,
    string AccessToken)
{
    public static WebApiConfig Default { get; } = new(
        Enabled: false,
        Host: "127.0.0.1",
        Port: 51888,
        AccessToken: string.Empty);

    public IReadOnlyDictionary<string, string> ToGlobalSettingUpdates()
    {
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Advanced.WebApi.Enabled"] = Enabled.ToString(),
            ["Advanced.WebApi.Host"] = Host,
            ["Advanced.WebApi.Port"] = Port.ToString(),
            ["Advanced.WebApi.AccessToken"] = AccessToken,
        };
    }
}
