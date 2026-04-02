using MAAUnified.App.ViewModels.Infrastructure;
using MAAUnified.Application.Services.Localization;

namespace MAAUnified.App.ViewModels.Copilot;

public sealed class CopilotLocalizationTextMap : ObservableObject
{
    private static readonly Dictionary<string, string> ZhCn = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Copilot.Tab.Main"] = "主线/故事集/SideStory",
        ["Copilot.Tab.Security"] = "保全派驻",
        ["Copilot.Tab.Paradox"] = "悖论模拟",
        ["Copilot.Tab.Other"] = "其他活动",
        ["Copilot.Input.PathOrCodeWatermark"] = "作业路径/神秘代码",
        ["Copilot.Button.File"] = "文件",
        ["Copilot.Tip.File"] = "作业文件可以直接用鼠标拖进来哦 (oﾟvﾟ)ノ",
        ["Copilot.Button.Paste"] = "粘贴",
        ["Copilot.Tip.Paste"] = "读取剪贴板并添加为作业",
        ["Copilot.Button.PasteSet"] = "作业集",
        ["Copilot.Tip.PasteSet"] = "读取剪贴板并添加为作业集",
        ["Copilot.Action.Start"] = "开始",
        ["Copilot.Action.Stop"] = "停止",
        ["Copilot.Option.AutoSquad"] = "自动编队",
        ["Copilot.Tip.AutoSquad"] = "自动编队可能无法识别带有「特别关注」标记的干员",
        ["Copilot.Option.UseFormation"] = "使用编队",
        ["Copilot.Option.IgnoreRequirements"] = "忽略干员属性要求",
        ["Copilot.Tip.IgnoreRequirements"] = "某些作业需要特定模组等作为前置条件\n勾选此项将跳过这些检查，但可能导致作业无法正常运行",
        ["Copilot.Option.UseSupportUnit"] = "借助战",
        ["Copilot.Tip.UseSupportUnit"] = "缺一个还能用用，缺两个以上还是换份作业吧",
        ["Copilot.Option.AddTrust"] = "补充低信赖干员",
        ["Copilot.Option.AddUserAdditional"] = "追加自定干员",
        ["Copilot.Button.Edit"] = "编辑",
        ["Copilot.Tip.UserAdditionalFormat"] = "以英文 「;」 为分隔符，英文 「,」 分隔干员名与技能，例: 史尔特尔,3;艾雅法拉,1",
        ["Copilot.Popup.UserAdditionalTitle"] = "追加自定干员",
        ["Copilot.Input.OperatorNameWatermark"] = "干员名",
        ["Copilot.Button.Delete"] = "删除",
        ["Copilot.Button.Add"] = "添加",
        ["Copilot.Button.Confirm"] = "确定",
        ["Copilot.Button.Cancel"] = "取消",
        ["Copilot.Option.BattleList"] = "战斗列表",
        ["Copilot.Tip.BattleList"] = "仅支持以下模式:\n  1. 主线: 同一章节内导航\n  2. SideStory: 当前页面内导航（普通/EX/S 不能互跳）\n  3. 故事集: 当前页面内导航\n  4. 悖论模拟: 从干员列表启动\n请在对应界面启动，不支持跨章节导航\n\n当「战斗列表」启用后，选择单个作业时会自动添加到「战斗列表」",
        ["Copilot.Option.UseSanityPotion"] = "使用理智药",
        ["Copilot.Option.LoopTimes"] = "循环次数",
        ["Copilot.Button.Load"] = "载入",
        ["Copilot.Button.ImportBatch"] = "批量导入",
        ["Copilot.FilePicker.SelectTask.Title"] = "选择作业",
        ["Copilot.FilePicker.ImportBatch.Title"] = "批量导入作业",
        ["Copilot.Tip.ImportBatch"] = "批量导入",
        ["Copilot.Input.StageNameWatermark"] = "关卡名",
        ["Copilot.Tip.StageName"] = "关卡名, 例: 1-7",
        ["Copilot.Tip.AddList"] = "左键添加普通难度\n右键添加突袭难度",
        ["Copilot.Button.Clear"] = "清除",
        ["Copilot.Tip.ClearList"] = "左键清除所有任务\n右键清除未激活任务",
        ["Copilot.Rating.Prompt"] = "作业怎么样？评价下吧！",
        ["Copilot.Button.Like"] = "点赞",
        ["Copilot.Button.Dislike"] = "点踩",
        ["Copilot.Option.SupportUnitUsage.FillGap"] = "补漏",
        ["Copilot.Option.SupportUnitUsage.Random"] = "随机",
        ["Copilot.Option.Module.None"] = "不使用模组",
        ["Copilot.Option.Module.Chi"] = "χ",
        ["Copilot.Option.Module.Gamma"] = "γ",
        ["Copilot.Option.Module.Alpha"] = "α",
        ["Copilot.Option.Module.Delta"] = "Δ",
        ["Copilot.HelpText"] = "小提示:\n\n1. 使用前请确认作业与所选的关卡类型一致。\n\n2. 主线、故事集、SideStory: 请在关卡界面的右下角存在「开始行动」按钮界面启动。\n\n3. 保全派驻: resource/copilot 文件夹内置多份作业。请先手动编队，在右下角存在「开始部署」按钮界面启动，可配合「循环次数」。\n\n4. 悖论模拟: 选好技能后，在技能选择界面存在「开始模拟」按钮界面启动，1/2 星干员（无技能）在右下角存在「开始模拟」按钮界面开始。若使用「战斗列表」，请从干员列表「等级/稀有度」筛选下启动。\n\n5. 使用好友助战时，请关闭「自动编队」和「战斗列表」，手动选择干员后，在编队界面右下角存在「开始行动」按钮界面启动。\n\n6. 干员若被标记为「特别关注」，可能影响「自动编队」的识别与选择。建议使用「自动编队」时移除关注，或在报错后关闭「自动编队」，根据提示手动补充缺失的干员。\n\n7. Copilot 作业站的神秘代码可通过输入框右侧的粘贴按钮粘贴：\n● 单击第 2 个按钮 = 添加作业。\n● 单击第 3 个按钮 = 添加作业集。\n\n8. 战斗列表:\n● 选择作业后，检查下方关卡名是否正确 (例: CV-EX-1)。\n● 添加: 左键 = 普通难度，右键 = 突袭难度。\n● 清除: 左键 = 全部清空，右键 = 仅移除未激活任务。\n● 请在能看到目标关卡名的界面启动，不支持跨章节导航。\n● 遇到理智不足、战斗失败、未能三星结算时将自动中止。",
        ["Copilot.Hint.ListPanelEnabled"] = "战斗列表中的勾选项会参与启动。",
        ["Copilot.Hint.ListPanelDisabled"] = "当前将直接启动输入框中的单个作业。",
        ["Copilot.Status.ImportFileFailed"] = "导入作业文件失败。",
        ["Copilot.Status.ImportFilePersistRollback"] = "导入作业成功，但列表保存失败，已回滚。",
        ["Copilot.Error.ImportFileRetry"] = "导入作业文件失败，请检查路径和 JSON 格式后重试。",
        ["Copilot.Status.ImportClipboardFailed"] = "导入剪贴板作业失败。",
        ["Copilot.Status.ImportClipboardPersistRollback"] = "导入剪贴板成功，但列表保存失败，已回滚。",
        ["Copilot.Error.ImportClipboardRetry"] = "导入剪贴板作业失败，请检查路径或 JSON 内容后重试。",
        ["Copilot.Status.AddEmptyTaskSuccess"] = "已新增空白作业。",
        ["Copilot.Error.AddEmptyTaskPersistFail"] = "新增作业失败：列表保存失败。",
        ["Copilot.Status.RemoveFailed"] = "删除作业失败。",
        ["Copilot.Error.SelectTaskToRemove"] = "请选择要删除的作业。",
        ["Copilot.Status.RemoveSuccess"] = "已删除选中作业。",
        ["Copilot.Error.RemovePersistFail"] = "删除作业失败：列表保存失败。",
        ["Copilot.Status.ClearSuccess"] = "已清空作业列表。",
        ["Copilot.Error.ClearPersistFail"] = "清空作业列表失败：列表保存失败。",
        ["Copilot.Status.SortFailed"] = "排序失败。",
        ["Copilot.Error.SelectTaskToSort"] = "请选择要排序的作业。",
        ["Copilot.Status.AlreadyTop"] = "当前作业已在顶部。",
        ["Copilot.Status.MoveUpSuccess"] = "已将选中作业上移。",
        ["Copilot.Error.SortPersistFail"] = "排序失败：列表保存失败。",
        ["Copilot.Status.AlreadyBottom"] = "当前作业已在底部。",
        ["Copilot.Status.MoveDownSuccess"] = "已将选中作业下移。",
        ["Copilot.Status.StartFailed"] = "启动失败。",
        ["Copilot.Error.StartRunOwnerBlocked"] = "Copilot 启动被拦截：当前运行所有者为 `{0}`。",
        ["Copilot.Status.StopFailed"] = "停止失败。",
        ["Copilot.Error.StopRunOwnerBlocked"] = "Copilot 停止被拦截：当前运行所有者为 `{0}`。",
        ["Copilot.Error.AppendTaskFailed"] = "追加 Copilot 任务失败：{0} {1}",
        ["Copilot.Error.SessionStateNotAllowed"] = "会话状态 `{0}` 不允许{1}。\nSession state `{0}` does not allow {2}.",
        ["Copilot.Status.FeedbackFailed"] = "反馈失败。",
        ["Copilot.Error.SelectTaskForFeedback"] = "请选择要反馈的作业。",
        ["Copilot.Status.FeedbackSubmittedLike"] = "已对作业 `{0}` 提交点赞。",
        ["Copilot.Status.FeedbackSubmittedDislike"] = "已对作业 `{0}` 提交点踩。",
        ["Copilot.Status.NoSelection"] = "未选中作业。",
        ["Copilot.Status.SelectedItem"] = "已选中作业：{0}",
        ["Copilot.Status.CorruptedListIgnored"] = "已忽略损坏的 Copilot 列表配置。",
        ["Copilot.Error.ReadListInvalidJson"] = "读取作业列表失败：配置不是合法 JSON。{0}",
        ["Copilot.Error.ReadListNotArray"] = "读取作业列表失败：配置必须是 JSON 数组。",
        ["Copilot.Error.ReadListMissingName"] = "读取作业列表失败：列表项缺少可识别字段（例如 name）。",
        ["Copilot.Status.InputUpdated"] = "已更新作业输入。",
        ["Copilot.Status.LoadCurrentFailed"] = "读取作业失败。",
        ["Copilot.Error.ReadFileFailed"] = "读取作业文件失败。",
        ["Copilot.Status.LoadClipboardFailed"] = "读取剪贴板作业失败。",
        ["Copilot.Status.LoadSetFailed"] = "读取作业集失败。",
        ["Copilot.Error.LoadSetNotArray"] = "作业集必须是 JSON 数组。",
        ["Copilot.Error.LoadSetNoRecognized"] = "作业集内没有可识别的作业条目。",
        ["Copilot.Status.LoadSetAddedCount"] = "已添加 {0} 个作业到战斗列表。",
        ["Copilot.Status.BatchImportFailed"] = "批量导入失败。",
        ["Copilot.Error.BatchImportNone"] = "未找到可导入的作业文件。",
        ["Copilot.Status.BatchImportedCount"] = "已批量导入 {0} 个作业。",
        ["Copilot.Status.AddCurrentFailed"] = "添加作业失败。",
        ["Copilot.Error.SelectCopilotBeforeAdd"] = "请先选择一个作业。",
        ["Copilot.Status.AddCurrentRaidSuccess"] = "已添加突袭作业到战斗列表。",
        ["Copilot.Status.AddCurrentSuccess"] = "已添加作业到战斗列表。",
        ["Copilot.Status.DeleteItemSuccess"] = "已删除作业。",
        ["Copilot.Status.CleanInactiveNone"] = "没有未激活作业需要清理。",
        ["Copilot.Status.CleanInactiveCount"] = "已清理 {0} 个未激活作业。",
        ["Copilot.Error.ListItemMissingSource"] = "所选列表项缺少可用的作业来源。",
        ["Copilot.Error.LoadedFeedbackMissingId"] = "当前作业没有可反馈的作业站 ID。",
        ["Copilot.Status.LoadedFeedbackLikeSuccess"] = "已提交点赞。",
        ["Copilot.Status.LoadedFeedbackDislikeSuccess"] = "已提交点踩。",
        ["Copilot.Error.CurrentPayloadNotObject"] = "当前作业必须是单个 JSON 对象。",
        ["Copilot.Error.StartSelectTask"] = "请选择要执行的作业。",
        ["Copilot.Error.ValidateTabMismatch"] = "当前选择的作业与页签不匹配",
        ["Copilot.Error.ListNoChecked"] = "正在使用「战斗列表」，但未勾选任何作业。",
        ["Copilot.Error.ListLegacyMissingTab"] = "正在使用「战斗列表」，但列表包含旧版本条目（缺少页签信息），请在正确的页签重新添加这些作业后再启动",
        ["Copilot.Error.ListMixedTabs"] = "正在使用「战斗列表」，但不允许混用「主线/故事集/SideStory」与「悖论模拟」，请分别在对应页签建立列表后再启动",
        ["Copilot.Error.ListTabMismatch"] = "正在使用「战斗列表」，当前页签为「{0}」，但列表来自「{1}」，请切换到对应页签后再启动",
        ["Copilot.Warn.ListSingleItem"] = "正在使用「战斗列表」执行单个作业, 不推荐此行为。 单个作业请直接运行",
        ["Copilot.Error.ListEmptyStageName"] = "存在关卡名为空的作业",
        ["Copilot.Error.StartInputFileNotFound"] = "作业文件不存在：{0}",
        ["Copilot.Error.StartInputMissingSource"] = "当前作业缺少可执行来源（文件路径或 JSON 内容）。",
        ["Copilot.Tab.Display.Main"] = "主线/故事集/SideStory",
        ["Copilot.Tab.Display.Security"] = "保全派驻",
        ["Copilot.Tab.Display.Paradox"] = "悖论模拟",
        ["Copilot.Tab.Display.Other"] = "其他活动",
    };

    private static readonly Dictionary<string, string> EnUs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Copilot.Tab.Main"] = "Main / Story / SideStory",
        ["Copilot.Tab.Security"] = "SSS",
        ["Copilot.Tab.Paradox"] = "Paradox Simulation",
        ["Copilot.Tab.Other"] = "Other Activities",
        ["Copilot.Input.PathOrCodeWatermark"] = "Copilot path / secret code",
        ["Copilot.Button.File"] = "File",
        ["Copilot.Tip.File"] = "You can also drag a copilot file here.",
        ["Copilot.Button.Paste"] = "Paste",
        ["Copilot.Tip.Paste"] = "Read clipboard and add as copilot",
        ["Copilot.Button.PasteSet"] = "Set",
        ["Copilot.Tip.PasteSet"] = "Read clipboard and add as copilot set",
        ["Copilot.Action.Start"] = "Start",
        ["Copilot.Action.Stop"] = "Stop",
        ["Copilot.Option.AutoSquad"] = "Auto squad",
        ["Copilot.Tip.AutoSquad"] = "Auto squad may miss operators marked as Favorite.",
        ["Copilot.Option.UseFormation"] = "Use formation",
        ["Copilot.Option.IgnoreRequirements"] = "Ignore operator requirements",
        ["Copilot.Tip.IgnoreRequirements"] = "Some copilot files require modules or other prerequisites.\nEnabling this may skip checks and can cause failures.",
        ["Copilot.Option.UseSupportUnit"] = "Use support unit",
        ["Copilot.Tip.UseSupportUnit"] = "Missing one may still work, but missing more usually fails.",
        ["Copilot.Option.AddTrust"] = "Add low-trust operators",
        ["Copilot.Option.AddUserAdditional"] = "Additional custom operators",
        ["Copilot.Button.Edit"] = "Edit",
        ["Copilot.Tip.UserAdditionalFormat"] = "Use ';' to split entries and ',' to split name/skill, e.g. Exusiai,3;Eyjafjalla,1",
        ["Copilot.Popup.UserAdditionalTitle"] = "Additional custom operators",
        ["Copilot.Input.OperatorNameWatermark"] = "Operator name",
        ["Copilot.Button.Delete"] = "Delete",
        ["Copilot.Button.Add"] = "Add",
        ["Copilot.Button.Confirm"] = "Confirm",
        ["Copilot.Button.Cancel"] = "Cancel",
        ["Copilot.Option.BattleList"] = "Battle list",
        ["Copilot.Tip.BattleList"] = "Supported modes only:\n  1. Main: navigate in the same chapter\n  2. SideStory: navigate in current page\n  3. Story Collection: navigate in current page\n  4. Paradox Simulation: launch from operator list\nPlease start from the correct screen. Cross-chapter navigation is unsupported.\n\nWhen enabled, selecting one copilot auto-adds it to battle list.",
        ["Copilot.Option.UseSanityPotion"] = "Use sanity potion",
        ["Copilot.Option.LoopTimes"] = "Loop times",
        ["Copilot.Button.Load"] = "Load",
        ["Copilot.Button.ImportBatch"] = "Batch Import",
        ["Copilot.FilePicker.SelectTask.Title"] = "Select copilot",
        ["Copilot.FilePicker.ImportBatch.Title"] = "Import copilot batch",
        ["Copilot.Tip.ImportBatch"] = "Batch import",
        ["Copilot.Input.StageNameWatermark"] = "Stage name",
        ["Copilot.Tip.StageName"] = "Stage name, e.g. 1-7",
        ["Copilot.Tip.AddList"] = "Left click: normal\nRight click: raid",
        ["Copilot.Button.Clear"] = "Clear",
        ["Copilot.Tip.ClearList"] = "Left click: clear all\nRight click: clear inactive only",
        ["Copilot.Rating.Prompt"] = "How is this copilot? Leave feedback.",
        ["Copilot.Button.Like"] = "Like",
        ["Copilot.Button.Dislike"] = "Dislike",
        ["Copilot.Option.SupportUnitUsage.FillGap"] = "Fill gap",
        ["Copilot.Option.SupportUnitUsage.Random"] = "Random",
        ["Copilot.Option.Module.None"] = "No module",
        ["Copilot.Option.Module.Chi"] = "χ",
        ["Copilot.Option.Module.Gamma"] = "γ",
        ["Copilot.Option.Module.Alpha"] = "α",
        ["Copilot.Option.Module.Delta"] = "Δ",
        ["Copilot.HelpText"] = "Tips:\n\n1. Make sure the copilot matches the selected tab.\n\n2. Main/Story/SideStory: start on a page where the bottom-right action button is visible.\n\n3. SSS: built-in files are in resource/copilot. Prepare formation first and start on a page with deploy/start button. Loop is supported.\n\n4. Paradox: select skill first and start from the simulation page. If battle list is used, start from operator list.\n\n5. When using friend support, disable Auto squad and Battle list, choose operators manually, then start.\n\n6. Favorite marks may affect Auto squad recognition. Remove marks or disable Auto squad if needed.\n\n7. Secret codes from copilot site can be pasted with the right-side buttons:\n● 2nd button: add single copilot\n● 3rd button: add copilot set\n\n8. Battle list:\n● Verify stage name below (e.g. CV-EX-1).\n● Add: left=normal, right=raid.\n● Clear: left=all, right=inactive only.\n● Start from a page where target stage is visible. Cross-chapter navigation is unsupported.\n● The run stops automatically on sanity shortage, battle failure, or non-3-star result.",
        ["Copilot.Hint.ListPanelEnabled"] = "Checked items in battle list will be executed.",
        ["Copilot.Hint.ListPanelDisabled"] = "The single copilot from the input box will be executed.",
        ["Copilot.Status.ImportFileFailed"] = "Failed to import copilot file.",
        ["Copilot.Status.ImportFilePersistRollback"] = "Copilot imported, but list save failed and was rolled back.",
        ["Copilot.Error.ImportFileRetry"] = "Failed to import copilot file. Check file path and JSON format.",
        ["Copilot.Status.ImportClipboardFailed"] = "Failed to import copilot from clipboard.",
        ["Copilot.Status.ImportClipboardPersistRollback"] = "Clipboard copilot imported, but list save failed and was rolled back.",
        ["Copilot.Error.ImportClipboardRetry"] = "Failed to import from clipboard. Check path or JSON content.",
        ["Copilot.Status.AddEmptyTaskSuccess"] = "Empty copilot item added.",
        ["Copilot.Error.AddEmptyTaskPersistFail"] = "Add failed: list save failed.",
        ["Copilot.Status.RemoveFailed"] = "Failed to remove copilot item.",
        ["Copilot.Error.SelectTaskToRemove"] = "Select a copilot item to remove.",
        ["Copilot.Status.RemoveSuccess"] = "Selected copilot item removed.",
        ["Copilot.Error.RemovePersistFail"] = "Remove failed: list save failed.",
        ["Copilot.Status.ClearSuccess"] = "Copilot list cleared.",
        ["Copilot.Error.ClearPersistFail"] = "Clear failed: list save failed.",
        ["Copilot.Status.SortFailed"] = "Sort failed.",
        ["Copilot.Error.SelectTaskToSort"] = "Select a copilot item to sort.",
        ["Copilot.Status.AlreadyTop"] = "Selected item is already at the top.",
        ["Copilot.Status.MoveUpSuccess"] = "Selected item moved up.",
        ["Copilot.Error.SortPersistFail"] = "Sort failed: list save failed.",
        ["Copilot.Status.AlreadyBottom"] = "Selected item is already at the bottom.",
        ["Copilot.Status.MoveDownSuccess"] = "Selected item moved down.",
        ["Copilot.Status.StartFailed"] = "Start failed.",
        ["Copilot.Error.StartRunOwnerBlocked"] = "Copilot start blocked: current run owner is `{0}`.",
        ["Copilot.Status.StopFailed"] = "Stop failed.",
        ["Copilot.Error.StopRunOwnerBlocked"] = "Copilot stop blocked: current run owner is `{0}`.",
        ["Copilot.Error.AppendTaskFailed"] = "Failed to append copilot task: {0} {1}",
        ["Copilot.Error.SessionStateNotAllowed"] = "会话状态 `{0}` 不允许{1}。\nSession state `{0}` does not allow {2}.",
        ["Copilot.Status.FeedbackFailed"] = "Feedback failed.",
        ["Copilot.Error.SelectTaskForFeedback"] = "Select a copilot item to submit feedback.",
        ["Copilot.Status.FeedbackSubmittedLike"] = "Submitted like for `{0}`.",
        ["Copilot.Status.FeedbackSubmittedDislike"] = "Submitted dislike for `{0}`.",
        ["Copilot.Status.NoSelection"] = "No copilot item selected.",
        ["Copilot.Status.SelectedItem"] = "Selected copilot: {0}",
        ["Copilot.Status.CorruptedListIgnored"] = "Ignored corrupted copilot list configuration.",
        ["Copilot.Error.ReadListInvalidJson"] = "Failed to read copilot list: invalid JSON. {0}",
        ["Copilot.Error.ReadListNotArray"] = "Failed to read copilot list: payload must be a JSON array.",
        ["Copilot.Error.ReadListMissingName"] = "Failed to read copilot list: list items missing recognizable fields (e.g. name).",
        ["Copilot.Status.InputUpdated"] = "Copilot input updated.",
        ["Copilot.Status.LoadCurrentFailed"] = "Failed to load copilot.",
        ["Copilot.Error.ReadFileFailed"] = "Failed to read copilot file.",
        ["Copilot.Status.LoadClipboardFailed"] = "Failed to read clipboard copilot.",
        ["Copilot.Status.LoadSetFailed"] = "Failed to load copilot set.",
        ["Copilot.Error.LoadSetNotArray"] = "Copilot set must be a JSON array.",
        ["Copilot.Error.LoadSetNoRecognized"] = "No recognizable copilot entries found in set.",
        ["Copilot.Status.LoadSetAddedCount"] = "Added {0} copilot item(s) to battle list.",
        ["Copilot.Status.BatchImportFailed"] = "Batch import failed.",
        ["Copilot.Error.BatchImportNone"] = "No importable copilot files found.",
        ["Copilot.Status.BatchImportedCount"] = "Batch imported {0} copilot item(s).",
        ["Copilot.Status.AddCurrentFailed"] = "Add failed.",
        ["Copilot.Error.SelectCopilotBeforeAdd"] = "Select a copilot first.",
        ["Copilot.Status.AddCurrentRaidSuccess"] = "Raid copilot added to battle list.",
        ["Copilot.Status.AddCurrentSuccess"] = "Copilot added to battle list.",
        ["Copilot.Status.DeleteItemSuccess"] = "Copilot item deleted.",
        ["Copilot.Status.CleanInactiveNone"] = "No inactive items to clean.",
        ["Copilot.Status.CleanInactiveCount"] = "Cleaned {0} inactive item(s).",
        ["Copilot.Error.ListItemMissingSource"] = "The selected item has no available source.",
        ["Copilot.Error.LoadedFeedbackMissingId"] = "Current copilot has no feedback ID.",
        ["Copilot.Status.LoadedFeedbackLikeSuccess"] = "Like submitted.",
        ["Copilot.Status.LoadedFeedbackDislikeSuccess"] = "Dislike submitted.",
        ["Copilot.Error.CurrentPayloadNotObject"] = "Current copilot payload must be a single JSON object.",
        ["Copilot.Error.StartSelectTask"] = "Select a copilot item to execute.",
        ["Copilot.Error.ValidateTabMismatch"] = "Selected copilot does not match current tab.",
        ["Copilot.Error.ListNoChecked"] = "Battle list mode is enabled but no item is checked.",
        ["Copilot.Error.ListLegacyMissingTab"] = "Battle list contains legacy items without tab info. Re-add them in the correct tab.",
        ["Copilot.Error.ListMixedTabs"] = "Battle list cannot mix Main/Story/SideStory with Paradox Simulation.",
        ["Copilot.Error.ListTabMismatch"] = "Battle list tab mismatch: current tab is `{0}`, list tab is `{1}`.",
        ["Copilot.Warn.ListSingleItem"] = "Battle list mode is used with only one item. Direct run is recommended.",
        ["Copilot.Error.ListEmptyStageName"] = "Found item(s) with empty stage name.",
        ["Copilot.Error.StartInputFileNotFound"] = "Copilot file not found: {0}",
        ["Copilot.Error.StartInputMissingSource"] = "Current copilot has no executable source (file path or JSON content).",
        ["Copilot.Tab.Display.Main"] = "Main / Story / SideStory",
        ["Copilot.Tab.Display.Security"] = "SSS",
        ["Copilot.Tab.Display.Paradox"] = "Paradox Simulation",
        ["Copilot.Tab.Display.Other"] = "Other Activities",
    };

    private static readonly Dictionary<string, string> ZhTw = new(ZhCn, StringComparer.OrdinalIgnoreCase)
    {
        ["Copilot.Tab.Main"] = "主線/故事集/SideStory",
        ["Copilot.Tab.Security"] = "保全派駐",
        ["Copilot.Tab.Paradox"] = "悖論模擬",
        ["Copilot.Tab.Other"] = "其他活動",
        ["Copilot.Button.Paste"] = "貼上",
        ["Copilot.Option.BattleList"] = "戰鬥列表",
        ["Copilot.Button.Like"] = "按讚",
        ["Copilot.Button.Dislike"] = "倒讚",
        ["Copilot.Status.AddEmptyTaskSuccess"] = "已新增空白作業。",
        ["Copilot.Status.RemoveSuccess"] = "已刪除選中作業。",
        ["Copilot.Status.ClearSuccess"] = "已清空作業列表。",
        ["Copilot.Status.MoveUpSuccess"] = "已將選中作業上移。",
        ["Copilot.Status.MoveDownSuccess"] = "已將選中作業下移。",
    };

    private static readonly Dictionary<string, string> JaJp = new(EnUs, StringComparer.OrdinalIgnoreCase)
    {
        ["Copilot.Tab.Main"] = "メイン/ストーリー/SideStory",
        ["Copilot.Tab.Security"] = "保全駐在",
        ["Copilot.Tab.Paradox"] = "逆理演算",
        ["Copilot.Tab.Other"] = "その他",
        ["Copilot.Input.PathOrCodeWatermark"] = "作業パス/シークレットコード",
        ["Copilot.Button.File"] = "ファイル",
        ["Copilot.Button.Paste"] = "貼り付け",
        ["Copilot.Button.PasteSet"] = "作業セット",
        ["Copilot.Action.Start"] = "開始",
        ["Copilot.Action.Stop"] = "停止",
        ["Copilot.Option.AutoSquad"] = "自動編成",
        ["Copilot.Option.UseFormation"] = "編成を使用",
        ["Copilot.Option.IgnoreRequirements"] = "オペレーター条件を無視",
        ["Copilot.Option.UseSupportUnit"] = "サポートを使用",
        ["Copilot.Option.AddTrust"] = "低信頼オペレーターを補充",
        ["Copilot.Option.AddUserAdditional"] = "カスタムオペレーター追加",
        ["Copilot.Button.Edit"] = "編集",
        ["Copilot.Input.OperatorNameWatermark"] = "オペレーター名",
        ["Copilot.Option.BattleList"] = "戦闘リスト",
        ["Copilot.Option.UseSanityPotion"] = "理性剤を使用",
        ["Copilot.Option.LoopTimes"] = "ループ回数",
        ["Copilot.Button.Load"] = "読み込み",
        ["Copilot.Button.ImportBatch"] = "一括インポート",
        ["Copilot.FilePicker.SelectTask.Title"] = "作業を選択",
        ["Copilot.FilePicker.ImportBatch.Title"] = "作業を一括インポート",
        ["Copilot.Input.StageNameWatermark"] = "ステージ名",
        ["Copilot.Button.Clear"] = "クリア",
        ["Copilot.Button.Like"] = "高評価",
        ["Copilot.Button.Dislike"] = "低評価",
        ["Copilot.Button.Confirm"] = "確認",
        ["Copilot.Button.Cancel"] = "キャンセル",
        ["Copilot.Button.Add"] = "追加",
        ["Copilot.Button.Delete"] = "削除",
        ["Copilot.Tab.Display.Main"] = "メイン/ストーリー/SideStory",
        ["Copilot.Tab.Display.Security"] = "保全駐在",
        ["Copilot.Tab.Display.Paradox"] = "逆理演算",
        ["Copilot.Tab.Display.Other"] = "その他",
    };

    private static readonly Dictionary<string, string> KoKr = new(EnUs, StringComparer.OrdinalIgnoreCase)
    {
        ["Copilot.Tab.Main"] = "메인/스토리/SideStory",
        ["Copilot.Tab.Security"] = "보전 주둔",
        ["Copilot.Tab.Paradox"] = "역리 시뮬레이션",
        ["Copilot.Tab.Other"] = "기타 활동",
        ["Copilot.Input.PathOrCodeWatermark"] = "작업 경로/시크릿 코드",
        ["Copilot.Button.File"] = "파일",
        ["Copilot.Button.Paste"] = "붙여넣기",
        ["Copilot.Button.PasteSet"] = "작업 세트",
        ["Copilot.Action.Start"] = "시작",
        ["Copilot.Action.Stop"] = "중지",
        ["Copilot.Option.AutoSquad"] = "자동 편성",
        ["Copilot.Option.UseFormation"] = "편성 사용",
        ["Copilot.Option.IgnoreRequirements"] = "오퍼레이터 조건 무시",
        ["Copilot.Option.UseSupportUnit"] = "지원 유닛 사용",
        ["Copilot.Option.AddTrust"] = "저신뢰 오퍼레이터 보충",
        ["Copilot.Option.AddUserAdditional"] = "커스텀 오퍼레이터 추가",
        ["Copilot.Button.Edit"] = "편집",
        ["Copilot.Input.OperatorNameWatermark"] = "오퍼레이터 이름",
        ["Copilot.Option.BattleList"] = "전투 목록",
        ["Copilot.Option.UseSanityPotion"] = "이성 물약 사용",
        ["Copilot.Option.LoopTimes"] = "반복 횟수",
        ["Copilot.Button.Load"] = "불러오기",
        ["Copilot.Button.ImportBatch"] = "일괄 가져오기",
        ["Copilot.FilePicker.SelectTask.Title"] = "작업 선택",
        ["Copilot.FilePicker.ImportBatch.Title"] = "작업 일괄 가져오기",
        ["Copilot.Input.StageNameWatermark"] = "스테이지 이름",
        ["Copilot.Button.Clear"] = "지우기",
        ["Copilot.Button.Like"] = "좋아요",
        ["Copilot.Button.Dislike"] = "싫어요",
        ["Copilot.Button.Confirm"] = "확인",
        ["Copilot.Button.Cancel"] = "취소",
        ["Copilot.Button.Add"] = "추가",
        ["Copilot.Button.Delete"] = "삭제",
        ["Copilot.Tab.Display.Main"] = "메인/스토리/SideStory",
        ["Copilot.Tab.Display.Security"] = "보전 주둔",
        ["Copilot.Tab.Display.Paradox"] = "역리 시뮬레이션",
        ["Copilot.Tab.Display.Other"] = "기타 활동",
    };

    private string _language = UiLanguageCatalog.DefaultLanguage;

    public string Language
    {
        get => _language;
        set
        {
            var normalized = UiLanguageCatalog.Normalize(value);
            if (!SetProperty(ref _language, normalized))
            {
                return;
            }

            OnPropertyChanged("Item[]");
        }
    }

    public string this[string key]
    {
        get
        {
            var source = ResolveSource(Language);
            if (source.TryGetValue(key, out var value))
            {
                return value;
            }

            if (EnUs.TryGetValue(key, out value))
            {
                return value;
            }

            if (ZhCn.TryGetValue(key, out value))
            {
                return value;
            }

            return key;
        }
    }

    public string GetOrDefault(string key, string fallback)
    {
        var value = this[key];
        return string.Equals(value, key, StringComparison.Ordinal) ? fallback : value;
    }

    private static Dictionary<string, string> ResolveSource(string language)
    {
        if (string.Equals(language, "en-us", StringComparison.OrdinalIgnoreCase))
        {
            return EnUs;
        }

        if (string.Equals(language, "zh-tw", StringComparison.OrdinalIgnoreCase))
        {
            return ZhTw;
        }

        if (string.Equals(language, "ja-jp", StringComparison.OrdinalIgnoreCase))
        {
            return JaJp;
        }

        if (string.Equals(language, "ko-kr", StringComparison.OrdinalIgnoreCase))
        {
            return KoKr;
        }

        return ZhCn;
    }
}
