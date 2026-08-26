> 1-6 节记录已完成但被真实画面否决的实现历史，不再构成当前验收。7 节是本次语义 container/content 返工的有效任务；完成前本 change 不得归档。

## 1. Imagegen 资源与 ArtSet 合同

- [x] 1.1 检查 `sunny-orchard` 的现有 action surface、图标线重和目标 52×52 可读性，通过 Codex 内置 imagegen 接口生成该 ArtSet 的 `action.compact-control` 与 `action.compact-control-active` 原始候选；记录使用的提示词/生成来源，不手绘或程序生成视觉主体。
- [x] 1.2 检查 `sunny-orchard-painted` 的现有 action surface、图标线重和目标 52×52 可读性，通过 Codex 内置 imagegen 接口独立生成该 ArtSet 的 `action.compact-control` 与 `action.compact-control-active` 原始候选；记录使用的提示词/生成来源，不复制另一 ArtSet 或旧底图充当资源。
- [x] 1.3 在目标 52×52 尺寸审阅四个 imagegen 结果，只做确定性的裁切、缩放、透明边缘清理和既有导出规范化，并把源/运行时 PNG 与 Unity importer/meta 纳入各自 ArtSet 的标准目录。
- [x] 1.4 扩展有限 `RuntimeUiArtSlot`、语义 ID 与 geometry 合同，为两个生产 ArtSet 同步新增必需绑定及 manifest/hash/来源记录，确保未激活与激活 compact surface 使用相同、可验证的 nine-slice 合同。
- [x] 1.5 扩展运行时 UI 视觉验证，令新增槽位的缺失、重复、跨 ArtSet 所有权、非 imagegen 来源记录、alpha/import、manifest/hash 或 geometry 错误成为失败，并确认不存在到 `action.quiet`、`marker.selected` 或其它资源的 fallback/兼容路径。

## 2. 紧凑控制生命周期实现

- [x] 2.1 在 UI 表现层实现 `Inactive / Activating / Active / Deactivating` 生命周期状态与纯求值逻辑，使用权威 active 值和 unscaled timestamp，覆盖过渡中反转以及会话重置初始化，不向模拟或持久化写入状态。
- [x] 2.2 在现有 feedback token/motion 体系中加入紧凑控制的启动、关闭和低幅持续采样，默认实现 0.16 秒启动、0.12 秒关闭和约 1.2 秒低幅 active 循环，并让 reduced-motion 直接返回静态 inactive/active 结果。
- [x] 2.3 在共享运行时 GUI 中实现专用 compact-control 绘制入口，正交接收交互状态与模式生命周期，按未激活面、完整激活面、图标/居中倍率和按压/禁用反馈合成，并保证所有派生视觉矩形 containment 于传入的权威 52×52 `Rect`。
- [x] 2.4 将 Battle 暂停/继续控件从 `action.quiet + Selected` 迁移到专用入口，以 authoritative paused 值驱动生命周期和继续图标，并保留原 `TrackBattleAction`、键盘结果、布局和命中矩形。
- [x] 2.5 将 Battle 1×/2×控件迁移到专用入口，以 authoritative speed != 1 驱动生命周期并把当前倍率作为中央唯一内容，同时保持既有速度切换规则、布局和命中矩形。
- [x] 2.6 让采用方形紧凑底座的关闭/其它瞬时命令显式固定为 `Inactive`，只显示 hover/press/disabled 交互反馈，并在 restart、离开 Battle 或会话重绑时清除任何持续控件的旧表现状态。

## 3. 自动化验证

- [x] 3.1 增加生命周期 Editor tests，覆盖 false→activating→active、true→deactivating→inactive、暂停时 unscaled 推进、快速反转、reduced-motion 静态化、交互状态正交组合和会话重置。
- [x] 3.2 扩展 Battle UI 布局/组件 smoke，证明暂停、继续、倍速及瞬时关闭的绘制层全部 containment 于原 52×52 矩形，draw/hit rect 未改变，active/pressed 可共存且相邻控件、资源指标和 safe area 不重叠。
- [x] 3.3 扩展资源/状态验收目录，覆盖两个生产 ArtSet 的新槽位、imagegen 来源、无 fallback、1× inactive、2× activating/active/deactivating、暂停/继续和瞬时关闭无持续态，并加入不依赖纯颜色的结构断言或灰度审阅输出。

## 4. Unity 与 WebGL 验收

- [x] 4.1 运行新增测试及聚合 `FruitDefense.Editor.ProjectSetup.SmokeValidate`，修复所有资源、manifest、import、状态、几何或现有 smoke 回归，并保存通过结果。
- [x] 4.2 使用 `FruitDefense.Editor.WebBuild.Build` 生成新的普通 WebGL payload，确认构建身份、release theme 和活动 ArtSet 记录与测试使用的资产一致。
- [x] 4.3 从真实 WebGL canvas 捕获 402×874 full 和代表性 inset 的 1×、2×启动/持续/关闭、暂停/继续、hover/press 及瞬时关闭状态；人工确认目标尺寸下非纯颜色区分、active-surface containment、无相邻遮挡/混合 ArtSet/fallback/safe-area 逃逸/输入漂移后，才将变更标记完成。

## 5. 简约剪影美术返修

- [x] 5.1 将用户确认的“整体剪影简约高可读、无细小花纹、装饰不影响缩略图主体”写入资源和 WebGL 验收合同，并为 52×52 显著 alpha 连通性、微小孤立组件、中心静区及主体优先级增加可重复检查。
- [x] 5.2 仅通过内置 imagegen，为 `sunny-orchard` 独立重做连续单剪影的未激活与激活 compact surface，拒绝碎叶、花结、密集双线、小高光节点和中心纹理。
- [x] 5.3 仅通过内置 imagegen，为 `sunny-orchard-painted` 独立重做同语义资源；在 52×52 合成缩略图中确认暂停、继续、倍速、关闭图标和倍率信息先于装饰被识别，再替换源图/runtime、manifest、hash 与 prompt provenance。
- [x] 5.4 运行新增资源复杂度检查、相关组件测试及聚合 `FruitDefense.Editor.ProjectSetup.SmokeValidate`，并重建普通 WebGL。
- [x] 5.5 重抓 402×874 full/inset 的 inactive、activating、active、deactivating、hover/press 和瞬时关闭证据；人工确认单一剪影、中心安静、装饰不抢主体且无 containment/输入回归后完成返修。

## 6. 最终合成单轮廓返工

- [x] 6.1 将第二轮真实画面中“底座描边 + 状态层内外沿 + 高光”造成按钮套按钮的问题记录为拒绝结论，并把最终合成只能出现一道可见外围轮廓写入规格、设计和证据说明。
- [x] 6.2 仅通过内置 imagegen 为两个生产 ArtSet 重新生成共享几何的平面未激活/激活完整按钮面，以及无叶片/徽章壳/双描边的暂停、继续、倍速、关闭基础几何图标；激活面只把唯一棕色外轮廓替换为琥珀色，并让倍速中央只显示 `1×/2×`，禁止通过代码修画视觉主体。
- [x] 6.3 增加按真实 nine-slice 与完整 active surface 交叉淡入规则采样的固定门禁，使第二轮透明环资源因多道同心轮廓失败，并让新资源只有在中心水平/垂直轴每侧均为一个连续外围带时通过。
- [x] 6.4 运行相关资源/生命周期测试、聚合 smoke 与普通 WebGL 构建，重抓新的 full/inset 生命周期和瞬时关闭证据。
- [x] 6.5 由主 agent 与独立只读 agent 直接审阅最终 WebGL 像素；只有确认无按钮套按钮、无内沿高光、图标/倍率先读且 full/inset 均无回归后，才恢复完成状态。

## 7. 语义 container/content 返工

- [x] 7.1 扩展 `RuntimeUiTheme` 的有限 action style/color contract，为 Primary、Secondary、Quiet、Danger、ModeActive 和 Disabled 提供完整 container/content pairing、统一 resolver 与对比度验证，删除只给 label 解析 tone、让 icon 使用烘焙颜色的旧路径。
- [x] 7.2 将两个生产 ArtSet 的操作型图标源图/runtime PNG 规范化为可着色的单色中性 master/alpha mask，并将 Primary/Danger surface 内容区确定性规范化为与暖白 content 至少 `4.5:1` 的深叶绿/深陶红；保留 canvas、alpha silhouette、optical bounds、nine-slice、GUID 与 import geometry，并更新 exporter、manifest、hash 和来源说明。
- [x] 7.3 迁移共享 action renderer，使 label、action glyph 和倍率使用同一 resolved content color；将开始波次保持 Primary、苗圃刷新改为 Secondary，并覆盖 normal/hover/pressed/disabled pairing。
- [x] 7.4 改造 compact renderer，使每帧只绘制一个 resolved inactive/active surface，删除 inactive + active surface overlay 合成；保留 unscaled lifecycle、reduced-motion、52×52 containment 与瞬时 close 排除规则。
- [x] 7.5 扩展 Editor 验收，覆盖 action 三轴映射、两个 ArtSet mask 单色性、所有 role/state pairing、最终像素对比度、single-surface compact、灰度结构以及 draw/hit rect 不变，并运行聚合 `FruitDefense.Editor.ProjectSetup.SmokeValidate`。
- [x] 7.6 重建普通 WebGL，在 402×874 full/inset 捕获 Primary 开始波次、Secondary 刷新、Quiet compact inactive/active、press/focus 与 close；Disabled 由完整 pairing/final-pixel Editor 门禁覆盖，不增加伪造状态的 production acceptance 分支。人工确认播放图标不再融入按钮、层级清楚且旧 overlay 证据已被替换。
