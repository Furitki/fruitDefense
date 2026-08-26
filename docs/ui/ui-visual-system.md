---
id: ui-visual-system
parent: design-kb-home
order: 45
status: active
---

# Fruit Defense 运行时 UI 视觉系统

## 1. 文档目的与批准方向

本文是 Bootstrap、Lobby、Battle 应用层界面、阻塞浮层和 Settlement 的稳定视觉规范。它定义跨页面应长期保持的语义、组件 anatomy、状态语言、美术生产规则和审查标准，不记录某次构建、截图、平台状态或验收结论。

发布视觉方向为已批准的 A「阳光果园」：温暖浅色表面、泥土棕文字与线条、叶绿主操作、阳光色强调与选中、克制果红危险提示、圆润可读轮廓和浅层次。批准风格板只用于方向与密度参考，不能直接裁切为生产资源；准确中文文案也不能从生成图中转录。参考说明见 `openspec/changes/unify-runtime-ui-visual-system/evidence/style-board/style-board-notes.md`。

当前 production registry 只保留已批准的 A。第二套 treatment 已在审查后被用户拒绝，其 production source、runtime export 与 ArtSet 定义均已删除；OpenSpec 中保留的历史审查证据不是可激活资源。

## 2. 所有权与边界

| 内容 | 唯一所有者 | 本文的约束 |
| --- | --- | --- |
| 稳定视觉语义、组件 anatomy、状态矩阵、美术生产与审查规则 | 本文 | 设计、美术和工程共同遵守 |
| 精确颜色、字体引用、字号、行高、间距数值、圆角、描边、透明度、动效时长、缩放值和 active ArtSet 引用 | 单一 [release RuntimeUiTheme asset](../../Assets/UI/Theme/ReleaseRuntimeUiTheme.asset) | 本文只定义角色和关系，不复制序列化值 |
| 纹理、Sprite、切片数据、稳定 set ID、revision 和完整语义槽映射 | `RuntimeUiArtSet` asset | 每个槽恰好一个资源；禁止继承、缺省和混用 |
| 页面 copy、资源数值、命令和显隐条件 | 各 route presenter | 只能把内容与显式状态交给共享组件，不能定义局部视觉主题 |
| draw/hit 几何、safe-area 投影和交互矩形 | 现有 Shell/Battle 布局 authority | 样式和换资源不能新增第二套矩形或移动点击/拖拽区域 |
| 地形、植物、武器、敌人、战斗特效和关卡级配色 | gameplay content / level presentation | 不属于 UI ArtSet，不得因替换 UI 资源而改变 |
| 变更需求、实施任务和一次性审批证据 | 对应 OpenSpec change | 不复制到本文成为长期状态记录 |

精确值和具体引用始终以上表链接的 release theme asset 为准；稳定的主题审查、候选预览与激活入口是 Unity 菜单 `Fruit Defense/UI/Visual System`。本文只保留已接受的长期规则与权威入口，不复制构建哈希、日期、截图结论或平台声明。

### 2.1 范围内

- `Bootstrap → Lobby → Battle → Settlement` 全流程的应用 chrome。
- screen/safe-area 表面、标准/抬升面板、卡片、按钮、指标、状态、工具槽、详情卡、模态和结果卡。
- 共享 UI 图标、状态标记、九宫格表面及其生产导入规则。
- 全屏与带顶部/底部 inset 的竖屏安全区表现。

### 2.2 范围外

- 导航合法性、命令、战斗规则、平衡、存档、快照、内容身份和平台适配。
- 关卡地形、植物/武器/敌人主图与战斗特效的重绘。
- uGUI/UI Toolkit 迁移、运行时皮肤选择器、远程资源或兼容主题。
- 为适配美术而改变现有布局、点击区域或拖拽几何。

### 2.3 后续需求入口

任何新的玩家可见 UI 需求在提案或实施前，必须先读取本文和 `openspec/specs/` 下受影响的当前能力规格。默认是在既有语义、组件和验收合同上增量演进；如果新需求要改变这些标准，OpenSpec 必须明确列出保留、修改和新增的规则，并在验收后同步回本文及对应主规格。`openspec/changes/archive/` 只保存决策过程和证据，不替代当前规范。

## 3. 视觉基础

### 3.1 语义颜色

代码和 Presenter 只使用 release theme 中的语义角色，不创建 `LobbyGreen`、`BattleBrown` 之类页面色：

- `edge-background`：安全区外或设计画布边缘的稳定背景。
- `surface-base`：温暖浅色基础表面。
- `surface-raised`：比基础面更高一层的卡片、顶栏和浮层。
- `action-primary`：叶绿主操作。
- `action-secondary`：次一级实心操作。
- `selection-accent`：阳光色选中、聚焦和关键强调。
- `success`、`warning`、`danger`：成功、警告和危险/错误。
- `disabled`：柔和鼠尾草方向的不可用语义。
- `scrim`：暖棕向遮罩，不能使用纯黑框住页面。
- `text-primary`、`text-secondary`、`text-inverse`：主文字、辅助文字和深色/高彩表面的反白文字。

Action 颜色必须以 `container/content` 语义对定义，而不是让文字或图标各自携带固定颜色。至少包含 `primary.container/content`、`secondary.container/content`、`quiet.container/content`、`danger.container/content`、`mode-active.container/content` 与 `disabled.container/content`。`content` 同时约束按钮上的文字、操作图标和紧凑倍率；相同图标轮廓放到不同 container 时必须解析为对应的 content 色。

颜色只是状态的一条线索。selected、mode-active、disabled、loading、success、warning、error 和拖拽反馈还必须具有轮廓、图标、文字、透明度或形状中的至少一个非颜色线索。所有精确色值和受测前景/背景组合由 `RuntimeUiTheme` 持有。按钮文字与实际渲染 container 的对比度不得低于 `4.5:1`；操作图标发布目标同样为 `4.5:1`，任何情况下不得低于 `3:1`；用于识别组件边界、焦点或状态的非文字线索不得低于 `3:1`。这些门槛按最终合成像素验证，不以源图色值或单张素材代替。

### 3.2 中文字体层级

所有角色使用 release theme 引用的已打包中文字体，Bootstrap 不得回退到默认 Unity 字体。运行时文字不得烘焙进按钮、卡片、指标或状态 Sprite。

| 角色 | 用途 | 稳定规则 |
| --- | --- | --- |
| `display` | 胜负结果、极少数路线级大标题 | 一屏只出现一个主焦点，不挤压主要操作 |
| `screen-title` | Lobby、Battle、Settlement 页面标题 | 跨页面保持相同权重和层级关系 |
| `section-title` | 卡片组、详情、模态标题 | 与 body 拉开层级，但不与 screen title 竞争 |
| `body` | 说明、关卡信息、错误详情 | 优先完整可读，不以缩小字号解决空间不足 |
| `control-label` | 按钮、标签和短操作 | 单行优先；加载时保留稳定宽度与原动作语义 |
| `metric` | 阳光、核心、波次和结果数值 | 标签弱于数值，数字基线和列宽稳定 |
| `supplemental` | 单位、冷却、状态补充 | 仍必须满足 theme 的最小可读尺寸与对比度 |

字体文件、精确字号、字重、行高和最小逻辑尺寸属于 `RuntimeUiTheme`。不得在 Presenter 中为了塞入 copy 临时缩小字号；应先缩短 copy、换行或调整既有布局 authority 内的内容排布。品牌字、生成图字形和系统字体不能替代项目字体覆盖验证。

单行文本的 owner 高度不得小于对应 typography role 的完整 line-height；共享绘制层必须以该 line-height 建立行盒，不能缩短行盒后依赖 `Clip` 看似塞入。Battle 的标题、指标、数量、成本、星级、状态、详情和提示必须声明有限行策略与 owner；运行时 formatter 和内容目录还必须登记发布边界样本，并用真实字体、真实 action 语义和实际 viewport 投影验证。内容域扩大时，同一变更必须更新 owner、批准 copy 或受控多行 anatomy，不允许自动缩字、截断或把 clamp 视为通过。

### 3.3 四点间距节奏

- 所有外边距、内边距、组件间隙和内容间隙以 4 logical point 为基础节奏，并使用 release theme 的命名 spacing token。
- 常用密度通过 spacing token 组合表达，不在页面中散落无名数值。
- 描边宽度、像素对齐补偿和九宫格 border 不强行按 4 的倍数处理，但必须来自 theme/ArtSet 元数据，不能成为页面例外。
- Lobby 的舒展密度与 Battle 的工具密度可以不同；二者必须从同一 spacing token 阶梯选择，而不是形成两套尺度。
- safe-area inset 先由布局 authority 解析，组件再在所得内容区内应用 spacing token；不能用视觉透明边距冒充 safe area。

### 3.4 圆角、描边与浅阴影

- 形状只使用小、中、大三类语义圆角；紧凑图标控制使用小，按钮/槽位使用中，主卡片/模态使用大。
- 泥土棕系描边保持清楚但克制；同一层级的等价组件必须使用同一 outline role。
- 深度关系固定为 `screen background → standard panel → gameplay stage / raised / card / action → modal/result`；gameplay stage 只作为页面内唯一高密度玩法锚点，不作为包裹下方控制区的外层容器。
- 阴影只表达相邻层级：短距离、低对比、单一光向。禁止厚重倒角、强高光、玻璃拟态、金属科技光、纯黑框和与战场内容争夺注意力的装饰。
- pressed 通过收短阴影和轻微内容位移表达，但外接框、布局矩形和点击区域不变。
- exact radius、outline、shadow offset/opacity 由 release theme 持有；九宫格边界由 ArtSet 元数据持有。

## 4. 内容层与安全区

### 4.1 UI chrome 与 gameplay content art

- UI ArtSet 拥有面板、按钮、卡片底板、槽位底板、状态标记、控制图标和资源指标图标。
- 植物、武器、敌人、地形和战斗特效由 gameplay content art 拥有。`slot.tool` / `slot.nursery` 只规定它们进入 UI 槽后的裁切、留白、状态覆盖和层级。
- 花盆若作为操作/槽位符号使用，可由 `icon.tool-pot` 表达；战场中的花盆实体仍属于 gameplay content。
- 关卡可以改变战场配色和内容插画，但不能改变应用按钮、导航、结果卡或状态语义。
- 美术换集不能改变命令、内容 identity、数值或任何战斗结果。

### 4.2 safe area 与现有几何

- `PortraitShellLayout`、`BattlefieldProjection` 及既有 Battle 交互矩形继续同时服务 draw 与 hit test。
- Battle 的 Header 与 BattleStage 使用同一全宽 owner track，最终 WebGL 左右可见边必须对齐；Header 使用 1–2px standard outline，BattleStage 使用唯一 3–5px gameplay-stage outline。框重差来自语义槽，不得通过页面私有 rect 或局部描边补丁制造。
- BattleStage 只包围同一个 `BattlefieldProjection`/Board。ContextTray、NurseryTray 与 RefreshAction 直接落在 page paper 的统一 8pt inset track 上，不得恢复包围 stage 与 controls 的下半页大框。
- ContextTray 在无选择时显示工具 anatomy、选择植物时在同一 rect 显示 detail anatomy，两者互斥；NurseryTray、RefreshAction 与棋盘内 WaveAction 始终保留。RefreshAction 下缘距 safe-content 下缘必须为 8–40 logical points。
- screen background 可以铺到可见边缘；可交互内容和必要中文必须留在 resolved safe area 内。
- UI texture 的透明 padding 只属于视觉资源，不得扩大、缩小或偏移逻辑 rect。
- 所有共享组件必须在 360×800、375×812、402×874、430×932 的支持竖屏尺寸，以及完整和代表性顶部/底部 inset safe area 中保持受保护边角、文字和操作目标。
- 缩放必须使用共享 UI scale context；不得为某个 Sprite 新建独立指针区域或第二套布局。

## 5. 组件 anatomy

所有组件接收 `theme + art set + explicit state + content`。Presenter 不得直接引用纹理路径，也不得复制共享绘制助手后再局部改色。

| 组件 | 必需 anatomy | 稳定约束 |
| --- | --- | --- |
| Screen / safe-area surface | edge background、screen background、safe content frame | 背景与安全区分层明确；不出现默认 skin 或黑色框边 |
| Standard panel | surface、outline、content inset | 承载普通内容，不用额外装饰制造假层级 |
| Gameplay stage | transparent-center heavy frame、protected rail、authoritative Board | 页面内唯一常驻重结构框；只包围 gameplay projection，不包围 Context/Nursery/Refresh |
| Raised panel | surface、outline、shallow shadow、content inset | 需要从 standard surface 抬起的卡片或浮层；与 modal 保持层级差，不用来给同级结构条制造不同框重 |
| Selectable card | surface、标题/正文区、辅助信息区、selection marker | normal 与 selected 的文本排版尺寸不变；选择使用强调边与明确标记 |
| Primary action | surface、outline、label、可选语义 icon、state cue | 每个操作组只能有清楚的主要动作；跨页面 anatomy 相同 |
| Secondary action | surface、outline、label、state cue | 可用但优先级低于 primary，不靠减小触控区域降级 |
| Quiet action | 低层级 surface 或图标承载面、label/icon、focus cue | 返回、关闭、暂停、倍速等；仍需完整状态和触控目标 |
| Danger action | danger surface、label/icon、state cue | 只用于破坏性/终止性操作；不与普通主操作争夺默认焦点 |
| Resource / metric | icon canvas、label、value、分隔/容器 | 数值为第一阅读点，标签和单位为第二层；跨 Battle/Settlement 对齐一致 |
| Status feedback | status surface、状态 icon、短标题/正文、可选恢复 action | success/warning/error/loading 位置和 anatomy 一致；错误不可只显示红字 |
| Tool / nursery slot | slot surface、content-art safe frame、数量/成本/星级区、state overlay | content art 不被 UI ArtSet 接管；状态层不改变槽位 rect |
| Contextual detail card | raised/detail surface、标题、属性/正文、quiet close | 关闭动作位置稳定；详情出现或退出不移动战场 hit rect |
| Blocking modal | scrim、modal surface、标题、正文/图标、action group | scrim 阻断背景输入；primary 与 danger 层级明确 |
| Result card | result surface、outcome 标题、metric group、action group | 胜/负共享 anatomy；结果语义由文字/图标加颜色表达 |

Bootstrap 初始化、阻塞错误和重试必须复用 screen、status、modal/action anatomy；Lobby 与 Settlement 不得保留 Shell 专属外观；Battle 高频控件不得形成独立组件家族。

### 5.1 Action 三轴合同

Action 的角色、内容形态和行为类型彼此正交，页面不得通过图标名称或控件尺寸反推动作角色：

- 角色：`Primary / Secondary / Quiet / Danger`，决定 container、content、outline 与视觉优先级。
- 内容形态：文字、图标加文字、纯图标、紧凑数值，只决定排版、安全区和光学居中。
- 行为类型：瞬时命令或持续模式，只决定是否具有 mode lifecycle，不创造新的颜色家族。

典型映射为：开始波次=`Primary + 图标文字 + 瞬时命令`；刷新苗圃=`Secondary + 图标文字 + 瞬时命令`；暂停/继续=`Quiet + 纯图标 + 持续模式`；`1×/2×`=`Quiet + 紧凑数值 + 持续模式`；关闭=`Quiet + 纯图标 + 瞬时命令`。同一操作组只能有一个 Primary；准备阶段的流程推进动作不得与辅助经济操作使用相同的 Primary 外观。

组件每一帧只解析一套完整视觉样式：`ButtonStyle(role, interactionState, modeState) -> container + outline + content + focus/state cue`。禁止把 active 当作第二张按钮覆盖在 inactive 按钮上；过渡只能在同一几何的语义 token 间插值或切换，不得同时暴露两套边框、两层按钮面或未经配对验证的前景色。

## 6. 完整状态矩阵

### 6.1 全局状态定义

| 状态 | 必需视觉线索 | 行为与组合规则 |
| --- | --- | --- |
| `normal` | 基础 surface、outline 和浅阴影 | 可交互组件的默认可用态 |
| `hovered/focused` | selection-accent focus ring 或清楚外圈 | 不改变布局；移动端无 hover 时仍保留键盘/可访问性 focus 语义 |
| `pressed` | 阴影收短 + 内容轻微位移/压下形态 | 命令触发时机不变；不得移动 hit rect |
| `selected` | selection-accent + 勾选/角标/明确标记 | 只用于可选择对象；不得通过改变字号或卡片尺寸表达 |
| `disabled` | 降饱和/降低对比 + 禁用 icon、标签或明确遮罩 | copy 仍可读；不能只降低 alpha；不接受输入 |
| `loading/transitioning` | spinner/进度符号 + 保留原动作标签或加载文案 | 尺寸稳定、阻止重复命令；不能伪装成普通 disabled |
| `success` | success color + 勾选/成功 icon 或短标签 | 用于明确完成反馈，不长期取代 normal 主操作 |
| `warning` | warning color + 感叹号/警告 icon 或短标签 | 必须与 error 在图标/标题上可区分 |
| `error` | danger color + 错误 icon/标题；可恢复时保留 recovery action | 不依赖红色；阻塞错误使用 modal/status hierarchy |
| `drag-legal` | success outline/目标框 + 对勾或吸附目标符号 | 只覆盖目标槽，不重定义拖拽几何 |
| `drag-illegal` | danger outline/目标框 + 禁止符号 | 不依赖红/绿区别；不得触发命令 |
| `merge` | selection/success cue + 合并 icon | 与普通 legal drop 可辨，目标 rect 不变 |
| `swap` | selection cue + 双向箭头 | 与 merge 使用不同形状线索 |

状态优先级为：`loading/transitioning` 或 blocking 状态优先于 `disabled`，`disabled` 优先于 `pressed`，`pressed` 优先于 `focused`，其余回到 `normal`。`selected` 是持久选择标记，可与 focus 并存；进入 disabled/loading 后仍可保留选择身份，但不可呈现为可操作。success/warning/error 属于结果或反馈语义，不能任意覆盖 action family 的优先级。

所有动效使用 theme 的 restrained、unscaled-time feedback token，并尊重降低视觉噪声；精确时长不写入 Presenter 或本文。

### 6.3 共享动效与 Press 生命周期

- 运行时只使用 `press`、`pop`、`strong-pop`、`fade-slide`、`stagger` 这组有限语义模式；精确时长、幅度、位移、错峰和降低动态效果策略由 release theme 持有。
- `press`、`pop`、`strong-pop` 只允许向内收缩并快速恢复到 `1.0`；任何样本都不得大于 `1.0` 或越出组件静态外框。pop 使用独立短时长，不能被 status/reward 的长显示 pulse 拉成长回弹。
- Motion evaluator 只消费显式传入的 unscaled time，返回 scale、alpha、offset 的纯值样本；Presenter 不创建 Coroutine、延迟命令或页面私有 Tween 类型。
- 每个反馈目标只拥有一个当前 pulse。重复触发以新 pulse 替换旧 pulse；Hide、Disable、导航开始和交互取消必须清除 owner，不能留下延迟回调或残余透明度。
- 动效只改变 visual rect。layout authority 产生的 draw/hit 基准矩形、safe-area 投影、BattlefieldProjection 和拖拽目标不随动画缩放或位移。
- Shell action 使用一个明确的 `down → pressed → release/cancel` 生命周期；移动超过阈值后抑制 click，释放到原目标之外、目标禁用或路线切换都不会执行命令。
- reduced-motion 下移除 travel、stagger 和 transient impulse，直接呈现静态终态；selected/loading/success/warning/error/disabled 仍依靠图标、文字、轮廓和表面状态表达。
- Lobby 只对路线进入、关卡选择和 Start 使用反馈；Battle 只对局部资源、状态、选择、波次与模态操作使用反馈；Settlement 按结果、指标、操作的阅读顺序短暂揭示。禁止给整屏 chrome 添加永久呼吸、循环闪光或无业务含义的漂浮。

### 6.2 组件覆盖矩阵

| 组件族 | N | F | P | S | D | L | OK | W | E | 拖拽派生 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Primary / Secondary / Quiet / Danger action | 必需 | 必需 | 必需 | 不适用 | 必需 | 必需 | 必需 | 必需 | 必需 | 不适用 |
| Selectable card | 必需 | 必需 | 必需 | 必需 | 必需 | 必需 | 必需 | 必需 | 必需 | 不适用 |
| Tool / nursery slot | 必需 | 必需 | 必需 | 必需 | 必需 | 必需 | 必需 | 必需 | 必需 | legal / illegal / merge / swap 全部必需 |
| Resource / metric | 必需 | 不适用 | 不适用 | 不适用 | 可用 | 可用 | 可用 | 可用 | 可用 | 不适用 |
| Status feedback | 基础信息可用 | 不适用 | 不适用 | 不适用 | 不适用 | 必需 | 必需 | 必需 | 必需 | 可承载拖拽解释 |
| Detail card | 必需 | close action 单独覆盖 | close action 单独覆盖 | 可显示选择身份 | 可用 | 可用 | 可用 | 可用 | 可用 | 不适用 |
| Blocking modal / Result card | 必需 | 内部 action 单独覆盖 | 内部 action 单独覆盖 | 不适用 | 内部 action 可用 | 必需 | 必需 | 必需 | 必需 | 不适用 |

缩写：N=`normal`、F=`hovered/focused`、P=`pressed`、S=`selected`、D=`disabled`、L=`loading/transitioning`、OK=`success`、W=`warning`、E=`error`。“可用”表示只有业务语义需要时才出现，但一旦出现必须遵守全局状态定义。

## 7. 语义资源槽（finite 56）

下列 56 个名称是最终有限美术槽合同；序列化枚举可以使用符合代码规范的拼写，但不得合并、增删或改变语义。Presenter 只请求语义，不知道具体文件名、set ID 或路径。

### 7.1 Surface / container

- `surface.screen-background`
- `surface.safe-area`
- `surface.panel-standard`
- `surface.panel-raised`
- `surface.card-selectable`
- `surface.metric`
- `surface.status`
- `surface.detail`
- `surface.modal`
- `surface.result`
- `surface.scrim`
- `surface.gameplay-stage`

`surface.gameplay-stage` 是 128×128、20px slice/safe inset、透明中心的独立九宫格，只服务唯一高密度玩法锚点。它不得借用 standard/raised/illustration frame，也不得包含棋盘、文字、按钮、植物或角落大装饰。

### 7.2 Action family

- `action.primary`
- `action.secondary`
- `action.quiet`
- `action.danger`
- `action.compact-control`
- `action.compact-control-active`

四类 action role 使用同一 anatomy 和状态名称。compact 两槽是 Quiet 紧凑形态的互斥完整状态资源，不是第五种动作角色，也不得同时绘制。若状态需要独立 Sprite，它仍属于对应 action role；页面不能自行决定是否回退到 normal 纹理。

### 7.3 Slot / marker / indicator

- `slot.tool`
- `slot.nursery`
- `marker.selected`
- `indicator.disabled`
- `indicator.loading`
- `indicator.success`
- `indicator.warning`
- `indicator.error`
- `indicator.drag-legal`
- `indicator.drag-illegal`
- `indicator.merge`
- `indicator.swap`

Selectable card 是共享组件语义，其唯一美术槽为 `surface.card-selectable`；`card.selectable` 只能作为文档中的组件别名，不得成为第二个 ArtSet slot。

### 7.4 Common icons

- `icon.resource-sun`
- `icon.resource-core`
- `icon.resource-wave`
- `icon.control-pause`
- `icon.control-continue`
- `icon.control-speed`
- `icon.control-start`
- `icon.control-start-wave`
- `icon.control-refresh`
- `icon.control-retry`
- `icon.control-return`
- `icon.control-close`
- `icon.tool-pot`

`start`、`start-wave` 与 `continue` 是三个独立语义绑定；首版 ArtSet 可以让它们指向同一 Sprite，但 Presenter 不能互相借槽或借路径。`refresh` 同样保持独立绑定。新增真正的新语义组件必须修改合同、补齐所有 intentional ArtSet 和验证；不能用文件命名约定在运行时动态发现缺失资源。

### 7.5 Ornament / illustration composition

- `ornament.screen-corner`
- `surface.section-ribbon`
- `surface.illustration-frame`
- `ornament.metric-divider`
- `ornament.result-banner`
- `illustration.orchard-vista`
- `illustration.lobby-orchard-01`
- `illustration.lobby-orchard-02`
- `illustration.lobby-orchard-03`
- `illustration.shell-orchard-depth`

这些槽只增强既有组件内部构图，不改变页面外接矩形、触控目标或流程。角饰、分隔符、横幅和插画使用显式 Icon、NineSlice 或固定宽高比 Stretch 几何；Presenter 不得按文件名猜测资源，也不得把运行时中文烘进 Sprite。

### 7.6 密集 HUD 资源图标

- `icon.resource-sun-micro`
- `icon.resource-core-micro`
- `icon.resource-wave-micro`

micro 槽只解决 Battle 顶栏最小尺寸的彩色资源识别，不属于可着色 action glyph；它们保持独立 optical bounds、manifest 绑定和最小尺寸验收。

## 8. 美术源文件与导出规则

### 8.1 Source/master 所有权

- 每个 production UI 纹理或 Sprite 必须有一个归项目所有、可继续编辑的无损 master，并能追溯到 semantic role、ArtSet 和导出设置。
- master 放入项目专用 editable-source hierarchy；使用团队可读取、无损且工具中立的可编辑格式，不能只保留某个生成器或私有工具中的不可复现状态。它必须在 runtime `Resources` 之外，也不得被 release theme/scene 直接引用。
- 风格板、原始生成输出、临时试色、评审截图和测试 fixture 都不是 production master。生成内容必须经过人工重制、边缘清理、中文字形校正、状态补齐和导出验证，才能成为 owned master。
- master 应分离 surface、outline、shadow、decoration 和切片/安全区 guide，避免每次调色重画几何。不得把运行时 copy、动态数值或状态文字烘焙进公共 UI Sprite。
- 一个语义资源只有一个明确 master；拒绝“最终版2”“备用”“兼容”之类并行来源。

### 8.2 Runtime export

- 优化后的 runtime export 只进入 `Assets/UI/Art/Runtime/<set-id>/` 对应的 production set 目录；ArtSet 定义归 `Assets/UI/Art/Sets` 所有。raw/review/source/fixture 不能混入 runtime 目录。
- 彩色 UI raster 使用无损、带 straight alpha 的 PNG；不得用 JPEG 或把透明阴影烘焙到不透明底色。
- 导出尺寸必须是 intended logical size 在批准 source scale 下的整数倍；一个 ArtSet 内使用一致的 source scale，不靠 Unity 的任意缩放修正资源大小。
- export 不自动裁掉透明 padding，不旋转图集单元，也不改变相同组件各状态的外接框。
- 独立 Sprite 与受控 atlas 均可由后续生产实现选择，但 semantic role、Sprite 名、rect、padding 和 slicing 必须稳定并可验证。atlas 不能成为运行时按名字回退查找的机制。
- 更新已 slotted 资源时保留稳定路径和 `.meta` GUID，并提高 ArtSet content revision；不在文件名后添加日期或版本号。

### 8.3 九宫格

- panel、card、action、detail、modal 和 result 等可伸缩表面使用九宫格；纯色 scrim 可以由 theme token 实现，但仍必须展示同一语义层级。
- 四角区域完整容纳圆角、描边转折和不可拉伸装饰；上下/左右 edge 只在各自轴向拉伸；中心区必须能够双向平铺/拉伸而不产生纹理接缝。
- shadow 若烘焙在同一 Sprite 中，必须包含在透明 padding 和 protected border 内；不得被 importer trimming 或组件最小尺寸裁切。
- Sprite border、ArtSet slicing metadata 和 master guide 必须一致。组件最小绘制尺寸必须大于受保护边界之和；验证失败时修正资源或组件合同，不静默降级为整图拉伸。
- 九宫格必须在组件允许的最窄、最宽和多行文字场景下检查轮廓粗细、圆角和纹理连续性。

### 8.4 透明安全边距

- 同一组件族的所有状态使用相同 canvas、透明 gutter 和视觉基线，切换状态不能跳动。
- 可见像素、抗锯齿、描边和阴影不得接触导出边界；透明区内保留合理 edge bleed，避免缩放或压缩产生浅/黑边。
- importer 使用 Full Rect 保留安全边距；禁止 Tight mesh 或自动 trim 改变对齐。
- 透明 padding 只供视觉溢出和抗锯齿使用。布局计算、content inset 和 hit rect 仍由组件/theme 明确给出，不能从透明像素猜测。
- 九宫格 border、icon safe inset、最终运行时 PNG 的 significant-alpha optical inset 和组件 optical offset 由 ArtSet metadata 保存，精确数值不散落在 Presenter 或本文。safe inset 只定义安全画布，不能代替实际可见像素边界参与 icon-label 组合居中。
- 同一 action surface 家族在相同阈值下必须具有一致、居中且完全 contained 的可见外框；由确定性 exporter 从 owned master 归一化，Presenter 不得通过扩大 draw rect 补偿资源差异。

### 8.5 图标画布

- 所有 common icon 使用同一方形逻辑 canvas、统一透明 safe inset 和统一 nominal scale；精确 canvas 与 inset 数值由 release theme/ArtSet contract 持有。
- 主体在 safe inset 内做 optical centering；箭头、暂停条等窄形状可做视觉补偿，但外接 canvas 不变。共享排版使用 exporter 从最终运行时 PNG 生成的 optical inset 计算真实可见盒，仍绘制完整 canvas。
- 操作型控制图标使用单色中性母版或纯 alpha mask；轮廓语言、线重和光学尺寸属于 ArtSet，最终颜色属于当前 action 的 `content` token。禁止在操作图标中烘焙泥土棕、金色、渐变、高光、阴影或状态色。单色可着色约束只规定颜色所有权，不要求图标几何雷同；后续可以在稳定画布和家族线重内，通过剪影、负形、比例和节奏增强各动作的识别度与个性。资源/内容图标可保留可识别的果园固有色，但不得替代 action content，也不得成为唯一状态线索。
- 图标不得包含 baked label；pause、continue、retry、return、close、warning/error 等必须仅凭形状和相邻可访问名称识别。
- outline weight、圆角语言和阴影方向在同一 ArtSet 内一致；不得混入科技线框、照片、emoji 或系统字体符号充当 production icon。

### 8.6 稳定命名

每个 ArtSet 文件夹使用同一 semantic stem，set identity 由文件夹和 `RuntimeUiArtSet` 持有，不写进每个文件名：

```text
<family>-<semantic-role>[-<state>].png
```

规则：

- 全部使用小写 ASCII `kebab-case`；family 只使用实际有限槽中的 `surface`、`action`、`slot`、`marker`、`indicator`、`icon`。Selectable card 的唯一文件 family 是 `surface`，`card.selectable` 只是组件别名。
- 示例：`surface-panel-standard.png`、`action-primary.png`、`marker-selected.png`、`indicator-warning.png`、`icon-resource-sun.png`。
- 禁止加入 `lobby`、`battle`、`settlement` 等 screen 名，除非资源语义确实只属于一个新组件且合同已批准。
- 只有确实存在独立状态资源时才加 state 后缀；是否复用 normal 不能由页面猜测，必须由有限 ArtSet contract 决定。
- master 使用同一 semantic stem 与可编辑格式；revision 记录在 ArtSet，不写 `v2`、`final`、日期或人员名。
- 文件重命名或移动必须保留 `.meta`；替换资源优先原位更新，以保持 GUID 和所有 semantic binding。

### 8.7 外部参考资源与临时替换

- 外部 APK、素材包或参考项目中的候选资源在进入 runtime export 前，必须记录来源文件哈希、包内路径、提取方法、许可/授权依据、输出格式、像素尺寸、alpha/color-space/import 设置、目标 semantic slot 和 `provisional` 状态。
- 仍受保护、不能完整解码、不能视觉复核或不能建立授权依据的字节不得进入 production ArtSet；不能按文件名猜图、截取损坏 payload 或用运行时 fallback 掩盖缺失。
- 可用候选先进入 editable-source hierarchy，成为无损、工具中立且可继续处理的临时 master，再通过现有确定性 exporter 和 importer validator 进入 runtime hierarchy。scene、Presenter 和 release theme 不直接引用 APK 路径、临时目录或原始 bundle。
- 临时使用也必须满足同一 alpha、透明边、九宫格、光学盒、中文、对比度和 WebGL 门禁，不因“后续会替换”降低 production 标准。
- runtime 只绑定既有 semantic slot；后续自有资源原位替换候选时保留路径、`.meta` GUID、Sprite border 和 slot identity，不增加兼容层、旧资源 fallback 或页面分支。

## 9. Unity importer 规则

以下是 production UI raster 的默认合同；任何例外必须由 ArtSet metadata 明确声明并被 editor validator 接受，不能成为手工记忆：

| Importer 项 | 稳定规则 |
| --- | --- |
| Texture Type | `Sprite (2D and UI)` |
| Sprite Mode | `Single`；当前 56 槽合同使用 standalone FullRect Sprite，不预留 atlas/Multiple 兼容路径 |
| Mesh Type | `Full Rect`，保留透明 padding 和九宫格边界 |
| Color Space | 彩色 UI 使用 sRGB；数据 mask 只有在新增明确合同后才可例外 |
| Alpha | 使用源 alpha，并启用透明边缘处理；不得烘焙不透明底色 |
| Filter | 果园绘制型 raster 默认 `Bilinear`；禁止各页面自行覆盖 |
| Wrap | `Clamp`，防止边缘串色 |
| Mip Maps | screen-space UI 关闭；不得靠 mipmap 模糊修复缩放问题 |
| Read/Write | runtime UI 默认关闭；editor 工具如需读取应使用 editor-only 路径 |
| NPOT / Resize | 不自动缩放；保留批准 export 尺寸与 source-scale 关系 |
| Border | 九宫格 Sprite Editor border 必须与 ArtSet slicing metadata/master guide 一致 |
| Pixels Per Unit | 同一 set 使用统一、受验证的值；精确值由资产合同持有 |
| Compression / Max Size | 使用 production platform profile，在保持 alpha、轮廓、中文邻近清晰度和无色边的前提下优化；精确格式/上限由 validator 配置持有 |
| Packing | 不打入 SpriteAtlas，不旋转或 trim；每槽的透明安全边距保留在 standalone runtime export 内 |

导入后必须验证 alpha、sRGB、filter、wrap、mipmap、compression、Full Rect、border、padding、尺寸、source/runtime ownership 和 theme reference。任何 release reference 指向 raw generation、review evidence、editable source、test fixture 或 runtime production hierarchy 外部时，聚合验证必须失败并报告具体资产。

## 10. ArtSet 互换规则

1. release application 始终只有一个 `RuntimeUiTheme` 和一个 active production `RuntimeUiArtSet`。
2. 每个 ArtSet 必须有稳定 set ID、content revision，并恰好填满有限 semantic slot contract；缺失、重复或越权资源都使候选无效。
3. ArtSet 不得继承另一个 set，不得按页面/关卡混用，不得回退到旧 revision、默认 Unity skin、文件名查找或远程资源。
4. Presenter、scene、prefab、layout 和 hit rect 只绑定共享语义合同；切换一个完整、已批准的 ArtSet 或原位更新 Sprite 不需要 C#、scene、prefab 或 Presenter-specific texture assignment。
5. preview 在 editor 内隔离运行，必须显示 common component/state gallery 与代表性 route chrome，且不能修改 serialized active set。
6. activation 先验证完整性、production ownership 和 importer 合同，再以一个可 Undo 的原子编辑替换 theme 的 active-set reference；失败时原 active set 保持不变。
7. 激活后所有 route 在同一 revision 上解析资源。正常 Unity reimport 对保留路径、`.meta` 和 semantic slot 的原位替换应同步更新全部消费者。
8. `sunny-orchard-painted` 是当前批准并激活的发布 ArtSet；完整非 active set 只用于隔离 preview 与互换验证。被否决的 source、export 和 ArtSet 定义在收口时删除，批准 master 不得误删。

## 11. 视觉审查 Checklist

以下清单用于组件预览、route 评审和最终真实画布验收。任何一项失败都不能用“功能仍可操作”豁免。

### 11.1 方向与层级

- [ ] 所有 release surface 符合 A「阳光果园」：温暖浅表面、泥土线条、叶绿主操作、阳光色强调、克制果红危险。
- [ ] Bootstrap、Lobby、Battle chrome、modal/result 与 Settlement 属于同一家族，没有默认 Unity skin、旧 Shell 或 Battle-local 混合样式。
- [ ] `background → standard → raised/card/action → modal/result` 深度清楚且阴影克制。
- [ ] 没有玻璃拟态、科技霓虹、厚重倒角、纯黑框、过亮高光或遮挡战场信息的装饰。

### 11.2 组件与动作优先级

- [ ] 等价组件 anatomy 跨 route 一致；primary、secondary、quiet、danger 层级清楚。
- [ ] 一组操作没有多个互相竞争的 primary；danger 不成为默认主焦点。
- [ ] selectable card、tool/nursery slot、metric、status、detail、modal、result 均来自共享组件合同。
- [ ] 视觉替换没有改变 copy、命令、显隐条件、layout、draw/hit rect 或拖拽结果。
- [ ] Action 角色、内容形态和行为类型分别解析；同一操作图标在 Primary、Secondary、Quiet、Danger 与 mode-active container 上使用对应 content 色，没有固定烘焙色泄漏。
- [ ] 准备阶段的开始波次是所属操作组的唯一 Primary；刷新等辅助操作不与其争夺同一主操作层级。

### 11.3 状态

- [ ] normal、focused、pressed、selected、disabled、loading/transitioning、success、warning、error 均在 gallery 中可辨。
- [ ] tool/nursery 同时覆盖 drag-legal、drag-illegal、merge、swap。
- [ ] selected、disabled、loading、warning、error 和拖拽反馈至少有颜色之外的第二线索。
- [ ] pressed、loading 和状态切换不改变组件外接框；disabled/loading 不接受重复命令。
- [ ] normal、hover/focus、pressed、mode-active 与 disabled 均使用完整且受测的 container/content 配对；active 不叠加第二张按钮面。
- [ ] 可恢复 error 保留清楚的 recovery action；blocking modal 的 scrim 正确阻断背景输入。
- [ ] 最终 WebGL 像素中按钮文字达到 `4.5:1`，操作图标目标达到 `4.5:1` 且绝不低于 `3:1`，必要边界、焦点与状态线索达到 `3:1`；灰度下仍能识别持续模式。

### 11.4 中文、数值与间距

- [ ] 所有 route 使用 release theme 的 packaged Chinese font，无默认字体或缺字方框。
- [ ] display/screen-title/section-title/body/control-label/metric/supplemental 层级一致，必要文字不低于 theme 最小值。
- [ ] 文案未烘焙进 Sprite；长中文、数字增长和加载文案不会重叠、截断或靠临时缩小字号解决。
- [ ] padding/gap/inset 使用 4pt spacing token；Lobby 与 Battle 只改变密度组合，不另建尺度。
- [ ] text-primary/secondary/inverse 与承载面满足 theme 的对比度检查。

### 11.5 竖屏、安全区与图形完整性

- [ ] 360×800、375×812、402×874、430×932 的 full safe area 均无裁切、错误拉伸、重叠或输入漂移。
- [ ] 至少一个代表性 top/bottom inset 下，必要文字、控制、九宫格边角和图标仍在 safe area 内。
- [ ] 九宫格圆角、描边、edge 和浅阴影在最窄/最宽组件上连续；没有整图拉伸痕迹。
- [ ] icon 使用统一方形 canvas、安全 inset、outline 与 optical alignment，在最小使用尺寸仍可辨。
- [ ] transparent padding、抗锯齿和 atlas 边缘没有黑边、浅边、bleed、trim 或状态跳动。

### 11.6 资产、导入与互换

- [ ] 每个 production export 有 owned lossless master、稳定 semantic stem、可追溯 source scale 和 production ownership。
- [ ] release theme/scene 未引用生成原图、风格板、评审截图、source master、fixture 或 production hierarchy 外资源。
- [ ] active ArtSet 的 set ID/revision 清楚，所有必需槽恰好一次；无继承、fallback、重复或混合集。
- [ ] Unity importer 的 alpha、sRGB、Full Rect、filter、wrap、mipmap、compression、border、padding 和 `.meta` 符合合同。
- [ ] editor preview 不改变 active set；合法 activation 原子且可 Undo；非法候选保持原 active set 不变。
- [ ] 换用完整候选或原位替换 slotted texture 后，所有 route 同步更新，代码、scene、layout 和 Presenter binding 无变化。

### 11.7 真实流程评审

- [ ] 组件 gallery 通过后，仍在真实 release canvas 检查 Bootstrap 初始化/错误/重试、Lobby 默认/选中/过渡/错误、Battle HUD/选择/详情/拖拽/暂停/终局、Settlement 胜/负/重试/返回。
- [ ] full 与 inset 证据标识同一 release theme、active set ID 和 revision，且不存在 legacy/default-skin/mixed-set chrome。
- [ ] 评审记录由对应 acceptance owner 保存；本文不写入本次是否通过、构建哈希、日期或平台支持结论。

## 12. 实施时的硬性禁项

- 不保留默认 Unity skin、旧 Shell/Battle 局部皮肤、兼容主题或缺图 fallback。
- 不由 Presenter 直接加载纹理、硬编码等价颜色/字号/间距或按 route 选择 ArtSet。
- 不通过透明像素、Sprite 尺寸或装饰偏移改变 layout/hit geometry。
- 不把植物、武器、敌人、地形或战斗特效迁入 UI ArtSet。
- 不把 review-only/生成/fixture/source 资源加入 release dependency graph 或生产 `Resources`。
- 不用运行时 skin switcher、remote loading、partial override 或多 set 混合实现快速迭代。
- 不在本文记录精确 release token 值、revision、active ArtSet identity、构建状态、截图结果、平台就绪或瞬态哈希。

## 13. 质量标准与权威工作流

本视觉系统的可读性、对齐、资源光学盒和真实画布验收由
`openspec/changes/archive/2026-08-20-polish-runtime-ui-quality-standard/evidence/runtime-ui-quality-checklist.md`
定义；机器阈值由同一变更中的
[`runtime-ui-quality-profile.json`](../../openspec/changes/archive/2026-08-20-polish-runtime-ui-quality-standard/evidence/runtime-ui-quality-profile.json)
持有。Presenter 不得复制这些阈值，也不得为了通过单个画面而加入局部例外。

### 13.1 日常编辑入口

1. 在 `Fruit Defense/UI/Visual System` 中检查 release theme、active set 和候选集；候选 preview 必须在隔离 theme clone 上完成。
2. 资源变更先运行 `RuntimeUiVisualSystemValidator.ValidateCandidate`；只有完整、无错误、无警告的 production 候选才能激活。
3. 激活通过 `RuntimeUiVisualSystemActivation.TryActivate` 完成，一次命名 Undo 只修改 release theme 的 active ArtSet；失败不得修改 theme、scene、代码或布局。
4. 提交前运行 `Fruit Defense/Validation/Run Project Smoke Validation`。该入口由 `ProjectSetup.SmokeValidate` 聚合有限 copy、八种画布变换、draw/hit 同源、对比度、资源、候选互换和共享状态门禁。
5. 发布门禁只使用 `Fruit Defense/Validation/Run P0 Release Gate`；`P0ValidationSuite.Run` 通过聚合入口消费 UI 检查，不另建散落 smoke 菜单。

### 13.2 资源迭代顺序

1. 在 `Assets/UI/Art/Sources/<set-id>/` 修改唯一 owned master，保留 semantic stem、导出目的和 `.meta`。
2. 运行该 set 的确定性 exporter，原位更新 `Assets/UI/Art/Runtime/<set-id>/` 与 manifest；禁止手改 runtime PNG。
3. 运行 release validator，修正缺槽、未绑定文件、importer、alpha 边缘、光学盒、九宫格、宽高比、review dependency 或 mixed-set 错误；不得以 fallback 绕过。
4. 在 Visual System 窗口预览完整九态组件 gallery 和代表性 route chrome；preview 不得改变 serialized theme。
5. 对完整候选执行一次原子激活/Undo/Redo 检查，最终恢复批准 active set；原位 reimport 与候选替换都不得要求修改 scene、Presenter、layout 或 hit geometry。
6. 最后运行聚合/P0，并在普通 WebGL 的 360/375/402/430 full+inset 与 402 cross-route 真实画布检查 copy、输入、缝隙、拉伸、混合集和状态线索。普通 WebGL 结果不能作为抖音或微信小游戏转换成功的证明。

### 13.3 持久质量门槛

- 普通 WebGL host 必须按宽、高两轴可用空间的较小缩放比完整 contain 竖屏画布，并保持居中、等比、无页面滚动和正确指针映射；不能依赖裁切、滚动或全屏才能触达顶部/底部内容。
- 本地发布服务对未版本化的 `index.html` 与 host 资源必须在文件原位替换后重新计算 ETag；`no-cache` 不能配合永久路径哈希缓存，否则普通刷新会继续显示旧布局。带内容哈希的 Build 文件才允许 immutable 缓存。
- 含 icon 与 label 的 action 以实际 icon alpha ink 与字体 glyph ink 的并集作为一个视觉组；该组在 action 内两轴中心偏差均不超过 2 logical point，可见间距为 4–8，且 icon/字形距可见描边至少 4。
- 单行标题、正文和 control label 必须先在 owner 内解析为按 typography line-height 居中的有限行盒，再应用 role-level optical offset；禁止把 fixed-height 字形盒顶锚到更高的 owner，或在页面 Presenter 中硬编码逐页 Y 补偿。
- Header/BattleStage 必须在 owner track 与最终 WebGL 左右可见边两处一致；outline weight 按语义分层为 standard 1–2px 与唯一 gameplay-stage 3–5px。出现第二个未批准的常驻 3px+ 大框、恢复包围下半页的外框或依赖逻辑 rect 推断最终像素都直接失败。
- Battle 的 ContextTray 必须记录 `tools` / `selected-detail` 互斥状态；验收同时记录 stage、context、nursery、refresh bounds、重框数、最终 outline band、RefreshAction 下余量和 live text containment。
- 稳定 copy 与动态 formatter 共用同一 containment gate。资源值、成本、库存数量、星级、状态前缀与原因、合成提示、详情组合及生产内容名称的边界样本必须覆盖 360/375/402/430 full+inset；resolver 返回 clamp、owner 越界或字体 ink 越界都直接失败。
- Battle 的结构验收同时检查 panel geometry、text-ink containment 与 occupied-content balance；不能以控件可点击或局部像素有颜色替代整体排版通过。
- 可比较的 header/result metric 使用同一 anatomy；同组 icon center 与文字 baseline 差不超过 1 logical point，icon 可见 alpha 距 row/card 边至少 8。
- Battle grid 的相对两侧视觉 gutter 差不超过 1 logical point；同一个 `BattlefieldProjection` 必须同时驱动绘制与 hit test，任何 chrome 调整都不得建立第二套格子或拖拽几何。
- Lobby 与 Settlement 必须声明 intentional occupied-content bounds、重复节奏和必要留白；“全部 Rect 均 contained”不能替代页面视觉重心与下半屏完成度检查。
- 结果 banner 若保留，必须承载有限、可测的 outcome 语义；空装饰不能占据主要信息层级。文字仍由 Noto Sans SC、有限 copy catalog 和语义 typography role 绘制，不能烘焙进美术。
- Noto 中文覆盖、显式行策略、实际组合对比度、非颜色状态线索、九宫格完整 partition/UV、透明边与插画宽高比都属于失败即阻断的 release gate，不能通过缩字、fallback 或放宽阈值绕过。
- active ArtSet 与每个 production candidate 都必须恰好满足 finite 56 槽及同一资源质量标准。原位更新保留路径和 `.meta` GUID；候选先隔离 preview，再单组原子激活并验证 Undo/Redo，失败保持 theme、scene、代码与布局不变。
- 以上真实画布结论只覆盖普通 WebGL；抖音、微信或其他小游戏平台仍需各自转换、模拟器、真机与包体门禁。

任何 Blocker/High、缺失有限 copy、默认 skin、回退资源、输入漂移或 release dependency 越界都必须失败关闭；不能在审查清单中作为“已知问题”保留。
