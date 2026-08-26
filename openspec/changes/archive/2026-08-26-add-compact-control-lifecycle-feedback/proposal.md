## Why

前一轮 compact-control 返工只解决了方形底座与状态轮廓，却继续让操作图标携带固定泥土棕色。真实 402×874 Battle 画面中，播放三角形与 Primary 绿色按钮的代表性对比度只有约 `1.17:1`，图标与按钮融为一体。现有规则还把动作角色、内容形态和持续模式混成同一组件家族，并用 inactive surface 上叠 active surface 表达状态；这不是可扩展的按钮视觉系统。

## What Changes

- 将 action 视觉合同改为受测的 `container/content` 语义配对；按钮文字、操作图标和紧凑倍率统一从所在 action role 解析 content 色。
- 将动作角色、内容形态和行为类型拆为三个正交输入：`Primary / Secondary / Quiet / Danger`，文字/图标文字/纯图标/紧凑数值，以及瞬时命令/持续模式。
- 操作型图标改为可着色的单色中性母版或 alpha mask；ArtSet 只拥有轮廓、线重和光学尺寸，固定泥土棕、渐变、高光和状态色不再烘焙进图标。
- 将两个生产 ArtSet 的 Primary 与 Danger surface 本体分别规范化为与暖白 content 达到至少 `4.5:1` 的深叶绿/深陶红内容区；container token 必须描述实际 runtime PNG，禁止依靠代码二次乘色掩盖资源与 token 不一致。
- compact control 每帧只绘制一套完整 resolved surface/content；active surface 若作为 ArtSet 状态资源保留，只能替换 inactive surface，禁止两张按钮面叠加或交叉暴露多层轮廓。
- 明确当前 Battle 映射：开始波次为 Primary，苗圃刷新降为 Secondary，暂停/继续与 `1×/2×` 为 Quiet 持续模式，关闭为 Quiet 瞬时命令。
- 增加最终像素对比门禁：文字至少 `4.5:1`；操作图标发布目标 `4.5:1` 且不得低于 `3:1`；必要边界、焦点和状态线索至少 `3:1`。
- 撤销前一轮 compact-control WebGL 人工通过结论，重新生成 full/inset 状态矩阵并以最终合成像素验收。

## Capabilities

### Modified Capabilities

- `compact-control-lifecycle-feedback`: 从专用底图叠加模型修订为统一 action role/content pairing、单 resolved surface、可着色操作图标和最终像素对比合同。

## Impact

- 影响 `RuntimeUiTheme` 语义颜色、`RuntimeUiGui` action/compact 绘制、操作图标源图与导出管线、Battle 的刷新角色映射以及 Editor/WebGL 验收。
- 保留暂停、继续、倍速生命周期的权威状态和 unscaled-time 行为，不改变模拟、输入、命中矩形、关卡或存档规则。
- 删除固定图标颜色和 active overlay 旧路径，不提供兼容层或 fallback。
