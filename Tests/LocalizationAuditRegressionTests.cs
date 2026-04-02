using System.Text.RegularExpressions;
using MAAUnified.App.ViewModels;
using MAAUnified.App.ViewModels.Copilot;
using MAAUnified.App.ViewModels.Infrastructure;
using MAAUnified.App.ViewModels.TaskQueue;
using MAAUnified.App.ViewModels.Toolbox;
using MAAUnified.Application.Configuration;
using MAAUnified.Application.Models;
using MAAUnified.Application.Orchestration;
using MAAUnified.Application.Services;
using MAAUnified.Application.Services.Features;
using MAAUnified.CoreBridge;
using MAAUnified.Platform;

namespace MAAUnified.Tests;

[Collection("MainShellSerial")]
public sealed class LocalizationAuditRegressionTests
{
    private static readonly string[] MainTabKeys =
    [
        "Main.Tab.TaskQueue",
        "Main.Tab.Copilot",
        "Main.Tab.Toolbox",
        "Main.Tab.Settings",
    ];

    private static readonly string[] TargetViewFiles =
    [
        "App/Features/Advanced/CopilotView.axaml",
        "App/Features/Advanced/ToolboxView.axaml",
        "App/Features/Root/TaskQueueView.axaml",
        "App/Features/Root/SettingsView.axaml",
    ];

    private static readonly string[] RootCriticalKeys =
    [
        "TaskQueue.Root.TaskListTitle",
        "TaskQueue.Root.TaskConfigTitle",
        "TaskQueue.Root.GeneralSettings",
        "TaskQueue.Root.AdvancedSettings",
        "TaskQueue.Root.RenameDialogTitle",
        "TaskQueue.Root.RenameDialogPrompt",
        "TaskQueue.Root.RenameDialogConfirm",
        "TaskQueue.Root.RenameDialogCancel",
        "TaskQueue.Root.RenameDialogCancelStatus",
        "TaskQueue.Root.RenameDialogClosedStatus",
        "TaskQueue.Root.OverlayTargetPickerTitle",
        "TaskQueue.Root.OverlayTargetPickerConfirm",
        "TaskQueue.Root.OverlayTargetPickerCancel",
        "TaskQueue.Root.OverlayTargetPickerCancelStatus",
        "TaskQueue.Root.OverlayTargetPickerClosedStatus",
        "Settings.Section.Performance",
        "Settings.Section.Game",
        "Settings.Section.GUI",
        "Settings.Section.VersionUpdate",
        "Settings.Section.About",
        "Settings.Start.Dialog.SelectEmulatorPathTitle",
        "Settings.Start.Dialog.SelectEmulatorPathConfirm",
        "Settings.Start.Dialog.SelectEmulatorPathCancel",
        "Settings.Start.Status.EmulatorPathUpdated",
        "Settings.Start.Status.EmulatorPathSelectionCancelled",
        "Settings.Start.Status.EmulatorPathSelectionClosed",
        "Settings.VersionUpdate.Dialog.Title",
        "Settings.VersionUpdate.Dialog.Confirm",
        "Settings.VersionUpdate.Dialog.Cancel",
        "Settings.VersionUpdate.Status.PersistFailedSuffix",
        "Settings.RemoteControl.Status.TestSucceeded",
        "Settings.SaveScoped.Error.BatchEmpty",
        "Settings.SaveScoped.Error.ProfileMissing",
        "Settings.SaveScoped.Error.SettingKeyMissing",
        "Settings.SaveScoped.Error.SaveFailed",
        "Settings.SaveScoped.Status.SavedCount",
    ];

    private static readonly string[] CopilotCriticalKeys =
    [
        "Copilot.Tab.Main",
        "Copilot.Input.PathOrCodeWatermark",
        "Copilot.Button.File",
        "Copilot.Button.Paste",
        "Copilot.Option.BattleList",
        "Copilot.Option.UseSanityPotion",
        "Copilot.Option.LoopTimes",
        "Copilot.Button.Clear",
    ];

    private static readonly string[] ToolboxCriticalKeys =
    [
        "Toolbox.Tab.Recruit",
        "Toolbox.Action.StartRecognition",
        "Toolbox.MiniGame.Name",
        "Toolbox.Status.WaitingForExecution",
        "Toolbox.Tip.RecruitRecognition",
    ];

    private static readonly string[] TaskQueueCriticalKeys =
    [
        "StartUp.Title",
        "StartUp.Option.ClientType.Official",
        "StartUp.Option.ConnectConfig.General",
        "StartUp.Option.ConnectConfig.PC",
        "StartUp.Option.ConnectConfig.GeneralWithoutScreencapErr",
        "StartUp.Option.TouchMode.MiniTouch",
        "StartUp.Option.AttachScreencap.FramePool",
        "StartUp.Option.AttachInput.Seize",
        "StartUp.Option.AttachInput.PostWithCursor",
        "StartUp.Option.AttachInput.PostWithWindowPos",
        "Fight.Title",
        "Recruit.Title",
        "Infrast.Title",
        "Mall.Title",
        "Award.Title",
        "PostAction.Title",
        "Roguelike.Title",
        "Reclamation.Title",
    ];

    private static readonly Regex ChineseUiLiteralPattern = new(
        "(?<attr>Header|HeaderText|Content|Text|Watermark|ToolTip\\.Tip)\\s*=\\s*\"(?<value>[^\"]*[\\u4e00-\\u9fff][^\"]*)\"",
        RegexOptions.Compiled);

    [Fact]
    public void MainTabs_ShouldResolveLocalizedText_ForFiveLanguages()
    {
        var map = new RootLocalizationTextMap("Root.Localization.Tests");
        var languages = new[] { "zh-cn", "zh-tw", "en-us", "ja-jp", "ko-kr" };

        foreach (var language in languages)
        {
            map.Language = language;
            foreach (var key in MainTabKeys)
            {
                var value = map[key];
                Assert.False(string.IsNullOrWhiteSpace(value), $"Expected non-empty localized text for {language}:{key}.");
                Assert.NotEqual(key, value);
            }
        }
    }

    [Fact]
    public void RootLocalizationTextMapSource_ShouldDefineMainTabOverrides_ForJaKoZhTw()
    {
        var root = GetMaaUnifiedRoot();
        var sourcePath = Path.Combine(root, "App", "ViewModels", "Infrastructure", "RootLocalizationTextMap.cs");
        var source = File.ReadAllText(sourcePath);

        var sections = new Dictionary<string, string>
        {
            ["JaJp"] = ExtractDictionarySection(source, "JaJp", "KoKr"),
            ["KoKr"] = ExtractDictionarySection(source, "KoKr", "ZhTw"),
            ["ZhTw"] = ExtractDictionarySection(source, "ZhTw", "Pallas"),
        };

        foreach (var pair in sections)
        {
            foreach (var key in MainTabKeys)
            {
                Assert.Contains($"[\"{key}\"]", pair.Value, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void TargetViews_ShouldNotContainHardcodedChineseStaticLabels()
    {
        var root = GetMaaUnifiedRoot();
        var findings = new List<string>();
        foreach (var relative in TargetViewFiles)
        {
            var fullPath = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
            findings.AddRange(FindHardcodedChineseLiterals(fullPath));
        }

        Assert.True(
            findings.Count == 0,
            "Detected hardcoded Chinese UI literals in target views:\n" + string.Join('\n', findings));
    }

    [Fact]
    public async Task SwitchLanguageToAsync_ShouldUpdateMainChainAndPageLocalizationStates()
    {
        await using var fixture = await RuntimeFixture.CreateAsync();
        var vm = new MainShellViewModel(fixture.Runtime);
        try
        {
            await vm.InitializeAsync();

            var beforeRootTab = vm.RootTexts["Main.Tab.Settings"];
            var beforeTaskQueueTitle = vm.TaskQueuePage.RootTexts["TaskQueue.Root.TaskListTitle"];

            await vm.SwitchLanguageToAsync("en-us");

            Assert.True(
                await WaitUntilAsync(
                    () =>
                        string.Equals(vm.CurrentShellLanguage, "en-us", StringComparison.OrdinalIgnoreCase)
                        && string.Equals(vm.SettingsPage.Language, "en-us", StringComparison.OrdinalIgnoreCase)
                        && string.Equals(vm.TaskQueuePage.Texts.Language, "en-us", StringComparison.OrdinalIgnoreCase)
                        && string.Equals(vm.CopilotPage.RootTexts.Language, "en-us", StringComparison.OrdinalIgnoreCase)
                        && string.Equals(vm.ToolboxPage.RootTexts.Language, "en-us", StringComparison.OrdinalIgnoreCase),
                    retry: 160,
                    delayMs: 25));

            var afterRootTab = vm.RootTexts["Main.Tab.Settings"];
            var afterTaskQueueTitle = vm.TaskQueuePage.RootTexts["TaskQueue.Root.TaskListTitle"];
            Assert.NotEqual(beforeRootTab, afterRootTab);
            Assert.NotEqual(beforeTaskQueueTitle, afterTaskQueueTitle);
        }
        finally
        {
            TestShellCleanup.StopTimerScheduler(vm);
        }
    }

    [Fact]
    public void RootCriticalKeys_ShouldNotFallbackToEnglish_ForJaKoZhTw()
    {
        var map = new RootLocalizationTextMap("Root.Localization.Tests");
        map.Language = "en-us";
        var enUsBaseline = RootCriticalKeys.ToDictionary(key => key, key => map[key], StringComparer.Ordinal);

        foreach (var language in new[] { "ja-jp", "ko-kr", "zh-tw" })
        {
            map.Language = language;
            foreach (var key in RootCriticalKeys)
            {
                Assert.NotEqual(
                    enUsBaseline[key],
                    map[key]);
            }
        }
    }

    [Fact]
    public void CopilotCriticalKeys_ShouldNotFallbackToEnglish_ForJaKo()
    {
        var map = new CopilotLocalizationTextMap();
        map.Language = "en-us";
        var enUsBaseline = CopilotCriticalKeys.ToDictionary(key => key, key => map[key], StringComparer.Ordinal);

        foreach (var language in new[] { "ja-jp", "ko-kr" })
        {
            map.Language = language;
            foreach (var key in CopilotCriticalKeys)
            {
                Assert.NotEqual(
                    enUsBaseline[key],
                    map[key]);
            }
        }
    }

    [Fact]
    public void ToolboxCriticalKeys_ShouldNotFallbackToEnglish_ForJaKo()
    {
        var map = new ToolboxLocalizationTextMap();
        map.Language = "en-us";
        var enUsBaseline = ToolboxCriticalKeys.ToDictionary(key => key, key => map[key], StringComparer.Ordinal);

        foreach (var language in new[] { "ja-jp", "ko-kr" })
        {
            map.Language = language;
            foreach (var key in ToolboxCriticalKeys)
            {
                Assert.NotEqual(
                    enUsBaseline[key],
                    map[key]);
            }
        }
    }

    [Fact]
    public void TaskQueueCriticalKeys_ShouldNotFallbackToEnglish_ForJaKoZhTw()
    {
        var map = new LocalizedTextMap();
        map.Language = "en-us";
        var enUsBaseline = TaskQueueCriticalKeys.ToDictionary(key => key, key => map[key], StringComparer.Ordinal);

        foreach (var language in new[] { "ja-jp", "ko-kr", "zh-tw" })
        {
            map.Language = language;
            foreach (var key in TaskQueueCriticalKeys)
            {
                Assert.NotEqual(
                    enUsBaseline[key],
                    map[key]);
            }
        }
    }

    [Fact]
    public void TaskQueueCoreTitles_ShouldMatchWpf_ForJaKoZhTw()
    {
        var map = new LocalizedTextMap();

        var expected = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["ja-jp"] = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["StartUp.Title"] = "ウェイクアップ",
                ["Fight.Title"] = "作戦",
                ["Recruit.Title"] = "公開求人",
                ["Mall.Title"] = "FP獲得と交換",
                ["Award.Title"] = "報酬受取",
                ["Roguelike.Title"] = "自動ローグ",
                ["Reclamation.Title"] = "生息演算",
            },
            ["ko-kr"] = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["StartUp.Title"] = "로그인",
                ["Fight.Title"] = "이성 사용",
                ["Recruit.Title"] = "공개모집",
                ["Mall.Title"] = "크레딧 수급 및 상점",
                ["Award.Title"] = "보상 수령",
                ["Roguelike.Title"] = "통합 전략",
                ["Reclamation.Title"] = "생존 연산",
            },
            ["zh-tw"] = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["StartUp.Title"] = "開始喚醒",
                ["Fight.Title"] = "刷理智",
                ["Recruit.Title"] = "自動公招",
                ["Mall.Title"] = "獲取信用及購物",
                ["Award.Title"] = "領取獎勵",
                ["Roguelike.Title"] = "自動肉鴿",
                ["Reclamation.Title"] = "生息演算",
            },
        };

        foreach (var language in expected.Keys)
        {
            map.Language = language;
            foreach (var pair in expected[language])
            {
                Assert.Equal(pair.Value, map[pair.Key]);
            }
        }
    }

    private static IEnumerable<string> FindHardcodedChineseLiterals(string path)
    {
        var xaml = File.ReadAllText(path);
        foreach (Match match in ChineseUiLiteralPattern.Matches(xaml))
        {
            var value = match.Groups["value"].Value;
            if (IsMarkupExpression(value))
            {
                continue;
            }

            var line = GetLineNumber(xaml, match.Index);
            var attr = match.Groups["attr"].Value;
            yield return $"{Path.GetFileName(path)}:{line} {attr}=\"{value}\"";
        }
    }

    private static bool IsMarkupExpression(string value)
    {
        return value.Contains("{Binding", StringComparison.Ordinal)
            || value.Contains("{DynamicResource", StringComparison.Ordinal)
            || value.Contains("{StaticResource", StringComparison.Ordinal)
            || value.Contains("{x:Static", StringComparison.Ordinal);
    }

    private static int GetLineNumber(string text, int index)
    {
        var line = 1;
        for (var i = 0; i < index; i++)
        {
            if (text[i] == '\n')
            {
                line++;
            }
        }

        return line;
    }

    private static string ExtractDictionarySection(string source, string dictionaryName, string nextDictionaryName)
    {
        var startToken = $"private static readonly Dictionary<string, string> {dictionaryName} =";
        var nextToken = $"private static readonly Dictionary<string, string> {nextDictionaryName} =";
        var start = source.IndexOf(startToken, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Cannot find dictionary section {dictionaryName}.");
        var end = source.IndexOf(nextToken, start, StringComparison.Ordinal);
        Assert.True(end > start, $"Cannot find boundary from {dictionaryName} to {nextDictionaryName}.");

        return source[start..end];
    }

    private static async Task<bool> WaitUntilAsync(Func<bool> condition, int retry = 80, int delayMs = 20)
    {
        for (var attempt = 0; attempt < retry; attempt++)
        {
            if (condition())
            {
                return true;
            }

            await Task.Delay(delayMs);
        }

        return false;
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

    private sealed class RuntimeFixture : IAsyncDisposable
    {
        private readonly bool _cleanupRoot;

        private RuntimeFixture(string root, MAAUnifiedRuntime runtime, bool cleanupRoot)
        {
            Root = root;
            Runtime = runtime;
            _cleanupRoot = cleanupRoot;
        }

        public string Root { get; }

        public MAAUnifiedRuntime Runtime { get; }

        public static async Task<RuntimeFixture> CreateAsync(string? root = null, bool cleanupRoot = true)
        {
            root ??= Path.Combine(Path.GetTempPath(), "maa-unified-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(root, "config"));

            var log = new UiLogService();
            var diagnostics = new UiDiagnosticsService(root, log);
            var config = new UnifiedConfigurationService(
                new AvaloniaJsonConfigStore(root),
                new GuiNewJsonConfigImporter(),
                new GuiJsonConfigImporter(),
                log,
                root);
            await config.LoadOrBootstrapAsync();

            var bridge = new MaaCoreBridgeStub();
            var session = new UnifiedSessionService(bridge, config, log, new SessionStateMachine());
            var trayService = new CapturingTrayService();
            var platform = new PlatformServiceBundle
            {
                TrayService = trayService,
                NotificationService = new NoOpNotificationService(),
                HotkeyService = new NoOpGlobalHotkeyService(),
                AutostartService = new NoOpAutostartService(),
                FileDialogService = new NoOpFileDialogService(),
                OverlayService = new NoOpOverlayCapabilityService(),
                PostActionExecutorService = new NoOpPostActionExecutorService(),
            };

            var capability = new PlatformCapabilityFeatureService(platform, diagnostics);
            var connect = new ConnectFeatureService(session, config);
            var runtime = new MAAUnifiedRuntime
            {
                CoreBridge = bridge,
                ConfigurationService = config,
                ResourceWorkflowService = new ResourceWorkflowService(root, bridge, log),
                SessionService = session,
                Platform = platform,
                LogService = log,
                DiagnosticsService = diagnostics,
                ConnectFeatureService = connect,
                ShellFeatureService = new ShellFeatureService(connect),
                TaskQueueFeatureService = new TaskQueueFeatureService(session, config),
                CopilotFeatureService = new CopilotFeatureService(),
                ToolboxFeatureService = new ToolboxFeatureService(),
                RemoteControlFeatureService = new RemoteControlFeatureService(),
                PlatformCapabilityService = capability,
                OverlayFeatureService = new OverlayFeatureService(capability),
                NotificationProviderFeatureService = new NotificationProviderFeatureService(),
                SettingsFeatureService = new SettingsFeatureService(config, capability, diagnostics),
                DialogFeatureService = new DialogFeatureService(diagnostics),
                PostActionFeatureService = new PostActionFeatureService(
                    config,
                    diagnostics,
                    platform.PostActionExecutorService),
            };

            return new RuntimeFixture(root, runtime, cleanupRoot);
        }

        public async ValueTask DisposeAsync()
        {
            await Runtime.DisposeAsync();
            if (!_cleanupRoot)
            {
                return;
            }

            try
            {
                Directory.Delete(Root, recursive: true);
            }
            catch
            {
                // ignore cleanup failures in temporary test directories
            }
        }
    }

    private sealed class CapturingTrayService : ITrayService
    {
        public PlatformCapabilityStatus Capability { get; } = new(
            Supported: true,
            Message: "tray test service",
            Provider: "test");

        public event EventHandler<TrayCommandEvent>? CommandInvoked;

        public Task<PlatformOperationResult> InitializeAsync(
            string appTitle,
            TrayMenuText? menuText,
            CancellationToken cancellationToken = default)
            => Task.FromResult(PlatformOperation.NativeSuccess(Capability.Provider, "initialized", "tray.initialize"));

        public Task<PlatformOperationResult> ShutdownAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(PlatformOperation.NativeSuccess(Capability.Provider, "shutdown", "tray.shutdown"));

        public Task<PlatformOperationResult> ShowAsync(string title, string message, CancellationToken cancellationToken = default)
            => Task.FromResult(PlatformOperation.NativeSuccess(Capability.Provider, "show", "tray.show"));

        public Task<PlatformOperationResult> SetMenuStateAsync(TrayMenuState state, CancellationToken cancellationToken = default)
            => Task.FromResult(PlatformOperation.NativeSuccess(Capability.Provider, "set-menu", "tray.setMenuState"));

        public Task<PlatformOperationResult> SetVisibleAsync(bool visible, CancellationToken cancellationToken = default)
            => Task.FromResult(PlatformOperation.NativeSuccess(Capability.Provider, "set-visible", "tray.setVisible"));
    }
}
