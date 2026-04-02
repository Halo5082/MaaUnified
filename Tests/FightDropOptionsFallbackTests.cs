using MAAUnified.App.ViewModels.TaskQueue;

namespace MAAUnified.Tests;

public sealed class FightDropOptionsFallbackTests
{
    [Fact]
    public void ResolveItemIndexCandidatePathsByDisplayLanguage_ShouldPreferLocalizedPath_ThenFallbackToRoot()
    {
        const string root = "/tmp/maa-fight-drop-options";
        var paths = FightTaskModuleViewModel.ResolveItemIndexCandidatePathsByDisplayLanguage("ja-jp", root);

        Assert.Equal(2, paths.Count);
        Assert.Equal(Path.Combine(root, "resource", "global", "YoStarJP", "resource", "item_index.json"), paths[0]);
        Assert.Equal(Path.Combine(root, "resource", "item_index.json"), paths[1]);
    }

    [Fact]
    public void BuildDropOptionsForLanguage_ShouldFallbackToRootItemIndex_WhenLocalizedFileMissing()
    {
        var root = Path.Combine(Path.GetTempPath(), $"maa-fight-drop-options-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(root, "resource"));

        try
        {
            File.WriteAllText(
                Path.Combine(root, "resource", "item_index.json"),
                """
                {
                  "3001": { "name": "Orirock" },
                  "foo": { "name": "IgnoreMe" }
                }
                """);

            var options = FightTaskModuleViewModel.BuildDropOptionsForLanguage("ja-jp", "Not selected", root);

            Assert.Equal("Not selected", options[0].DisplayName);
            Assert.Equal(string.Empty, options[0].Value);
            Assert.Contains(options, option => option.Value == "3001" && option.DisplayName == "Orirock");
            Assert.True(options.Count > 1);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
