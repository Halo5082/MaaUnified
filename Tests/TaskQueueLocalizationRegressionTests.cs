using MAAUnified.App.ViewModels.TaskQueue;

namespace MAAUnified.Tests;

public sealed class TaskQueueLocalizationRegressionTests
{
    private static readonly string[] FightNoFallbackKeys =
    [
        "Fight.UseStoneDisplay",
        "Fight.PerformBattles",
        "Fight.SeriesTip",
        "Fight.DrGrandetTip",
        "Fight.AssignedMaterial",
        "Fight.SpecifiedDropsTip",
        "Fight.Drop.NotSelected",
        "Fight.StageReset.Current",
        "Fight.StageReset.Ignore",
        "Fight.DefaultStage",
        "Fight.Annihilation.Current",
        "Fight.Annihilation.Chernobog",
        "Fight.Annihilation.LungmenOutskirts",
        "Fight.Annihilation.LungmenDowntown",
        "Fight.HideSeries",
        "Fight.AllowUseStoneSave",
        "Fight.AllowUseStoneSaveWarning",
    ];

    [Fact]
    public void FightRemainingSanityKeys_ShouldNotFallbackToEnglish_ForJaKoZhTw()
    {
        var map = new LocalizedTextMap();

        map.Language = "en-us";
        var enUsBaseline = FightNoFallbackKeys.ToDictionary(key => key, key => map[key], StringComparer.Ordinal);

        foreach (var language in new[] { "ja-jp", "ko-kr", "zh-tw" })
        {
            map.Language = language;
            foreach (var key in FightNoFallbackKeys)
            {
                var value = map[key];
                Assert.False(string.IsNullOrWhiteSpace(value), $"Expected non-empty text for {language}:{key}.");
                Assert.NotEqual(key, value);
                Assert.NotEqual(enUsBaseline[key], value);
            }
        }
    }

    private static string GetMaaUnifiedRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var appDir = Path.Combine(current.FullName, "App");
            var testsDir = Path.Combine(current.FullName, "Tests");
            if (Directory.Exists(appDir) && Directory.Exists(testsDir))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Cannot locate src/MAAUnified root from test runtime path.");
    }
}
