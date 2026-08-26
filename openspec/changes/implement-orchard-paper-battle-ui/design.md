## Context

当前 `BattleUiLayout` 在 402×874 logical design 中以 `Header(8..104)` 和覆盖其余页面的 `BattleSurface(108..870)` 作为同级 standard panel，再在 `BattleSurface` 内绘制 ToolTray、NurseryTray 和 Detail 等闭合 standard/detail surface。该结构已经消除了框宽和文字溢出错误，却让一个外层框、两个常驻 section 框、槽位框和操作框同时争夺视觉重量；无选中详情时，`Detail(796..866)` 不绘制但仍留下明显空带。

运行时为 IMGUI。`BattleUiLayout` 和 `BattlefieldProjection` 是 draw/hit 的共同权威；release theme、有限 ArtSet、packaged Noto Sans SC、360/375/402/430 full+inset 和真实 WebGL 402×874 仍是硬约束。概念 D 只提供视觉目标，不可直接成为运行时截图或烘焙文字资产。

## Goals / Non-Goals

**Goals:**

- 建立 `paper page → light header/section → gameplay stage → detail/modal` 的有限结构层级，Battle 只有一个常态重框。
- 删除覆盖全部下半页的旧 `BattleSurface` 画框，让 stage、context tray、nursery 和 actions 直接在安全纸面上形成清楚节奏。
- 用同一 context tray 在默认态显示构筑工具、选中态显示可关闭详情，消除永久 Detail 空带；nursery、刷新和棋盘内开始波次保持可见。
- 把 stage frame 做成有限 ArtSet 语义槽，并通过 owned master、确定性导出、稳定 GUID 和 importer metadata 进入两个 production set。
- 以 Editor 结构门禁和 WebGL 最终像素证据证明层级、文字、full/inset 和输入均成立。

**Non-Goals:**

- 不改变战斗模拟、资源数值、波次、内容、存档、导航、拖拽语义或格子投影。
- 不重画植物、武器、花盆、地形和战斗特效。
- 不把概念图整页切片或运行时显示生成图中的中文。
- 不新增 UI 框架、主题切换器、旧 BattleSurface 回退或页面私有纹理加载。

## Decisions

### 1. 新增一个通用 `surface.gameplay-stage` 九宫格槽

有限合同从 55 槽直接升级到 56 槽，末尾新增 `surface.gameplay-stage`，不重排既有序号。该槽表示承载高密度可交互 gameplay 的唯一重结构框，当前由 Battle 棋盘使用；它不等同于通用 Raised panel、插画框或 modal。

选择新增语义槽而不是把 `surface.panel-raised` 改成木框，是因为 Raised 仍需服务未来详情/浮层，重木框会污染共享层级。也不借用 `surface.illustration-frame`，因为透明插画窗和可交互 gameplay stage 的安全边、对比与切片职责不同。

### 2. 生产 master 是独立无字边框，不是整页概念截图

stage master 为正方形透明中心九宫格，只包含窄胡桃木外框、克制内侧高光和低噪声手绘纹理；不得包含棋盘、植物、文字、按钮或角落大装饰。运行时导出为 128×128，固定 20px slice border，显著 Alpha 包络完全 contained。两个 production ArtSet 必须都绑定该槽；原位更新保留 `.meta` GUID。

概念 D 保存于 change evidence 只供人工比较。生产 master 需要单独审查，并由现有 exporter 记录 source/runtime hash、semantic id、slice border、safe inset 和 optical inset。

### 3. 删除大下半页画框，Stage 与 Header 共享轨道但不共享框重

`BattleSurface` 被 `BattleStage` 取代。Header 与 BattleStage 继续来自同一 full-width track helper，保证左右边一致；Header 使用 `surface.panel-standard`，BattleStage 使用 `surface.gameplay-stage`。Stage 只包围权威 Board/Projection，不再包含 ToolTray、NurseryTray、Refresh 或 Detail。

这是对旧“Header 与 BattleSurface 同 surface role”规则的有意修改：几何同轨继续保留，框重改由语义层级决定。替代方案是保留大外框并仅换颜色，但仍会形成 stage 与 controls 的双层套框，不能达到概念目标。

### 4. Context tray 在工具和详情之间互斥，不预留空区

布局只声明一个 `ContextTray`。无选中植物时，它绘制 Tool section；有选中植物时，同一 rect 绘制 Raised Detail，提供标题、有限单行属性和 44pt close action。两个状态不同时绘制，也不存在隐藏但占位的 Detail rect。

NurseryTray 与 RefreshAction 始终位于 ContextTray 下方；开始波次仍在 Board control strip。详情因此不会遮住主要流程动作。Draw 与 hit 均消费同一状态解析后的 rect，不创建视觉专用副本。

### 5. 参考布局采用固定 4pt 节奏和可测收口

402×874 参考布局保持 Header 为 96pt；BattleStage 以 Board rect 为外接框；Stage、ContextTray、NurseryTray、RefreshAction 之间只使用 4pt token 的组合。RefreshAction 下缘距 design/safe content 下缘为 8–40pt，页面不再留下 70pt 的空 Detail 带。

常态闭合结构面限定为 Header、BattleStage、ContextTray、NurseryTray 加 action/slot 自身；禁止恢复一个包围 stage 与 controls 的外层闭合 panel。Stage 最终可见 outline band 在 402 capture 中为 3–5px，standard section 为 1–2px；除 modal/detail overlay 外不得出现第二个 3px 以上的常态大框。

### 6. 验收分为结构、最终像素和人工相似性三层

Editor 测试验证 56 槽完整性、stage geometry/importer、同轨边界、无外层 BattleSurface、context 互斥、4pt rhythm、文字 fit、touch targets 和 draw/hit 同源。WebGL acceptance 从 live canvas 记录 stage/section 可见边、outline band、闭框清单、occupied bounds、底部余量和 full/inset copy containment。

人工评审只比较概念 D 的设计意图：战场第一焦点、下方纸面轻、主操作清楚、无多余装饰；不要求像素复刻生成图，也不以生成图中文作为正确文案。

## Risks / Trade-offs

- [Risk] 新 stage 框过重会缩小或遮挡棋盘 → Stage rect 不改变 `BattlefieldProjection` 的权威 Board，九宫格只绘制在既有视觉 gutter，Editor 检查 grid containment 与 draw/hit identity。
- [Risk] 详情替换工具后玩家暂时看不到装备 → Detail 必须有稳定 44pt close，Nursery、Refresh 和开始波次保持可见；详情本身只在明确选择后出现。
- [Risk] 生成 master 的纹理或边角不适合九宫格 → 先检查透明中心、四角保护区和最窄/最宽 gallery；失败则重做 master，不在运行时拉伸或遮盖修补。
- [Risk] 新槽使非 active production set 不完整 → 两个 set、manifest、ArtSet 和 56 槽 validator 在同一提交中升级，不提供缺槽 fallback。
- [Trade-off] Header 与 Stage 不再使用同一 surface role → 保留同轨几何和统一棕色家族，通过 1–2px 对 3–5px 的明确框重差建立真实层级。

## Migration Plan

1. 添加 `surface.gameplay-stage` 枚举、metadata、两个 production binding 和经审查的 master/runtime export。
2. 将 `BattleSurface` 直接替换为 `BattleStage`，将 Detail 与 Tool 合并为互斥 ContextTray；删除旧外层 panel 绘制和 Detail 空带路径。
3. 更新 Editor smoke、visual-system validator、gallery 与 56 槽断言，先跑 focused suites，再跑聚合 smoke。
4. 构建普通 WebGL，捕获 402×874 full/inset ready、paused、detail、active，记录 live outline/occupancy/text/input 证据。
5. 验收通过后同步 `docs/ui/ui-visual-system.md` 和主规格；回退通过撤销本变更完成，运行时不保留旧路径。

## Open Questions

无。概念方向、功能边界和验收路径已经由当前请求与现有规范确定。
