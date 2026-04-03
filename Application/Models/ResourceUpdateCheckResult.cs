namespace MAAUnified.Application.Models;

public sealed record ResourceUpdateCheckResult(
    bool IsUpdateAvailable,
    string DisplayVersion,
    string ReleaseNote,
    bool RequiresMirrorChyanCdk,
    string? DownloadUrl);
