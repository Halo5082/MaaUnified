using System.Reflection;
using MAAUnified.Application.Services.Localization;

namespace MAAUnified.Tests;

public sealed class AchievementLocalizationTextCatalogTests
{
    [Theory]
    [InlineData("ja-jp", new[] { "ja-jp", "en-us", "zh-cn" })]
    [InlineData("ko-kr", new[] { "ko-kr", "en-us", "zh-cn" })]
    [InlineData("zh-tw", new[] { "zh-tw", "zh-cn", "en-us" })]
    public void BuildLookupOrder_ShouldPrioritizeRequestedLanguage(string language, string[] expected)
    {
        var method = typeof(AchievementTextCatalog).GetMethod(
            "BuildLookupOrder",
            BindingFlags.NonPublic | BindingFlags.Static);

        var actual = Assert.IsAssignableFrom<IReadOnlyList<string>>(method!.Invoke(null, [language]));
        Assert.Equal(expected, actual);
    }
}
