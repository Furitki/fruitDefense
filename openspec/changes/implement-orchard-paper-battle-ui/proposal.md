## Why

Battle 已经满足同轨、同级描边和有限文案 containment，但当前页面仍由多层相似米黄色闭合框组成，战场、信息区和操作区的视觉重量接近，导致页面规整却缺少明确焦点。已审查的概念方向证明更合适的目标是“单一木作战场框 + 轻量果园纸面控制区”；现在需要把该方向转成真实 Unity 组件、确定性资源和最终 WebGL 像素门禁，而不是把生成截图当作生产资产。

## What Changes

- 保留 A「阳光果园」的温暖浅表面、泥土棕、叶绿主操作和现有 gameplay content art，但将 Battle 的结构层级收敛为：页面纸面、唯一重边框战场、轻量控制分区、Raised 详情/模态。
- Battle 只允许战场使用重结构框；Header 与常驻 Tool/Nursery 区使用统一轻量纸面，read-only metric 和 section 不再通过重复闭合边框冒充层级。
- 重新分配 Battle 纵向节奏，使准备阶段、构筑栏、刷新栏和刷新操作形成连续收口；无详情态不得在主操作下留下无意图的大块空白，详情态仍保持可折叠且不遮挡持久主操作。
- 保持开始波次为 `Primary + icon/text + instantaneous`，刷新为 `Secondary + icon/text + instantaneous`，暂停/倍速为 `Quiet + icon/compact value + persistent mode`；不改变其命令、状态或 hit rect 语义。
- 在 release theme/ArtSet 中用有限语义角色实现轻纸面、重战场框和一致 outline/shadow，不新增页面私有颜色、局部纹理路径、旧样式回退或并行 layout。
- 将生成的 D 版概念图仅作为人工方向证据；生产资源必须来自 owned master，经现有确定性 exporter、稳定 GUID、ArtSet 和 importer 门禁进入 runtime。
- 扩展 Editor 与普通 WebGL 验收，量化结构层数、重边框唯一性、同轨左右边、section 闭框数量、底部占用、文字 fit、最终描边厚度和 full/inset 视觉平衡。
- 不改变战斗模拟、内容、数值、存档、导航、拖拽投影或平台授权状态。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `portrait-game-interface`: 明确 Battle 的单一战场视觉锚点、轻量控制分区和无详情/详情态纵向收口要求。
- `embedded-battle-control-surface`: 将旧“全部嵌入大 BattleSurface”的合同替换为 stage 外的 ContextTray、NurseryTray 与 RefreshAction，并保持 draw/hit 同源。
- `runtime-ui-quality-standard`: 增加重/轻 surface 层级、闭合框克制、唯一重边框和概念图到生产资源的可追溯合同。
- `webgl-visual-acceptance`: 增加 live Battle 截图中的层级、框重、闭框数量、文字 containment 和底部占用证据。

## Impact

- 运行时布局与绘制：`BattleUiLayout`、`FruitDefenseGame`、共享 `RuntimeUiGui`/theme surface 解析。
- 生产美术：Sunny Orchard Painted 的 surface master/runtime export、manifest、ArtSet 与 release theme；原位更新时保留路径和 `.meta` GUID。
- 自动门禁：Battle layout、runtime UI quality/visual-system smoke、aggregate smoke 和 WebGL acceptance manifest。
- 真实流程：普通 WebGL 402×874 full/inset 的 ready、paused、detail 与 active 状态；普通 WebGL 通过不代表抖音或微信转换成功。
