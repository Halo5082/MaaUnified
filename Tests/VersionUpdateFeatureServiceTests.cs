using System;
using System.IO;
using System.Text.Json;
using MAAUnified.Application.Models;
using MAAUnified.Application.Services.Features;
using Xunit;

namespace MAAUnified.Tests;

public sealed class VersionUpdateFeatureServiceTests
{
    [Fact]
    public async Task CheckForUpdatesAsync_ReadsLocalReleaseFeed()
    {
        var service = new VersionUpdateFeatureService();
        var feedPath = Path.Combine(Path.GetTempPath(), $"maa-unified-release-feed-{Guid.NewGuid():N}.json");

        try
        {
            await File.WriteAllTextAsync(feedPath, JsonSerializer.Serialize(new[]
            {
                new
                {
                    tag_name = "v2.0.0",
                    name = "Release v2.0.0",
                    body = "Line one.\nLine two.",
                    prerelease = false,
                    assets = new[]
                    {
                        new
                        {
                            name = "MAAUnified-v2.0.0.zip",
                            browser_download_url = "https://example.com/MAAUnified-v2.0.0.zip",
                            size = 1234,
                        },
                    },
                },
            }));

            var policy = VersionUpdatePolicy.Default with
            {
                ResourceApi = feedPath,
                VersionType = "Stable",
            };

            var result = await service.CheckForUpdatesAsync(policy, "v1.0.0");

            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal("v2.0.0", result.Value!.TargetVersion);
            Assert.Equal("Release v2.0.0", result.Value.ReleaseName);
            Assert.Equal("Line one.\nLine two.", result.Value.Body);
            Assert.Equal("MAAUnified-v2.0.0.zip", result.Value.PackageName);
            Assert.True(result.Value.IsNewVersion);
            Assert.True(result.Value.HasPackage);
        }
        finally
        {
            try
            {
                if (File.Exists(feedPath))
                {
                    File.Delete(feedPath);
                }
            }
            catch
            {
                // Best-effort cleanup.
            }
        }
    }
}
