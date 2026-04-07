# MAAUnified

`MAAUnified` 是 MAA 的跨平台图形前端，基于 Avalonia 构建。

本项目的初衷，是为 MAA 提供一套可持续演进的跨平台 GUI。相较于继续在现有实现上局部修补，独立维护一套统一前端更适合承担 macOS 与 Linux 的图形界面，并为后续能力扩展预留清晰边界。

在项目规划上，`MAAUnified` 将长期与 WPF 前端并行演进，优先面向 macOS 与 Linux 使用场景，逐步完善功能与平台能力。代码结构自始即按独立仓库组织，并计划以 `submodule` 形式接入宿主仓库。

## 技术栈

- .NET `10.0`
- Avalonia `11.2.8`
- C#
- xUnit

SDK 版本沿用主仓库在 [`global.json`](./global.json) 中指定的版本，当前为 `10.0.201`。

## 构建与运行

独立仓库形态下：

```bash
dotnet restore App/MAAUnified.App.csproj
dotnet run --project App/MAAUnified.App.csproj
dotnet test Tests/MAAUnified.Tests.csproj -c Release
```

在 `MaaAssistantArknights` 宿主仓库中联调时，请先进入 `src/MAAUnified/` 后再执行上述命令。

## 配置约定

- 主配置文件：`config/avalonia.json`
- 自动导入条件：`avalonia.json` 不存在
- 导入顺序：`gui.new.json` -> `gui.json` -> 默认值
- 旧配置文件仅作为读取来源，不会被回写覆盖

## 暂未支持

- macOS / Linux 显卡能力支持：该部分涉及 MaaCore 边界，当前暂不实现
- macOS / Linux 关闭模拟器功能：仍需按平台分别适配，暂未完成调试
- MaaUnified 的软件更新：暂未开发更新相关功能

## 目录结构

- [`App/`](./App/)：应用入口、视图、样式、ViewModel 与 UI 服务
- [`Application/`](./Application/)：配置、运行时编排、功能服务、诊断与多语言资源
- [`Platform/`](./Platform/)：托盘、通知、热键、自启动、Overlay 等平台能力封装
- [`CoreBridge/`](./CoreBridge/)：MaaCore 桥接层与调试替身
- [`Compat/`](./Compat/)：兼容映射、历史字段与默认值适配
- [`Tests/`](./Tests/)：单元测试、契约测试与回归测试
- [`Docs/`](./Docs/)：迁移文档、基线说明、平台策略与映射说明
- [`CI/`](./CI/)：独立仓库与宿主仓库的 CI 模板

## 开发原则

- 以现有 WPF 行为为主要参考，逐步完成配置语义、交互逻辑与平台能力收口
- 变更范围尽量限定在 `src/MAAUnified/**` 内
- 涉及 MaaCore 边界的能力调整单独处理，不在前端层强行扩展

## 相关文档

- [`Docs/README.md`](./Docs/README.md)
- [`Docs/avalonia-migration.md`](./Docs/avalonia-migration.md)
- [`Docs/avalonia-parity-matrix.md`](./Docs/avalonia-parity-matrix.md)
- [`Docs/avalonia-platform-degrade-strategy.md`](./Docs/avalonia-platform-degrade-strategy.md)
- [`Docs/wpf-avalonia-field-mapping.md`](./Docs/wpf-avalonia-field-mapping.md)
