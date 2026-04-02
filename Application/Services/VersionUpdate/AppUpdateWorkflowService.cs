using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using MAAUnified.Application.Models;
using MAAUnified.Application.Services;

namespace MAAUnified.Application.Services.VersionUpdate;

public sealed class AppUpdateWorkflowService
{
    private const string GitHubReleasesUrl = "https://api.github.com/repos/MaaAssistantArknights/MaaAssistantArknights/releases";
    private static readonly HttpClient DefaultHttpClient = BuildDefaultHttpClient();

    private readonly IAppLifecycleService _appLifecycleService;
    private readonly HttpClient _httpClient;

    public AppUpdateWorkflowService(
        IAppLifecycleService appLifecycleService,
        HttpClient? httpClient = null)
    {
        _appLifecycleService = appLifecycleService;
        _httpClient = httpClient ?? DefaultHttpClient;
    }

    public async Task<VersionUpdateCheckResult> CheckForUpdatesAsync(
        VersionUpdatePolicy policy,
        string currentVersion,
        CancellationToken cancellationToken)
    {
        var releases = await ResolveReleasesAsync(policy.ResourceApi, cancellationToken).ConfigureAwait(false);
        var release = SelectRelease(releases, policy.VersionType);
        if (!release.HasValue)
        {
            throw new InvalidOperationException("No releases were returned by the configured resource.");
        }

        var releaseValue = release.Value;

        var tag = releaseValue.GetProperty("tag_name").GetString()?.Trim() ?? string.Empty;
        var releaseName = releaseValue.GetProperty("name").GetString()?.Trim() ?? tag;
        var body = releaseValue.GetProperty("body").GetString()?.Trim() ?? string.Empty;
        var summary = string.IsNullOrWhiteSpace(body) ? releaseName : body;
        var package = SelectPackageAsset(releaseValue);
        var downloadUrl = package?.GetProperty("browser_download_url").GetString();
        var packageName = package?.GetProperty("name").GetString()?.Trim();
        long? packageSize = null;
        if (package?.TryGetProperty("size", out var sizeNode) == true && sizeNode.TryGetInt64(out var sizeValue))
        {
            packageSize = sizeValue;
        }

        var targetVersion = tag;
        var isNew = !string.Equals(currentVersion, tag, StringComparison.OrdinalIgnoreCase);

        return new VersionUpdateCheckResult(
            Channel: policy.VersionType,
            CurrentVersion: currentVersion,
            TargetVersion: targetVersion,
            ReleaseName: releaseName,
            Summary: summary,
            Body: body,
            PackageName: packageName,
            PackageDownloadUrl: string.IsNullOrWhiteSpace(downloadUrl) ? null : new Uri(downloadUrl, UriKind.Absolute),
            PackageSize: packageSize,
            IsNewVersion: isNew,
            HasPackage: !string.IsNullOrWhiteSpace(downloadUrl));
    }

    public async Task<UiOperationResult<string>> DownloadPackageAsync(
        VersionUpdateCheckResult checkResult,
        string runtimeBaseDirectory,
        bool forceDownload,
        CancellationToken cancellationToken)
    {
        if (!checkResult.HasPackage || checkResult.PackageDownloadUrl is null)
        {
            return UiOperationResult<string>.Fail(
                UiErrorCode.UiOperationFailed,
                "No update package is available for download.");
        }

        var targetDirectory = Path.Combine(runtimeBaseDirectory, "update-packages");
        Directory.CreateDirectory(targetDirectory);
        var packageName = string.IsNullOrWhiteSpace(checkResult.PackageName)
            ? $"MAAUnified-{checkResult.TargetVersion}.zip"
            : checkResult.PackageName;
        var destinationPath = Path.Combine(targetDirectory, packageName);

        if (File.Exists(destinationPath) && !forceDownload)
        {
            return UiOperationResult<string>.Ok(destinationPath, "Reused existing update package.");
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, checkResult.PackageDownloadUrl);
            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return UiOperationResult<string>.Fail(
                    UiErrorCode.UiOperationFailed,
                    $"Package download failed with HTTP {(int)response.StatusCode}.");
            }

            await using var sourceStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await using var destinationStream = File.Create(destinationPath);
            await sourceStream.CopyToAsync(destinationStream, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return UiOperationResult<string>.Fail(
                UiErrorCode.UiOperationFailed,
                $"Failed to download update package: {ex.Message}");
        }

        return UiOperationResult<string>.Ok(destinationPath, "Update package downloaded.");
    }

    public Task<UiOperationResult> InstallPackageAsync(string packagePath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(packagePath) || !File.Exists(packagePath))
        {
            return Task.FromResult(UiOperationResult.Fail(
                UiErrorCode.UiOperationFailed,
                "Update package file is missing."));
        }

        return _appLifecycleService.RestartAsync(cancellationToken);
    }

    private static HttpClient BuildDefaultHttpClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("MAAUnified-VersionUpdate/1.0");
        return client;
    }

    private Task<JsonElement[]> ResolveReleasesAsync(string? resourceApi, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(resourceApi))
        {
            var trimmed = resourceApi.Trim();
            if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            {
                if (string.Equals(uri.Scheme, Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult(LoadReleasesFromFileSync(uri.LocalPath));
                }

                return FetchReleasesFromUrlAsync(uri.ToString(), cancellationToken);
            }

            if (File.Exists(trimmed))
            {
                return Task.FromResult(LoadReleasesFromFileSync(trimmed));
            }
        }

        return FetchReleasesFromUrlAsync(GitHubReleasesUrl, cancellationToken);
    }

    private static JsonElement[] LoadReleasesFromFileSync(string path)
    {
        var text = File.ReadAllText(path);
        using var document = JsonDocument.Parse(text);
        return document.RootElement.ValueKind == JsonValueKind.Array
            ? document.RootElement.EnumerateArray().Select(static element => element.Clone()).ToArray()
            : Array.Empty<JsonElement>();
    }

    private async Task<JsonElement[]> FetchReleasesFromUrlAsync(string url, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        return document.RootElement.ValueKind == JsonValueKind.Array
            ? document.RootElement.EnumerateArray().Select(static element => element.Clone()).ToArray()
            : Array.Empty<JsonElement>();
    }

    private static JsonElement? SelectPackageAsset(JsonElement release)
    {
        if (!release.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var asset in assets.EnumerateArray())
        {
            if (!asset.TryGetProperty("browser_download_url", out var browserNode)
                || browserNode.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            if (asset.TryGetProperty("name", out var nameNode)
                && nameNode.ValueKind == JsonValueKind.String)
            {
                var name = nameNode.GetString() ?? string.Empty;
                if (name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("MAAUnified", StringComparison.OrdinalIgnoreCase))
                {
                    return asset;
                }
            }
        }

        return assets.EnumerateArray().FirstOrDefault();
    }

    private static JsonElement? SelectRelease(JsonElement[] releases, string channel)
    {
        JsonElement? fallback = null;
        var normalizedChannel = (channel ?? string.Empty).Trim();
        foreach (var release in releases)
        {
            if (fallback is null)
            {
                fallback = release;
            }

            if (MatchesChannel(release, normalizedChannel))
            {
                return release;
            }
        }

        return fallback;
    }

    private static bool MatchesChannel(JsonElement release, string channel)
    {
        var tag = release.GetProperty("tag_name").GetString() ?? string.Empty;
        var name = release.GetProperty("name").GetString() ?? string.Empty;
        var prerelease = release.GetProperty("prerelease").GetBoolean();

        if (string.Equals(channel, "Beta", StringComparison.OrdinalIgnoreCase))
        {
            return tag.Contains("beta", StringComparison.OrdinalIgnoreCase)
                || name.Contains("beta", StringComparison.OrdinalIgnoreCase);
        }

        if (string.Equals(channel, "Nightly", StringComparison.OrdinalIgnoreCase))
        {
            return tag.Contains("nightly", StringComparison.OrdinalIgnoreCase)
                || name.Contains("nightly", StringComparison.OrdinalIgnoreCase);
        }

        if (string.Equals(channel, "Stable", StringComparison.OrdinalIgnoreCase))
        {
            return !prerelease;
        }

        return true;
    }
}
