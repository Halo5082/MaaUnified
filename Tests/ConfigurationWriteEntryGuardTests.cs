namespace MAAUnified.Tests;

public sealed class ConfigurationWriteEntryGuardTests
{
    [Fact]
    public void StoreSaveAsync_ShouldOnlyBeCalledInUnifiedConfigurationServiceSaveCore()
    {
        var repoRoot = ResolveRepoRoot();
        var files = EnumerateSourceFiles(repoRoot, includeTests: false);

        var hits = new List<string>();
        foreach (var file in files)
        {
            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                if (!lines[i].Contains("_store.SaveAsync(", StringComparison.Ordinal))
                {
                    continue;
                }

                hits.Add($"{Path.GetRelativePath(repoRoot, file)}:{i + 1}");
            }
        }

        Assert.NotEmpty(hits);
        Assert.All(
            hits,
            hit => Assert.StartsWith(
                Path.Combine("Application", "Services", "UnifiedConfigurationService.cs"),
                hit,
                StringComparison.Ordinal));
    }

    [Fact]
    public void AvaloniaJsonConfigStore_ShouldOnlyBeConstructedByRuntimeFactory()
    {
        var repoRoot = ResolveRepoRoot();
        var files = EnumerateSourceFiles(repoRoot, includeTests: false);

        var hits = new List<string>();
        foreach (var file in files)
        {
            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                if (!lines[i].Contains("new AvaloniaJsonConfigStore(", StringComparison.Ordinal))
                {
                    continue;
                }

                hits.Add($"{Path.GetRelativePath(repoRoot, file)}:{i + 1}");
            }
        }

        var hit = Assert.Single(hits);
        Assert.StartsWith(
            Path.Combine("Application", "Services", "MAAUnifiedRuntime.cs"),
            hit,
            StringComparison.Ordinal);
    }

    private static IReadOnlyList<string> EnumerateSourceFiles(string sourceRoot, bool includeTests)
    {
        return Directory
            .EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(file => includeTests || !file.Contains($"{Path.DirectorySeparatorChar}Tests{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .ToArray();
    }

    private static string ResolveRepoRoot()
    {
        return TestRepoLayout.GetMaaUnifiedRoot();
    }
}
