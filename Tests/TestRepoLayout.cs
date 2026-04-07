using System.Diagnostics.CodeAnalysis;

namespace MAAUnified.Tests;

internal static class TestRepoLayout
{
    public static string GetMaaUnifiedRoot()
    {
        if (TryGetMaaUnifiedRoot(out var root))
        {
            return root;
        }

        throw new DirectoryNotFoundException("Cannot locate MAAUnified repo root from test runtime path.");
    }

    public static bool TryGetMaaUnifiedRoot([NotNullWhen(true)] out string? root)
    {
        root = FindAncestor(path =>
        {
            if (LooksLikeMaaUnifiedRoot(path))
            {
                return true;
            }

            var nested = Path.Combine(path, "src", "MAAUnified");
            return LooksLikeMaaUnifiedRoot(nested);
        });

        if (root is null)
        {
            return false;
        }

        if (!LooksLikeMaaUnifiedRoot(root))
        {
            root = Path.Combine(root, "src", "MAAUnified");
        }

        return true;
    }

    public static string GetHostRepoRoot()
    {
        if (TryGetHostRepoRoot(out var root))
        {
            return root;
        }

        throw new DirectoryNotFoundException(
            "Cannot locate MaaAssistantArknights host repo root containing src/MAAUnified and resource/battle_data.json.");
    }

    public static bool TryGetHostRepoRoot([NotNullWhen(true)] out string? root)
    {
        root = FindAncestor(LooksLikeHostRepoRoot);
        return root is not null;
    }

    private static string? FindAncestor(Func<string, bool> predicate)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (predicate(current.FullName))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return null;
    }

    private static bool LooksLikeMaaUnifiedRoot(string path)
    {
        return Directory.Exists(Path.Combine(path, "App"))
            && Directory.Exists(Path.Combine(path, "Application"))
            && Directory.Exists(Path.Combine(path, "Tests"));
    }

    private static bool LooksLikeHostRepoRoot(string path)
    {
        return Directory.Exists(Path.Combine(path, "src", "MAAUnified", "App"))
            && Directory.Exists(Path.Combine(path, "src", "MAAUnified", "Tests"))
            && File.Exists(Path.Combine(path, "resource", "battle_data.json"));
    }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
internal sealed class HostRepoFactAttribute : FactAttribute
{
    public HostRepoFactAttribute()
    {
        if (!TestRepoLayout.TryGetHostRepoRoot(out _))
        {
            Skip = "Requires the MaaAssistantArknights host repository layout and resource assets.";
        }
    }
}
