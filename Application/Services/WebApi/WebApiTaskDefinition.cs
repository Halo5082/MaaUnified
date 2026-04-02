using System.Text.Json.Nodes;
using MAAUnified.CoreBridge;

namespace MAAUnified.Application.Services.WebApi;

public sealed record WebApiTaskDefinition(
    int Id,
    string TaskType,
    string Name,
    JsonObject? Parameters,
    bool Enabled)
{
    public WebApiTaskDefinition WithParameters(JsonObject? parameters)
        => this with { Parameters = parameters };

    public CoreTaskRequest ToCoreTaskRequest()
    {
        var payload = Parameters?.ToJsonString() ?? "{}";
        return new CoreBridge.CoreTaskRequest(TaskType, Name, Enabled, payload);
    }
}
