using System.Text.Json.Nodes;
using MAAUnified.Application.Models;

namespace MAAUnified.Application.Services.RemoteControl;

internal sealed class RemoteControlCommandDispatcher
{
    public RemoteControlCommandDispatcher(
        object? configService,
        object? sessionService,
        object? connectFeatureService,
        object? taskQueueFeatureService,
        object? toolboxFeatureService,
        object? coreBridge,
        object? logService)
    {
    }

    public Task<RemoteControlCommandResult> DispatchAsync(
        RemoteControlCommandRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var command = string.IsNullOrWhiteSpace(request.RawCommand)
            ? string.Empty
            : request.RawCommand.Trim();
        return Task.FromResult(new RemoteControlCommandResult(
            request.RawCommand,
            command,
            Success: false,
            "Remote control command dispatch is not implemented in the unified shell.",
            ErrorCode: UiErrorCode.RemoteControlUnsupported));
    }
}
