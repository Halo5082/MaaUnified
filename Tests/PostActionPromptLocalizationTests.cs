using System.Reflection;
using MAAUnified.App.Services;
using MAAUnified.Platform;

namespace MAAUnified.Tests;

public sealed class PostActionPromptLocalizationTests
{
    [Theory]
    [InlineData(
        "ja-jp",
        PostActionType.Shutdown,
        "シャットダウンの確認",
        "キャンセルしない場合、MAA は 15 秒後にこのコンピューターをシャットダウンします。",
        "今すぐ実行")]
    [InlineData(
        "ko-kr",
        PostActionType.Sleep,
        "절전 모드 확인",
        "취소하지 않으면 MAA가 15초 후에 이 컴퓨터를 절전 모드로 전환합니다.",
        "지금 실행")]
    public void PostActionDialogTexts_ShouldUseLocalizedResources(
        string language,
        PostActionType action,
        string expectedTitle,
        string expectedMessage,
        string expectedConfirmText)
    {
        Assert.Equal(expectedTitle, InvokeBuildTitle(action, language));
        Assert.Equal(expectedMessage, InvokeBuildMessage(action, 15, language));
        Assert.Equal(expectedConfirmText, InvokeBuildConfirmText(language));
    }

    private static string InvokeBuildTitle(PostActionType action, string language)
    {
        return (string)typeof(AvaloniaPostActionPromptService)
            .GetMethod("BuildTitle", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, [action, language])!;
    }

    private static string InvokeBuildMessage(PostActionType action, int seconds, string language)
    {
        return (string)typeof(AvaloniaPostActionPromptService)
            .GetMethod("BuildMessage", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, [action, seconds, language])!;
    }

    private static string InvokeBuildConfirmText(string language)
    {
        return (string)typeof(AvaloniaPostActionPromptService)
            .GetMethod("BuildConfirmText", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, [language])!;
    }
}
