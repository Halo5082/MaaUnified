using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using MAAUnified.Application.Models;
using MAAUnified.Application.Services;

namespace MAAUnified.Application.Services.VersionUpdate;

public sealed class AppUpdateWorkflowService
{
    private const string MaaApiBaseUrl = "https://api.maa.plus/MaaAssistantArknights/api/";
    private const string MaaApiFallbackBaseUrl = "https://api2.maa.plus/MaaAssistantArknights/api/";
    private const string GitHubReleasesUrl = "https://api.github.com/repos/MaaAssistantArknights/MaaAssistantArknights/releases";
    private static readonly HttpClient DefaultHttpClient = BuildDefaultHttpClient();

    private readonly IAppLifecycleService _appLifecycleService;
    private readonly HttpClient? _httpClient;

    public AppUpdateWorkflowService(
        IAppLifecycleService appLifecycleService,
        HttpClient? httpClient = null)
    {
        _appLifecycleService = appLifecycleService;
        _httpClient = httpClient;
    }

    public async Task<VersionUpdateCheckResult> CheckForUpdatesAsync(
        VersionUpdatePolicy policy,
        string currentVersion,
        CancellationToken cancellationToken)
    {
        var (httpClient, disposeClient) = ResolveHttpClient(policy);
        try
        {
            var release = await ResolveReleaseAsync(policy.ResourceApi, policy.VersionType, httpClient, cancellationToken).ConfigureAwait(false);
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
        finally
        {
            if (disposeClient)
            {
                httpClient.Dispose();
            }
        }
    }

    public async Task<UiOperationResult<string>> DownloadPackageAsync(
        VersionUpdateCheckResult checkResult,
        string runtimeBaseDirectory,
        bool forceDownload,
        VersionUpdatePolicy? policy,
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

        var (httpClient, disposeClient) = ResolveHttpClient(policy);
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, checkResult.PackageDownloadUrl);
            using var response = await httpClient.SendAsync(
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
        finally
        {
            if (disposeClient)
            {
                httpClient.Dispose();
            }
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

    private static HttpClient BuildDefaultHttpClient(VersionUpdatePolicy? policy = null)
    {
        var handler = BuildHttpClientHandler(policy);
        var client = new HttpClient(handler, disposeHandler: true) { Timeout = TimeSpan.FromSeconds(60) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("MAAUnified-VersionUpdate/1.0");
        return client;
    }

    private (HttpClient Client, bool DisposeClient) ResolveHttpClient(VersionUpdatePolicy? policy)
    {
        if (_httpClient is not null)
        {
            return (_httpClient, false);
        }

        if (policy is null || string.IsNullOrWhiteSpace(policy.Proxy))
        {
            return (DefaultHttpClient, false);
        }

        return (BuildDefaultHttpClient(policy), true);
    }

    private static HttpClientHandler BuildHttpClientHandler(VersionUpdatePolicy? policy)
    {
        var handler = new HttpClientHandler();
        if (policy is null || !TryBuildProxy(policy, out var proxy))
        {
            return handler;
        }

        handler.UseProxy = true;
        handler.Proxy = proxy;
        return handler;
    }

    private static bool TryBuildProxy(VersionUpdatePolicy policy, out IWebProxy? proxy)
    {
        proxy = null;
        var rawProxy = policy.Proxy?.Trim();
        if (string.IsNullOrWhiteSpace(rawProxy))
        {
            return false;
        }

        if (string.Equals(policy.ProxyType, "system", StringComparison.OrdinalIgnoreCase))
        {
            proxy = WebRequest.DefaultWebProxy;
            return proxy is not null;
        }

        var candidate = rawProxy.Contains("://", StringComparison.Ordinal)
            ? rawProxy
            : $"{policy.ProxyType}://{rawProxy}";
        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var proxyUri))
        {
            return false;
        }

        proxy = new WebProxy(proxyUri);
        return true;
    }

    private async Task<JsonElement?> ResolveReleaseAsync(
        string? resourceApi,
        string channel,
        HttpClient httpClient,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(resourceApi))
        {
            var trimmed = resourceApi.Trim();
            if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            {
                if (string.Equals(uri.Scheme, Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase))
                {
                    return SelectRelease(LoadReleasesFromFileSync(uri.LocalPath), channel);
                }

                var directRelease = await TryResolveReleaseFromDirectFeedAsync(httpClient, uri, channel, cancellationToken).ConfigureAwait(false);
                if (directRelease.HasValue)
                {
                    return directRelease;
                }

                foreach (var baseUrl in BuildMaaApiBaseUrlCandidates(uri))
                {
                    var apiRelease = await TryResolveReleaseFromMaaApiBaseUrlAsync(httpClient, baseUrl, channel, cancellationToken).ConfigureAwait(false);
                    if (apiRelease.HasValue)
                    {
                        return apiRelease;
                    }
                }
            }

            if (File.Exists(trimmed))
            {
                return SelectRelease(LoadReleasesFromFileSync(trimmed), channel);
            }
        }

        foreach (var baseUrl in GetDefaultMaaApiBaseUrls())
        {
            var apiRelease = await TryResolveReleaseFromMaaApiBaseUrlAsync(httpClient, baseUrl, channel, cancellationToken).ConfigureAwait(false);
            if (apiRelease.HasValue)
            {
                return apiRelease;
            }
        }

        var releases = await FetchReleasesFromUrlAsync(httpClient, GitHubReleasesUrl, cancellationToken).ConfigureAwait(false);
        return SelectRelease(releases, channel);
    }

    private static bool LooksLikeDirectReleaseFeedUri(Uri uri)
    {
        if (string.Equals(uri.Host, "api.github.com", StringComparison.OrdinalIgnoreCase)
            && uri.AbsolutePath.Contains("/releases", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return uri.AbsolutePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<JsonElement?> TryResolveReleaseFromDirectFeedAsync(
        HttpClient httpClient,
        Uri uri,
        string channel,
        CancellationToken cancellationToken)
    {
        if (!LooksLikeDirectReleaseFeedUri(uri))
        {
            return null;
        }

        try
        {
            var releases = await FetchReleasesFromUrlAsync(httpClient, uri.ToString(), cancellationToken).ConfigureAwait(false);
            return SelectRelease(releases, channel);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    private static string[] BuildMaaApiBaseUrlCandidates(Uri resourceApiUri)
    {
        var candidates = new List<string>();
        if (TryNormalizeMaaApiBaseUrl(resourceApiUri, out var normalized))
        {
            candidates.Add(normalized);
        }

        foreach (var officialBaseUrl in GetDefaultMaaApiBaseUrls())
        {
            if (!candidates.Contains(officialBaseUrl, StringComparer.OrdinalIgnoreCase))
            {
                candidates.Add(officialBaseUrl);
            }
        }

        return candidates.ToArray();
    }

    private static string[] GetDefaultMaaApiBaseUrls()
    {
        return [MaaApiBaseUrl, MaaApiFallbackBaseUrl];
    }

    private static bool TryNormalizeMaaApiBaseUrl(Uri resourceApiUri, out string baseUrl)
    {
        baseUrl = string.Empty;
        if (!string.Equals(resourceApiUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(resourceApiUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.Equals(resourceApiUri.Host, "api.github.com", StringComparison.OrdinalIgnoreCase)
            || resourceApiUri.AbsolutePath.Contains("/releases", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var absoluteUri = resourceApiUri.AbsoluteUri;
        var versionIndex = absoluteUri.IndexOf("/version/", StringComparison.OrdinalIgnoreCase);
        if (versionIndex >= 0)
        {
            baseUrl = absoluteUri[..(versionIndex + 1)];
            return true;
        }

        if (Path.HasExtension(resourceApiUri.AbsolutePath))
        {
            return false;
        }

        baseUrl = absoluteUri.EndsWith("/", StringComparison.Ordinal) ? absoluteUri : $"{absoluteUri}/";
        return true;
    }

    private async Task<JsonElement?> TryResolveReleaseFromMaaApiBaseUrlAsync(
        HttpClient httpClient,
        string baseUrl,
        string channel,
        CancellationToken cancellationToken)
    {
        try
        {
            var summaryUri = new Uri(new Uri(baseUrl), "version/summary.json");
            using var summaryDocument = await FetchJsonDocumentAsync(httpClient, summaryUri.ToString(), cancellationToken).ConfigureAwait(false);
            if (summaryDocument.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var detailUri = ResolveMaaApiDetailUri(summaryDocument.RootElement, baseUrl, channel);
            using var detailDocument = await FetchJsonDocumentAsync(httpClient, detailUri, cancellationToken).ConfigureAwait(false);
            if (detailDocument.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (detailDocument.RootElement.TryGetProperty("details", out var details)
                && details.ValueKind == JsonValueKind.Object)
            {
                return details.Clone();
            }

            return detailDocument.RootElement.TryGetProperty("tag_name", out _)
                ? detailDocument.RootElement.Clone()
                : null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    private static string ResolveMaaApiDetailUri(JsonElement summary, string baseUrl, string channel)
    {
        var channelKey = NormalizeMaaApiChannel(channel);
        if (summary.TryGetProperty(channelKey, out var channelElement)
            && channelElement.ValueKind == JsonValueKind.Object
            && channelElement.TryGetProperty("detail", out var detailElement)
            && detailElement.ValueKind == JsonValueKind.String)
        {
            var detail = detailElement.GetString();
            if (!string.IsNullOrWhiteSpace(detail))
            {
                if (Uri.TryCreate(detail, UriKind.Absolute, out var absoluteDetail))
                {
                    return absoluteDetail.ToString();
                }

                return new Uri(new Uri(baseUrl), detail).ToString();
            }
        }

        return new Uri(new Uri(baseUrl), $"version/{channelKey}.json").ToString();
    }

    private static string NormalizeMaaApiChannel(string channel)
    {
        if (string.Equals(channel, "Beta", StringComparison.OrdinalIgnoreCase))
        {
            return "beta";
        }

        if (string.Equals(channel, "Nightly", StringComparison.OrdinalIgnoreCase))
        {
            return "alpha";
        }

        return "stable";
    }

    private static JsonElement[] LoadReleasesFromFileSync(string path)
    {
        var text = File.ReadAllText(path);
        using var document = JsonDocument.Parse(text);
        return document.RootElement.ValueKind == JsonValueKind.Array
            ? document.RootElement.EnumerateArray().Select(static element => element.Clone()).ToArray()
            : Array.Empty<JsonElement>();
    }

    private static async Task<JsonDocument> FetchJsonDocumentAsync(
        HttpClient httpClient,
        string url,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        using var response = await httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private static async Task<JsonElement[]> FetchReleasesFromUrlAsync(HttpClient httpClient, string url, CancellationToken cancellationToken)
    {
        using var document = await FetchJsonDocumentAsync(httpClient, url, cancellationToken).ConfigureAwait(false);
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
