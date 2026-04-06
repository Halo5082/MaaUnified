using System.Reflection;
using MAAUnified.Platform;

namespace MAAUnified.Tests;

public sealed class PostActionExecutorSupportTests
{
    [Fact]
    public void CommandPostActionExecutorService_GetCapabilityMatrix_ShouldReportWindowsOnlySupportForExitEmulator()
    {
        var service = new CommandPostActionExecutorService();
        var request = new PostActionExecutorRequest(ConnectAddress: "127.0.0.1:5555", ConnectConfig: "LDPlayer", AdbPath: "adb");

        var capability = service.GetCapabilityMatrix(request).Get(PostActionType.ExitEmulator);

        if (OperatingSystem.IsWindows())
        {
            Assert.True(capability.Supported, capability.Message);
            return;
        }

        Assert.False(capability.Supported);
        Assert.Contains("unsupported", capability.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WindowsEmulatorExitHelper_TryResolveMuMuIndex_ShouldPreferBridgeIndexAndPortRules()
    {
        var helperType = typeof(CommandPostActionExecutorService).Assembly.GetType("MAAUnified.Platform.WindowsEmulatorExitHelper");
        Assert.NotNull(helperType);

        var method = helperType!.GetMethod(
            "TryResolveMuMuIndex",
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.NotNull(method);

        var byBridge = new PostActionExecutorRequest(
            ConnectAddress: "127.0.0.1:16416",
            ConnectConfig: "MuMuEmulator12",
            MuMuBridgeConnection: true,
            MuMu12Index: "3");
        var byPort = new PostActionExecutorRequest(
            ConnectAddress: "127.0.0.1:16448",
            ConnectConfig: "MuMuEmulator12");

        object?[] bridgeArgs = [byBridge, 0];
        object?[] portArgs = [byPort, 0];
        Assert.True((bool)method!.Invoke(null, bridgeArgs)!);
        Assert.Equal(3, Assert.IsType<int>(bridgeArgs[1]));
        Assert.True((bool)method.Invoke(null, portArgs)!);
        Assert.Equal(2, Assert.IsType<int>(portArgs[1]));
    }

    [Fact]
    public void WindowsEmulatorExitHelper_TryResolveLdPlayerIndex_ShouldSupportManualAndSerialFormats()
    {
        var helperType = typeof(CommandPostActionExecutorService).Assembly.GetType("MAAUnified.Platform.WindowsEmulatorExitHelper");
        Assert.NotNull(helperType);

        var method = helperType!.GetMethod(
            "TryResolveLdPlayerIndex",
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.NotNull(method);

        var manual = new PostActionExecutorRequest(
            ConnectAddress: "127.0.0.1:5555",
            ConnectConfig: "LDPlayer",
            LdPlayerManualSetIndex: true,
            LdPlayerIndex: "4");
        var serial = new PostActionExecutorRequest(
            ConnectAddress: "emulator-5558",
            ConnectConfig: "LDPlayer");

        object?[] manualArgs = [manual, 0];
        object?[] serialArgs = [serial, 0];
        Assert.True((bool)method!.Invoke(null, manualArgs)!);
        Assert.Equal(4, Assert.IsType<int>(manualArgs[1]));
        Assert.True((bool)method.Invoke(null, serialArgs)!);
        Assert.Equal(2, Assert.IsType<int>(serialArgs[1]));
    }

    [Fact]
    public void CommandPostActionExecutorService_GetCapabilityMatrix_ShouldNotUseLegacyCommandLineFallback_ForCoreManagedActions()
    {
        var service = new CommandPostActionExecutorService();
        var withLegacyCommand = new PostActionExecutorRequest(CommandLine: "echo close-maa");
        var capability = service.GetCapabilityMatrix(withLegacyCommand);

        Assert.False(capability.ExitArknights.Supported);
        Assert.False(capability.BackToAndroidHome.Supported);
        Assert.False(capability.ExitSelf.Supported);
        Assert.Contains("requires native provider", capability.ExitArknights.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("requires native provider", capability.BackToAndroidHome.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("requires native provider", capability.ExitSelf.Message, StringComparison.OrdinalIgnoreCase);
    }
}
