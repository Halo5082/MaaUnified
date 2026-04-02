namespace MAAUnified.Application.Models;

public sealed record StageManagerState(
    string ClientType,
    IReadOnlyList<string> LocalStageCodes,
    IReadOnlyList<string> WebStageCodes,
    string? WebSourceUrl,
    DateTimeOffset? LocalRefreshedAt,
    DateTimeOffset? WebRefreshedAt)
{
    public static StageManagerState Default { get; } = new(
        ClientType: "Official",
        LocalStageCodes: Array.Empty<string>(),
        WebStageCodes: Array.Empty<string>(),
        WebSourceUrl: null,
        LocalRefreshedAt: null,
        WebRefreshedAt: null);

    public IReadOnlyList<string> ActiveStageCodes => WebStageCodes.Count > 0 ? WebStageCodes : LocalStageCodes;

    public string ActiveStageCodesText => string.Join(Environment.NewLine, ActiveStageCodes);

    public string LocalStageCodesText => string.Join(Environment.NewLine, LocalStageCodes);

    public string WebStageCodesText => string.Join(Environment.NewLine, WebStageCodes);
}
