# UI 风格板候选与互换说明

## 状态与用途

本文件记录任务 1.2 的两套候选视觉 treatment。两张板使用相同的信息层级、组件几何和语义槽，仅改变可替换的 ArtSet 表现，用来比较统一性、可读性与美术方向。它们不是最终逐像素界面、不是生产切图；运行时主题值、有限槽位契约和生产资源仍分别由后续任务 2.2、2.3、2.5 决定。

## 审批结果

- 2026-08-18：用户明确批准 A「阳光果园」作为发布视觉方向。
- A 是后续稳定规范、生产 UI 资产和 release theme 的视觉依据。
- 2026-08-19：用户在完成技术互换验证后明确拒绝 B「果园木作」。B 的 production source、runtime export 与 ArtSet 定义已由 7.3 清理；本文件和两张原始风格板仅保留任务 1.2/1.3 的历史审查事实，不能作为可激活资源。
- 生产化仍需按本文件的组件/状态规则重制独立九宫格与图标资源，候选板本身不得直接切图发布。

| 候选 | 预览 | 画布 | SHA-256 |
| --- | --- | --- | --- |
| A「阳光果园」 | [`artset-a-sunny-orchard-style-board.png`](artset-a-sunny-orchard-style-board.png) | 1024 × 1536 PNG | `B9F976C8DC36761B62086A8BE63C2CF4A2FCBED683E3B62CC78526FCB14D87E1` |
| B「果园木作」 | [`artset-b-orchard-woodcraft-style-board.png`](artset-b-orchard-woodcraft-style-board.png) | 1024 × 1536 PNG | `1C96CA79DD89EBBE50BC6E61F12E2237D0B38150912BB35FC645735EA65E07EB` |

## 固定构图与同槽位原则

两套板逐项保持以下不变量，未来切换 ArtSet 时不允许改 Presenter、页面布局或点击区域：

1. 顶部均为六个主题色样和同一组中文字体层级。
2. 左侧 Lobby 均为三张关卡卡片、第二张选中、一个主操作按钮。
3. 中央 Battle 均为三项顶栏指标、暂停/倍速、战场、四格操作托盘、选中格、详情卡和暂停模态。
4. 右侧 Settlement 均为结果卡、三项结果指标、重试主操作和返回安静操作。
5. 底部均按 `normal → hover/focused → pressed → disabled → selected → loading → success → warning → error` 排列状态，并使用相同的通用图标顺序。

板中植物、武器与花盆缩略图只演示内容图放入 UI 槽后的安全边距；植物和武器仍属于 gameplay content art，不归 UI ArtSet 所有。

## 候选 A：阳光果园

### 调色板与材质

| 角色 | 候选值 | 用途 |
| --- | --- | --- |
| 温暖奶油 | `#FFF6E0` | 页面、卡片与大面积可读表面 |
| 浅阳光 | `#FFE7A3` | 浅强调、悬停/聚焦辅助 |
| 阳光琥珀 | `#FFD24D` | 选中、星级、关键强调 |
| 叶绿 | `#6DBE4B` | 主操作与成功 |
| 柔和鼠尾草 | `#8FBF74` | 次级层、禁用与低优先级状态 |
| 泥土棕 | `#8B5E3C` | 正文、图标和受控描边 |

- 形状：柔和圆角；卡片、按钮和模态共享约 10–14 视觉像素的圆角家族，内部紧凑控件按比例减小。
- 描边：泥土棕 2–3 视觉像素，重要选中态叠加琥珀外圈与勾选标记。
- 阴影：仅使用浅、短距离偏移阴影建立 `background → panel → card/action → modal` 的深度关系，不使用厚重倒角。
- 纹理：奶油表面只保留很轻的纸张颗粒；内容插画与应用 chrome 的边界清楚。
- 图标：圆润果园插画线条，主轮廓与文字同色系，彩色资源图标不依赖文字才能辨认。

### 字体层级

风格板展示的是比例候选，不提前锁死任务 2.2 的运行时精确值：

| 层级 | 板上规格 | 典型用途 |
| --- | --- | --- |
| 大标题 | 40/46，加粗 | 页面结果、路线主标题 |
| 标题 | 28/34，加粗 | 卡片组、模态和详情标题 |
| 正文 | 20/28，常规 | 说明与关卡信息 |
| 数值 | 24/28，加粗 | 资源和结果指标 |
| 辅助 | 16/22，常规 | 状态、单位与补充信息 |

中文采用清楚、不过度装饰的圆润无衬线方向；品牌字样只是情绪参考，不能代替运行时中文字体资产与字形覆盖验证。

## 候选 B：果园木作

### 调色板与材质

B 保留与 A 相同的六个语义主题 token，便于只换 ArtSet 时继续满足对比度和状态语义；材质层用更偏燕麦纸、鼠尾草绿、陶土橙和胡桃棕的局部处理建立木作气质。

- 形状：严格复用 A 的组件外接框、圆角家族与内边距，不通过改变几何表达“木作”。
- 描边：2–3 视觉像素胡桃棕轮廓；选中仍由琥珀/陶土强调加勾选标记表达。
- 阴影：与 A 相同的浅层级，木边、木钉和缝叶只做边缘点缀，禁止深倒角或厚重梁柱。
- 纹理：宽表面为浅燕麦纸；页签和主按钮可用克制缝线；外框允许轻木纹，但文字承载面不得出现高对比木结。
- 点缀：鼠尾草绿承担主操作/稳定状态；陶土橙用于选中、warning 和 danger 的局部识别，错误仍需图标或文字等第二线索。
- 图标：沿用同一画布、安全边距和识别轮廓，只允许局部木作/缝叶质感，不新增或替换语义。

本候选的辨识度更强，但板上外围木框、叶片和木钉比 A 更重。若任务 1.3 选择 B，生产化时应减少装饰覆盖面积，优先保留战场面积、中文留白和小屏信息清晰度。

## 语义槽一一对应

下表是风格板与审计之间的候选映射，不替代任务 2.3 的最终有限槽位定义。

| 语义槽 | 板上位置/用途 | A treatment | B treatment |
| --- | --- | --- | --- |
| `surface.screen-background` | 整板与三个页面底层 | 温暖奶油、轻纸纹 | 燕麦纸、克制木边 |
| `surface.safe-area` | 三个竖屏切片的内容框 | 无装饰安全留白 | 同几何留白，装饰仅在边缘 |
| `surface.panel-standard` | 托盘与普通内容区 | 奶油面、泥棕细线 | 浅纸面、胡桃细线 |
| `surface.panel-raised` | 顶栏和高一级容器 | 浅偏移阴影 | 同深度，轻木边/木钉 |
| `surface.card-selectable` / `card.selectable` | Lobby 关卡卡 | 琥珀外圈 + 勾选 | 同位置同线索，缝边/木边材质 |
| `surface.metric` | Battle 顶栏、Settlement 指标行 | 奶油分隔行 | 燕麦纸分隔行 |
| `surface.status` | 状态带/短反馈承载面 | 浅色状态底 | 同几何，克制材质化边缘 |
| `surface.detail` | Battle 植物详情 | 奶油圆角详情卡 | 燕麦纸详情卡、胡桃轮廓 |
| `surface.modal` | 暂停卡 | 奶油浮层 | 燕麦纸浮层、轻木/叶页签 |
| `surface.result` | Settlement 结果卡 | 奶油指标卡 | 燕麦纸指标卡、胡桃细边 |
| `surface.scrim` | 暂停模态下方 | 暖黑棕透明遮罩 | 同不透明度层级的胡桃棕遮罩 |
| `action.primary` | 开始、继续、重试 | 叶绿实底 | 鼠尾草绿、克制缝边 |
| `action.secondary` | 次一级实心操作 | 浅奶油/浅绿 | 燕麦纸/浅鼠尾草 |
| `action.quiet` | 返回、关闭、暂停/倍速 | 奶油低层级 | 纸面低层级、胡桃图标 |
| `action.danger` | 重新开始/错误操作 | 果红/陶土实底 | 陶土红实底、胡桃轮廓 |
| `slot.tool` | Battle 四格托盘 | 奶油方槽、琥珀选中 | 纸面方槽、陶土/琥珀选中 |
| `slot.nursery` | 植物/花盆承载与安全边距 | 同 tool 的圆角家族 | 同几何，材质替换 |
| `marker.selected` | 卡片、托盘与组件状态带 | 琥珀边 + 勾选 | 陶土/琥珀边 + 勾选 |
| `indicator.disabled` | 底部状态带 | 降饱和 + 低对比度标签 | 灰鼠尾草 + 低对比度标签 |
| `indicator.loading` | 底部状态带 | 旋转进度符号 + 文案 | 同符号与位置，材质替换 |
| `indicator.success` | 底部状态带 | 叶绿 + 勾选 | 鼠尾草绿 + 勾选 |
| `indicator.warning` | 底部状态带 | 琥珀 + 感叹号 | 陶土橙 + 感叹号 |
| `indicator.error` | 底部状态带 | 果红 + 感叹号 | 陶土红 + 感叹号 |
| `indicator.drag-legal` | 未在板中展开的交互派生 | success 颜色 + 对勾/目标框 | 同语义线索，B 材质 |
| `indicator.drag-illegal` | 未在板中展开的交互派生 | error 颜色 + 禁止符号 | 同语义线索，B 材质 |
| `indicator.merge` | 未在板中展开的交互派生 | selection 颜色 + 合并图标 | 同语义线索，B 材质 |
| `indicator.swap` | 未在板中展开的交互派生 | selection 颜色 + 双向箭头 | 同语义线索，B 材质 |

通用图标严格共用一个 48 × 48 逻辑画布候选，主体尽量落在约 36 × 36 安全区内：

- `icon.resource-sun`
- `icon.resource-core`
- `icon.resource-wave`
- `icon.control-pause`
- `icon.control-continue`
- `icon.control-speed`
- `icon.control-start-wave`（与 continue 共享三角形语法，但绑定语义不同）
- `icon.control-retry`
- `icon.control-return`
- `icon.control-close`
- `icon.tool-pot`

## 组件状态与节奏

- 基础间距采用 4 点节奏；风格板视觉示例主要落在 4、8、12、16、24、32 的倍数。
- `normal`：基础面、描边和浅阴影。
- `hover/focused`：增加外圈/聚焦描边；移动端无 hover 时作为键盘或可访问性 focus 候选。
- `pressed`：内容下移约 2 视觉像素并收短阴影，点击几何不变。
- `disabled`：降饱和、降低对比并保留禁用文字/图标线索；不能只改透明度。
- `selected`：强调色外圈和勾选标记同时存在。
- `loading`：旋转进度符号与 loading 文案同时存在，操作区域尺寸不变。
- `success`、`warning`、`error`：分别使用绿、琥珀/陶土、果红/陶土红，并配勾选或感叹号。

## 逐张质检

| 检查项 | A | B |
| --- | --- | --- |
| Lobby / Battle / Settlement 信息齐全 | 通过 | 通过 |
| 三指标、操作托盘、选择/详情/暂停模态 | 通过 | 通过 |
| 结果卡、重试/返回 | 通过 | 通过 |
| 九种组件状态 | 通过 | 通过 |
| 两套构图和槽位一一对应 | 基准 | 通过，基于 A 做 treatment 转换 |
| 中文层级与主要短标签可辨 | 通过，少量生成字形不作为最终 copy | 通过，少量生成字形不作为最终 copy |
| 默认 Unity skin / 科技感 / 玻璃拟态 | 未发现 | 未发现 |
| 生产可直接切图 | 否，需在 1.3 批准后按 2.5 重制 | 否，需在 1.3 批准后按 2.5 重制 |

生成图中的短文案只用于层级和密度判断；生成文字不是文案源真相，任何轻微字形、标点或小尺寸图标语义误差都不应被复制进生产资源。最终可访问名称、准确中文 copy、像素尺寸、九宫切片和导入规则以批准后的规范与运行时主题为准。

## 生成方式与源输出

- 工具：OpenAI 内置 `image_gen`。
- 模型：工具结果没有公开具体模型标识，因此不臆测型号。
- A 模式：新图生成，prompt use case 为 `ui-mockup`；以现有 Lobby、Battle ready/detail/paused、Settlement 截图作为信息架构参考。
- B 模式：编辑/风格转换，prompt use case 为 `style-transfer`；使用 A 作为唯一结构锚点，以保证位置和组件数量不变。
- A 源输出：`C:\Users\18163\.codex\generated_images\01a00ede-d028-7590-b24c-399145d8b82f\exec-ae2b6e13-559e-41b0-8dfb-40ba527101f6.png`
- B 源输出：`C:\Users\18163\.codex\generated_images\01a00ede-d028-7590-b24c-399145d8b82f\exec-b17aef69-11af-426a-9c88-9937ecfb9c17.png`
- 项目证据目录保存独立副本；源输出保留不动。

## 最终生成 Prompt

### A「阳光果园」

```text
Use case: ui-mockup
Asset type: high-resolution portrait game UI style board, candidate ArtSet A
Primary request: Create a polished production-oriented style board for a portrait orchard tower-defense game named treatment A “阳光果园 / SUNNY ORCHARD”. The five input screenshots are information-architecture and gameplay-content references only; do not reproduce their dark default Unity skin or rough flat styling.

Composition/framing: one tall portrait presentation board on a warm neutral backdrop, crisp orthographic front view, strict modular grid. Use the exact reusable layout described here so a second art treatment can match it one-for-one:
1) top band: six palette swatches and Chinese typography ladder;
2) middle: three aligned portrait UI slices—Lobby on the left, Battle largest in the center, Settlement on the right;
3) bottom band: one consistent component-state strip plus common icon samples.
Lobby slice: three rounded level cards, one clearly selected using amber outline plus check marker, and a prominent leaf-green primary button.
Battle slice: header with exactly three compact resource metrics represented by sun, core-heart, and wave icons; compact pause and speed controls; green battlefield placeholder; operation/tool tray with fruit, weapon and flower-pot icons; one amber selected slot; contextual plant detail card with quiet close control; a clear pause modal inset with scrim, continue primary action and restrained red restart action.
Settlement slice: success/result card with three metrics, green retry primary action, quiet return action.
Bottom state strip: show the same base component in normal, hover/focused, pressed, disabled, selected, loading, success, warning, and error states. Every critical state must combine color with a second cue such as outline, icon, check, spinner, or label. Add icon samples for sun, core, wave, pause, speed, retry, return, close, and pot.

Style/medium: shippable mobile game UI mockup and asset-direction board, clean vector-like painted raster, warm orchard cartoon, practical layout rather than concept art.
Color palette: warm cream surfaces; leaf-green primary action; sunlight amber selection/emphasis; deep soil-brown text and controlled outlines; muted sage disabled; fruit red only for danger/error.
Materials/textures: very subtle paper-grain only, soft rounded corners, controlled 2–3 px soil-brown outlines, restrained shallow offset shadows, no heavy bevel.
Typography: demonstrate a clear Chinese sans-serif hierarchy using only short specimen labels where possible: “大标题”, “标题”, “正文”, “数值”, “辅助”. Other UI copy should be short; emphasize hierarchy and space rather than lots of generated text.
Constraints: keep gameplay content art visually separate from application chrome; consistent 4-point spacing rhythm; touch-friendly controls; equal semantic slots across all three screens; compare-ready; no watermark; no logos; no extra screens; no photographic elements.
Avoid: dark default Unity skin, glossy generic mobile chrome, glassmorphism, neon technology aesthetic, pure black framing, overbright highlights, deep 3D perspective, clutter, long paragraphs, illegible tiny text.
```

### B「果园木作」

```text
Use case: style-transfer
Asset type: high-resolution portrait game UI style board, candidate ArtSet B
Input image: Image 1 is the approved layout/content anchor from candidate A. Treat it as the edit target for structure.
Primary request: Create treatment B “果园木作 / ORCHARD WOODCRAFT” by changing only the visual ArtSet treatment of Image 1. Preserve the exact same portrait board composition, section order, three screen slices, component geometry, semantic slots, information hierarchy, component count, state-strip order, and icon list so A and B can be compared one-for-one.

Invariants—must remain present in the same locations:
1) top band with six palette swatches and Chinese typography ladder;
2) Lobby left with three level cards, one selected card plus check marker, primary start action;
3) Battle center with exactly three resource metrics (sun, core-heart, wave), pause/speed controls, battlefield placeholder, operation/tool tray, selected slot, contextual detail card, and pause modal inset with continue/restart actions;
4) Settlement right with result card, three metrics, primary retry and quiet return;
5) bottom strip with normal, hover/focused, pressed, disabled, selected, loading, success, warning, error, plus icon samples for sun, core, wave, pause, speed, retry, return, close, pot.
Keep critical states using color plus a second cue.

Change only the visual treatment:
Style/medium: warm orchard workshop cartoon UI; shippable mobile game mockup; clean vector-like painted raster, not concept art.
Color palette: parchment oat and pale linen surfaces, sage-green primary actions, terracotta-orange emphasis and warning, dark walnut-brown text and outlines, dusty olive disabled, restrained berry-red danger.
Materials/textures: subtle hand-planed wood edge bands, restrained stitched-leaf tabs and tiny peg details, very light paper/wood grain inside broad surfaces; soft 10–14 px rounded corners; controlled 2–3 px walnut outlines; restrained shallow offset shadows. Keep textures quiet enough for Chinese text and gameplay icons.
Typography: preserve the same clear Chinese sans-serif hierarchy and short labels; do not turn type into carved decorative lettering.
Constraints: practical touch-friendly UI; same 4-point spacing rhythm; gameplay content art remains distinct from application chrome; no new elements, no removed components, no rearranged panels; no watermark; no logos.
Avoid: generic technology UI, glassmorphism, neon, metallic sci-fi, glossy highlights, heavy skeuomorphic timber beams, excessive knots, deep bevels, pure black framing, deep 3D perspective, clutter, long paragraphs, illegible text.
```
