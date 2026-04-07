# MAAUnified 文档索引

本文档目录仅保留当前仍作为开发、迁移、对齐或测试依据的内容。

## 核心文档

- [`avalonia-migration.md`](./avalonia-migration.md)：配置迁移与导入规则
- [`avalonia-module-boundaries.md`](./avalonia-module-boundaries.md)：`MAAUnified` 目录内的分层边界
- [`avalonia-platform-degrade-strategy.md`](./avalonia-platform-degrade-strategy.md)：平台能力降级策略
- [`wpf-avalonia-field-mapping.md`](./wpf-avalonia-field-mapping.md)：WPF 与 Avalonia 页面、模块、设置项映射
- [`avalonia-parity-matrix.md`](./avalonia-parity-matrix.md)：面向阅读的功能对齐概览

## 基线与验收

- [`baseline.freeze.v1.md`](./baseline.freeze.v1.md)：当前冻结基线的可读投影
- [`baseline-change-control.v1.md`](./baseline-change-control.v1.md)：基线变更控制规则
- [`acceptance.checklist.template.v1.md`](./acceptance.checklist.template.v1.md)：验收清单模板

机读源位于 `Compat/Mapping/Baseline/`，上述文档中部分内容由机读基线生成或与之保持同步。

## 示例与参考

- [`config-import-report.example.json`](./config-import-report.example.json)：配置导入报告示例

## 维护约定

- 仅保留当前仍有明确用途的文档
- 阶段性发布记录、回滚演练、历史验收结果与已关闭问题不在此目录长期保留
- 若新增文档，请优先补充到本索引并说明其用途
