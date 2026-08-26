## Context

Battle 使用固定 402×874 logical design，由 `BattleUiLayout` 同时提供 draw/hit geometry，并通过统一 viewport matrix 投影到 360/375/402/430 full+inset。原问题不在 safe-area 投影，而在 design 内部：Header 与旧 `BattleSurface` 使用不同横向 owner track，资源指标又与 52×52 控制共用同一纵向带。最终结构基线将 Header 与 `BattleStage` 对齐到同一 owner track，但不再让两者共享框重：Header 和常驻 section 使用轻量结构框，BattleStage 是唯一常态重框，且下部控制栈没有第二层包围外框。

共享 visual system 已要求 4pt spacing、有限行策略、同层级 outline role 和最终像素验收。本变更把这些稳定要求落到 Battle layout authority、共享资产验证和聚合 smoke 中；不新增第二套布局或 hit rect。

## Goals / Non-Goals

**Goals:**

- Header 与 BattleStage 使用相同的顶层横向 owner track；Header/常驻 section 保持 1–2 capture-pixel 轻框，BattleStage 作为唯一常态重框使用 3–5 capture-pixel outline，且下部控制栈没有第二外框。
- Header 将标题/控制与资源指标分为两行，给有限动态数值留下明确 owner rect。
- 内部 section 使用命名的 4pt-derived inset/gap/line-box 常量，并保持 draw/hit 同源。
- 稳定、格式化和动态文案都有明确 role、line policy、边界样本和全 viewport fit gate。
- Editor gate 先阻断结构错误，普通 WebGL 再验证最终像素和 live canvas。

**Non-Goals:**

- 不修改战斗模拟、资源计算、波次、内容定义、存档或导航。
- 不迁移 uGUI/UI Toolkit，不新增响应式框架、运行时缩字、ellipsis、fallback skin 或兼容旧 header。
- 不以普通 WebGL 结果声明抖音或微信小游戏支持。

## Decisions

### 1. 顶层共享 full-width owner track，结构框重按职责分层

`Header` 与 `BattleStage` 都从 `BattleUiLayout` 的同一 full-width owner-track helper 产生，保证最终可见左右边对齐。Header 和常驻 Context/Nursery section 使用 light standard family，最终 outline 为 1–2 capture pixels；BattleStage 使用 `surface.gameplay-stage`，是正常 Battle 中唯一 3–5 capture-pixel heavy frame。Stage 只包围权威 Board/Projection，下部控制栈不再由第二个外框整体包围。顶层之间保留 4pt gap，内部 Context/Nursery/Refresh 使用声明的共享 owner/inset track。

共享 owner track 负责几何一致性，语义 surface role 负责结构重量；两者不再被错误绑定。轻量 Header/常驻 section 与重型 BattleStage 在所有 ArtSet、状态和缩放下保持明确的 1–2px 对 3–5px 层级；Raised 只留给真正覆盖内容的 detail/modal，不作为顶栏默认边线或第二个页面外框。

### 2. Header 与下部 section 使用真实 typography line-height

第一行承载标题与两个 52×52 Quiet persistent-mode 控制；第二行承载三个等宽 micro metric group。Header 增高，Board 顶部与底部共同上移/收敛，为下部 section 释放真实行高所需空间；地图网格仍由宽度约束保持原 tile size。ContextTray 的 tool/detail anatomy 使用各自完整语义 line-height，Nursery title/slot label 同理，且四边至少保留 4pt。

`SingleLineText` 的 fixed height 改为 theme line-height；owner 低于该高度时直接报告布局合同失败，不再用 `Min(owner, lineHeight)` 静默压扁。ContextTray/NurseryTray 随之重排并重新分配 Board 高度。这比在旧 owner 中继续挤压或缩小文字更稳定，也不需要按文案长度走页面分支。

### 3. 布局节奏由少量命名常量表达

Battle layout 暴露 `SpacingUnit=4`、`SectionGap=8`、`ContentInset=8` 和顶层/内部轨 helper。Context/Nursery 的 gap 改为 4pt token；其余历史 magic number 只在属于独立组件尺寸（52 touch target、18 micro icon、44 action）时保留。

不引入通用 layout framework；固定 portrait composition 只需要一个明确、可测的 layout authority。

### 4. 文案门禁覆盖真实格式化边界

继续以 `RuntimeUiCopyCatalog` 管理稳定 copy，同时在 inspection catalog/route smoke 中为 header 数值、刷新 cost、inventory count、星级、详情标题/正文、merge hint 和 transient status（包含真实状态前缀）注册发布边界样本。每个样本以 packaged font、最终 role、line policy 和 owner rect 在全部 full/inset viewport 中测量。inspection 通过 `BattleUiActionSemantic` 解析真实 action spec，禁止再次把 Secondary refresh 手写成 Primary。

运行时不截断、不省略、不自动缩字。若内容域扩大，提交者必须先扩大 owner、缩短批准 copy 或新增受控多行 anatomy，并更新边界样本。

### 5. 结构门禁与最终像素门禁分层

Editor smoke 检查 owner track 对齐、4pt 节奏、包含/不重叠、draw/hit 同源、唯一 heavy-stage 结构、无第二包围外框、文本 fit 和 ArtSet panel metadata。WebGL acceptance 在 402×874 full+inset 捕获 live canvas，测量顶层可见左右边、Header/常驻 section 的 1–2px outline band、BattleStage 的 3–5px outline band、文字 ink 是否逃逸 owner，并保留 build/theme/ArtSet identity。

源码字符串断言只作为 wiring 辅助，不作为几何或视觉质量的唯一证明。

## Risks / Trade-offs

- [Header 与下部 section 增高会压缩 Board 高度] → 保持 402pt 横向投影和原 tile size 所需的最小 map viewport，重新验证 GridRect、route、core、pots 与 wave action，同一 projection 继续驱动输入。
- [4pt gap 产生半 logical point 的等分宽度] → 接受 logical-space 等分并由现有统一 viewport/nine-slice device-boundary snapping 处理；测试比较边界与节奏，不用逐控件私有补偿。
- [动态文本域未来扩大] → 边界样本成为发布合同；域扩大但未更新 owner/line policy 时 smoke 必须失败。
- [旧证据仍显示不一致布局] → 新 evidence 使用新目录与新 payload identity，旧截图只保留历史用途且不能满足本变更。

## Migration Plan

1. 更新 layout authority 与对应 draw wiring，将旧 `BattleSurface` 替换为同 owner track 的 `BattleStage`，删除 raised Header path 和包围下部控制栈的第二外框。
2. 更新 geometry、text、ArtSet 和 aggregate validation；先跑 focused Editor suites，再跑聚合 smoke。
3. 构建普通 WebGL，捕获 402×874 full+inset 的 ready/paused/detail/terminal 代表状态并执行最终像素检查。
4. 验收通过后同步主规格和 `docs/ui/ui-visual-system.md`；不保留旧布局开关或回退分支。

## Open Questions

无。当前请求已明确要求统一框轨、描边和溢出防护。
