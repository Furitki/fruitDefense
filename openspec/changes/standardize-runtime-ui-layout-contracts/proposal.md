## Why

当前 Battle 顶栏与主战斗外框使用不同的横向轨道和不同层级的面板表面，导致最终画面中的上下框宽度、边线和视觉重量不一致。现有门禁能够证明矩形包含与有限静态文案可用，但没有把同级框轨、动态文案边界样本和最终像素描边一致性设为发布阻断项，因此同类问题仍可能在后续 UI 中反复出现。

## What Changes

- 将 Battle 顶栏与主战斗外框放入同一顶层横向轨道，并把标题、紧凑控制和资源指标拆成不重叠的两行排版。
- 统一同级结构面板的 surface/outline 角色；深度只由明确的 surface 层级表达，不允许页面通过不同外边距或局部描边制造偶然差异。
- 将 Battle 内部 section 的 inset、gap、标题行盒和内容轨道收敛到 4pt 间距节奏及命名布局常量，删除对应的散落尺寸。
- 为稳定 copy、格式化 copy、动态数值和详情组合补齐显式的 typography role、有限行策略、边界样本与 owner rect 测量；任何溢出、裁切或隐式缩字均阻断验证。
- 扩展 Editor 与普通 WebGL 验收，记录顶层框边对齐、同级 outline 最终像素、支持 viewport matrix 的文字 fit 与 occupied-content 结果。
- 保持现有 Primary/Secondary/Quiet/Danger 角色、content form、behavior 与交互状态合同；本变更不修改游戏规则、数值、存档、战斗结果或平台授权状态。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `portrait-game-interface`: 增加 Battle 顶层同轨、分行 header 排版、同级结构框一致性和动态文案有限布局要求。
- `runtime-ui-quality-standard`: 把命名布局轨道、4pt 节奏、面板 outline 合同及动态文案边界样本纳入统一质量门禁。
- `webgl-visual-acceptance`: 增加 live canvas 的框边/描边一致性与动态文字无溢出证据。

## Impact

- 运行时布局与绘制：`BattleUiLayout`、`FruitDefenseGame`、共享 UI drawing/layout helpers。
- 质量门禁：`RuntimeUiQualitySmoke`、`BattleUiLayoutSmoke`、`RuntimeUiVisualSystemValidator`、聚合 smoke 与 WebGL acceptance。
- 规格与长期规范：受影响的三个 OpenSpec capability；验收通过后同步 `docs/ui/ui-visual-system.md`，不改动游戏策划总览。
- 不新增依赖，不增加兼容层，不保留旧 Battle header/框轨路径。
