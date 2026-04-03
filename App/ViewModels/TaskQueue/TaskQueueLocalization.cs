using MAAUnified.App.ViewModels.Infrastructure;

namespace MAAUnified.App.ViewModels.TaskQueue;

public sealed class LocalizedTextMap : UiTextMapAdapter
{
    public LocalizedTextMap()
        : base("TaskQueue.Localization")
    {
    }
}
