## Context

当前 `RuntimeUiGui.DrawActionVisual` 会为 label 解析 Primary/Secondary 等 text tone，却按图标 PNG 的烘焙颜色绘制 icon；同一播放三角形同时用于继续、开始波次和开始操作，因此在奶油 compact surface 上可读、在绿色 Primary surface 上失效。compact renderer 还先画 inactive surface，再把 active surface 作为第二层覆盖，视觉状态与 surface/content 配色没有统一解析。

## Goals / Non-Goals

**Goals:**

- 每个 action role/state 都拥有完整、显式、受对比度验证的 container/content 配对。
- 同一操作图标轮廓可安全复用到不同 action surface，并由运行时 content token 着色。
- compact 持续模式保留生命周期语义，但最终只出现一个 resolved container、一个 content 层和一个独立 focus/state cue。
- 在真实普通 WebGL 画面中修复开始波次播放图标与按钮融合，并恢复 Primary/Secondary 层级。

**Non-Goals:**

- 不重画操作图标的基本几何，不改变点击、键盘、暂停、继续、倍速或刷新结果。
- 不让资源/水果图标进入单色 action glyph 规则。
- 不新增旧图标 fallback、旧/新 renderer 并行路径或迁移兼容层。

## Decisions

### 1. 三轴解析 action，而不是按控件名字分家族

共享 resolver 接收 action role、content form、behavior/mode state 与 interaction state，返回 `container slot/tint + content color + outline/focus cue`。角色决定视觉优先级，内容形态只决定排版，行为类型只决定 lifecycle。开始波次、刷新、暂停、倍率和关闭使用 proposal 中的显式映射，页面不得按 icon slot 猜测角色。

### 2. Theme 持有 surface/content 语义对

`RuntimeUiSemanticColors` 增加 Primary、Secondary、Quiet、Danger、ModeActive 和 Disabled 的 content/container 配对，或提供等价的有限强类型结构。action label、glyph 和倍率必须调用同一个 content resolver；禁止继续存在只服务文字的 `ResolveActionTextTone` 与图标烘焙色两套逻辑。精确色值属于 release theme，验证器对实际 pairing 计算 WCAG 相对亮度。

### 3. 操作图标是可着色 mask

暂停、继续/开始、速度、重试、返回、关闭和刷新等 action glyph 保留现有 alpha 轮廓与 optical bounds，导出为单色中性母版。运行时用 resolved content color 着色。资源图标保持彩色并使用不同绘制入口，避免把固有色内容误当作 action glyph。

两个生产 ArtSet 的 Primary 与 Danger surface 内容区同时确定性规范化到与暖白 content 至少 `4.5:1` 的深叶绿和深陶红。保留 alpha、nine-slice、纹理方向、optical envelope、GUID 和 import geometry；主题的 container token 必须匹配实际输出。renderer 不把 container token 再乘到已着色 surface 上，避免二次变暗。

### 4. Compact control 只绘制一个 resolved surface

保留 `Inactive / Activating / Active / Deactivating` 表现状态和 unscaled-time 求值，但 renderer 不再先画 inactive 再叠 active。每一帧只在同一几何的两个完整端点中解析一个 surface，过渡按语义阈值切换，不产生半透明第二按钮面。`action.compact-control-active` 是 mutually-exclusive active variant，不是 overlay。旧 `ActiveSurfaceOpacity`、`ShowsActiveSurface` 和无实际消费者的 active-cycle token 全部删除。Pressed/focus/disabled 作为正交 cue 合成，且不改变 52×52 draw/hit rect。

Hover/focus 使用 resolved outline color 绘制 contained 的四段内描边；它是当前唯一 surface 上的结构提示，不是新的 surface 或 ArtSet fallback。Disabled 继续由完整 pairing 和 Editor 最终像素门禁覆盖；当前 release 没有稳定、自然可达的 disabled action，因此普通 WebGL 不注入 acceptance-only 业务分支伪造该状态。

### 5. 状态必须重新解析完整 pairing

Normal、hover/focus、pressed、mode-active 和 disabled 都返回成对 container/content。Hover/pressed 默认保持 content 色稳定，只改变 surface state、阴影或 offset；disabled 不能仅降低全局 alpha，也不能把深色 content 放回深色 Primary surface。Mode-active 同时使用不同 container pairing 与继续图标或 `2×` 结构，灰度下仍可识别。

### 6. 最终像素而不是 token 名称决定验收

Editor 门禁覆盖所有 production ArtSet 的 action role × state pairing（含 disabled）、mask 单色性、无 baked hue、单 surface 绘制和对比度。普通 WebGL 在 402×874 full/inset 捕获自然可达的 Primary 开始波次、Secondary 刷新、Quiet inactive/active compact、pressed/focus 与 close，并检查实际合成像素、灰度结构、邻接和命中；不通过 production acceptance-only 分支伪造 disabled。

## Risks / Trade-offs

- 中性 mask 乘色可能暴露 PNG 边缘脏色；导出器必须保留 alpha 与 optical bounds，同时把可见 RGB 归一为中性值。
- 现有 surface PNG 含纹理，token 级对比通过不保证最终像素通过；门禁需采样最不利的实际 surface 区域并留抗锯齿余量。
- Active 改为唯一 resolved surface 后，旧 overlay 动画证据失效；这是刻意删除过时实现，不保留旧验收。

## Migration Plan

1. 扩展 theme/action style contract 与统一 resolver。
2. 将操作图标导出为中性 mask，并把 Primary/Danger surface 规范化为受测深叶绿/深陶红；更新两个生产 ArtSet manifest/hash，保留 GUID、nine-slice 和 optical geometry。
3. 迁移 action 与 compact renderer，删除 overlay 逻辑，并把刷新改为 Secondary。
4. 更新验证器与 tests，运行聚合 Editor smoke。
5. 重建普通 WebGL，重新捕获 full/inset 并人工验收最终像素。
