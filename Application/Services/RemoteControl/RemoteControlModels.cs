using System.Text.Json.Nodes;

namespace MAAUnified.Application.Services.RemoteControl;

internal sealed record RemoteControlPollingSnapshot(
    Uri? GetTaskEndpoint,
    Uri? ReportEndpoint,
    string UserIdentity,
    string DeviceIdentity,
    int PollIntervalMs)
{
    public bool IsConfigured => GetTaskEndpoint is not null && ReportEndpoint is not null;
}

internal sealed record RemoteControlCommandRequest(
    string RawCommand,
    JsonNode? Payload,
    string UserIdentity,
    string DeviceIdentity);

internal sealed record RemoteControlCommandResult(
    string RawCommand,
    string NormalizedCommand,
    bool Success,
    string Message,
    string? ErrorCode = null,
    string? Details = null,
    int? CoreTaskId = null,
    string? TaskType = null,
    byte[]? ImageBytes = null,
    string? ImageContentType = null);
