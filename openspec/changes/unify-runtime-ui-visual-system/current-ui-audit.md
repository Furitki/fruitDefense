# 当前运行时 UI 视觉盘点

## 盘点边界

本盘点对应任务 1.1，只记录现状，不定义最终视觉值，也不修改运行时代码、美术资产、场景或策划文档。

- 基准构建：当前工作区已有普通 WebGL 构建，payload 版本为 loader `c222dba000b7`、data `4774cd1e5a6d`、framework `ecf670072498`、wasm `6f50b3bba3ee`。
- 基准画布：`402 × 874` CSS pixel，`safeTop=0`、`safeBottom=0`，设计缩放 `1`。
- 正式流程：`Bootstrap → Lobby → Battle → Settlement`；release-flow 与 Battle 状态矩阵均由现有 `scripts/accept-webgl-portrait.ps1` 驱动真实 WebGL canvas。
- 证据索引：[`evidence/current-ui/README.md`](evidence/current-ui/README.md)。

## 截图覆盖结果

| 路由 / 状态 | 捕获结果 | 证据 | 结论 |
|---|---|---|---|
| Web 宿主加载 | 已捕获上下文图；请求 402 × 874 viewport，浏览器可见区 PNG 为 387 × 841 | [`bootstrap/00-after-navigation.png`](evidence/current-ui/bootstrap/00-after-navigation.png) | 仅是 Unity Web 模板的黑底、Unity 标志、进度条和滚动条，不是 `AppFlowCoordinator.OnGUI` 的运行时 Bootstrap 表面，不能当作 402 × 874 运行态 Bootstrap 验收。 |
| Bootstrap 初始化 / 阻塞错误 / 重试 | 未形成稳定运行态截图 | 代码依据：`Assets/Scripts/App/AppFlowCoordinator.cs` 的 `OnGUI` | 现有脚本在 Unity 实例完成加载后才继续，初始化表面只存在很短时间；acceptance bridge 仅支持 Battle `victory` / `defeat`，没有启动挂起或失败注入。详见“未解决证据缺口”。 |
| Lobby 当前选中态 | 已捕获并通过 flow gate | [`release-flow/01-lobby.png`](evidence/current-ui/release-flow/01-lobby.png) | 同屏包含选中卡、两个未选中卡和主操作，可直接比较卡片状态与动作层级。 |
| Battle ready HUD | 已捕获并通过状态 gate | [`battle-states/01-ready.png`](evidence/current-ui/battle-states/01-ready.png) | 覆盖标题、三项资源/波次、暂停/倍速、状态条、工具、苗圃和刷新动作。 |
| Battle active HUD | 已捕获并通过状态 gate | [`battle-states/02-active-wave.png`](evidence/current-ui/battle-states/02-active-wave.png) | 覆盖运行中状态、战场内容与 HUD 的关系。 |
| Battle 详情卡 | 已捕获并通过交互 gate | [`battle-states/11-inspection-click.png`](evidence/current-ui/battle-states/11-inspection-click.png) | 覆盖选中轮廓、攻击范围、详情面板和关闭动作。 |
| Battle 拖拽反馈 | 已捕获并通过交互 gate | [`battle-states/09-drag-target.png`](evidence/current-ui/battle-states/09-drag-target.png) | 覆盖拖拽 ghost 与目标轮廓；其余点击/移动前后证据见 evidence 索引。 |
| Battle 暂停弹窗 | 已捕获并通过会话 gate | [`battle-states/05-paused.png`](evidence/current-ui/battle-states/05-paused.png) | 覆盖 scrim、弹窗、主操作和危险操作。 |
| Settlement 胜利 | 已捕获并通过 flow gate | [`release-flow/03-settlement.png`](evidence/current-ui/release-flow/03-settlement.png) | 覆盖结果标题、三项指标、重试和返回动作。 |

## 当前视觉实现分区

| 区域 | 当前实现 | 视觉资源来源 | 独立样式路径 |
|---|---|---|---|
| Bootstrap 初始化/错误 | `AppFlowCoordinator.OnGUI` | `GUI.skin.box/label/button`，未绑定项目中文字体 | 是，完全依赖默认 Unity skin |
| Lobby / Settlement | `ShellStyleSet` + `ShellGui` | `GUI.skin` 背景，`Texture2D.whiteTexture` 深绿着色，Noto Sans SC 字体 | 是，Shell 局部样式 |
| Battle chrome / modal | `FruitDefenseGame.BuildStyles`、`DrawPanel`、`ColoredButton` | `Texture2D.whiteTexture` 着色，内容 atlas，Noto Sans SC 字体 | 是，Battle 局部样式 |
| Battlefield content | `FruitDefenseGame` terrain/content rendering | 关卡地形与内容 sprite | 不属于共享应用 chrome，但当前与 Battle UI 紧密交织 |

现状因此不是一套可换资源的 UI 框架，而是三个独立 IMGUI 视觉实现。任何美术调整都需要在 Bootstrap、Shell 和 Battle 中分别改代码或依赖默认皮肤行为。

## 组件逐项不一致审计

### 1. Screen background 与 safe-area surface

- Bootstrap 直接基于 `Screen.width/height` 放置一个最多 360 宽、190 高的默认 Box，没有调用 `RuntimeSafeAreaResolver`；Web 宿主加载画面另有黑底和浏览器滚动条风险。
- Lobby/Settlement 用 `ShellGui.DrawPanel` 把整个 safe area 填为 `Color(.18,.25,.20)`，然后叠加透明度 `.32` 的默认 `GUI.Box`。
- Battle 先填整屏和 402×874 设计面，颜色来自关卡 theme background，缺失时回退 `Color(.91,.86,.75)`；safe area 通过 `BattlefieldProjection.CalculateViewportLayout` 居中缩放。
- 不一致：应用背景同时存在黑色 Web 模板、深墨绿 Shell 和暖米色 Battle；Bootstrap 无 safe-area 合约，Shell 顶部锚定而 Battle 在 safe area 中居中，跨页视觉锚点会跳变。

### 2. Screen title

- Lobby/Settlement：31px、Bold、居中、白色。
- Battle：20px、Bold、左对齐、深棕 `Color(.20,.13,.08)`。
- Bootstrap：默认 `GUI.Label`，没有项目字体、字号、字重或颜色角色。
- 不一致：同为页面一级标题却有 31/20/default 三档、居中/左对齐两种布局、白/深棕两种前景，无法形成稳定标题层级。

### 3. Standard / raised panel

- Shell：深绿纯色底上叠默认 Box，产生默认 Unity 灰黑渐变、细描边和直角表面。
- Battle：用 2px 深棕外框 `Color(.34,.25,.15)` 加纯色内填；header、detail、modal 使用不同的局部米色值。
- Settlement result card 与页面背景颜色几乎相同，只靠默认 Box 边缘区分；卡内保留大面积空白。
- 不一致：边框宽度、圆角、阴影、表面层级和内边距没有共享规则；当前没有九宫格或受保护边角，所有表面均为拉伸白纹理或默认皮肤。

### 4. Selectable level card

- 未选卡直接使用 Shell secondary button（18px normal）；选中卡复用 primary button（22px bold），并在文案前加“✓ 已选择”。
- 优点：选择不只依赖颜色，已有文字/符号第二线索。
- 问题：选择态会同时改变字号和字重，使同一信息的排版密度变化；卡片没有专用 normal/pressed/selected/disabled anatomy，按钮语义与卡片语义混用。
- 当前视觉仍由默认 Unity button 的灰黑渐变决定，和 Battle 的黄色描边选择态不是同一家族。

### 5. Primary / secondary / quiet / danger action

- Lobby Start、Settlement Retry/Return 都使用 `GUI.skin.button`；所谓 primary/secondary 只改变字号/字重，背景仍是同一默认渐变。
- Battle Start Wave 是橙色，Refresh/Continue 是叶绿色，Pause 是黄褐色，Speed 是浅绿，Restart 是红色；均通过 `GUI.backgroundColor` 给默认白纹理按钮着色。
- Bootstrap Retry 使用未经包装的默认 button。
- 不一致：等价主操作跨页没有共同形状、颜色、描边、按压和禁用规则；Shell 的主次只靠文字大小，Battle 的主次主要靠局部硬编码颜色。没有 quiet action 的统一表现。

### 6. Typography 与中文层级

- Lobby/Settlement 与 Battle 都加载 `Resources/Fonts/NotoSansSC-UI`，这是目前唯一明显的跨页共同点；Bootstrap 没有加载它。
- Shell：13、14、18、19、22、28、31px 多档；Battle：10、12、15、16、18、20、24px 多档，且大量 `Style(...)` 临时创建。
- Shell 全部状态都强制白字；Battle 主要使用深棕，战场实体/星级使用白字。
- 不一致：相近语义映射到不同字号和对齐；补充文字在 Shell 的深色底上偏灰且尺寸较小，Battle 的 10/12px 星级与状态在 WebGL 参考尺寸上密度高；Bootstrap 中文字体覆盖不可由当前代码保证。

### 7. Spacing rhythm 与密度

- Shell 使用 16px 水平边距、18px 顶部边距，Lobby 卡片 82px 高、16px 间隔，Start 前间隔 26px；Settlement result card 260px 高，按钮间隔 18px。
- Battle 使用固定 8px 外边距、4/6/8px 局部间隔，header 60px、tool tray 68px、nursery tray 80px、detail 70px，形成更紧密的工具型密度。
- 不一致：不存在统一 4pt rhythm 的命名 token；同一 402×874 画布上，Shell 非常疏、Battle 非常密，跨页像两个产品。内边距有时由 GUI skin 决定，无法通过资源集可靠迭代。

### 8. Resource / metric display

- Battle header 把阳光、核心、波次写成三段纯文本；Settlement 把完成关卡、波次、生命作为三行居中文字；Lobby 将关卡 ID 拼进 Start 按钮。
- 没有共享 metric container、标签/数值层级或资源图标；`<b>` 仅在 Battle 数值中局部使用。
- 不一致：相同“标签 + 值”信息在不同页面使用不同对齐、字号、间隔和容器，无法替换一套资源图标后统一生效。

### 9. Tool / nursery slot

- Battle 工具格以浅绿矩形和内容 sprite 表达，选中/拖拽时加黄边；无库存时仍保留普通背景，只靠 `×0` 与点击逻辑表达不可用。
- Nursery 空位是裸文字，植物位使用无框 sprite 与白色星级；工具格、苗圃格和 Lobby 卡片没有共同的卡片/槽位语言。
- 不一致：disabled、selected、dragging 不是同一状态矩阵；缺少统一禁用遮罩、锁定/不可用图标和按压反馈。

### 10. Status / success / warning / error feedback

- Bootstrap blocking error 是默认 Label + Retry；Lobby/Settlement 错误是页面底部白色纯文本；coordinator recoverable error 又直接绘制在屏幕底部。
- Battle 状态用 `✓` 或 `!` 文本前缀、短时颜色和战场浮字；合法/非法拖拽主要依赖绿/红轮廓，merge/swap 又使用黄/蓝。
- 优点：部分错误已有 `!`，Lobby selected 有 `✓`，不是完全依赖颜色。
- 问题：success/warning/error 的位置、容器、图标、持续时间和文案层级均不统一；拖拽语义颜色数量多且没有共享图例，色觉友好线索不足。

### 11. Contextual detail card

- Battle 详情卡使用独立米灰面板、16px 标题、15px正文和右侧 44px 宽关闭按钮；它紧贴画布底部并只在选择植物时出现。
- 关闭按钮颜色与其他 secondary/quiet action 均不同；详情面板和 Settlement result card 没有共享 anatomy。
- 不一致：缺少 detail 的统一标题区、内容间距、关闭图标和进入/退出状态；美术替换需要改 `DrawSelectedPlant` 的局部颜色与代码。

### 12. Blocking modal 与 result card

- Battle modal 使用 `.68` 深棕 scrim、2px 深棕边、暖米色面板、24px 标题；Continue 绿、Restart 红。
- Settlement result card 没有 scrim，是 Shell 默认 Box；28px 结果标题和三行 18px 指标。
- Bootstrap error panel也是 blocking surface，但使用默认 Box 且位置由 `Screen.height * .3` 决定。
- 不一致：三个 blocking/result 表面没有共同边角、深度、标题、正文、按钮排列和危险状态规则。

### 13. Icon / texture / art ownership

- Shell 与 Bootstrap 没有产品 UI icon；勾选符号和错误标记由文字承担。
- Battle 的工具与花盆直接复用内容 atlas sprite，UI 表面与按钮仅使用 `Texture2D.whiteTexture`；没有 UI 专属 production hierarchy、九宫格、切片或透明安全边界。
- 默认 Unity skin 仍决定 Bootstrap、Lobby、Settlement 和部分 Battle button 的具体像素，无法以一组语义资源完整替换。
- 不一致：当前不存在可枚举的 art-set identity/revision 或 slot completeness；替换图片不能保证所有路由同步，也无法避免旧/新资源混用。

## 跨页面差异矩阵

| 审计项 | Bootstrap | Lobby / Settlement | Battle | 统一化结论 |
|---|---|---|---|---|
| 颜色 | 默认 skin | 深绿 + 白字 + 灰黑渐变 | 暖米色 + 深棕 + 绿/黄/红 | 以语义色替代页面硬编码色 |
| 字体层级 | 未绑定项目字体 | 13–31px，多数居中 | 10–24px，左/中混合 | 统一 display/title/section/body/control/metric/supplemental |
| 间距 | 原始 screen rect | Shell 独立布局常量 | Battle 固定设计 rect | 保留布局 authority，抽取共享视觉 metric |
| 面板/按钮/卡片 | 默认 Box/Button | 默认 skin + 深绿底 | 白纹理着色 + 2px 边 | 建立共享九宫格表面和四级 action |
| 状态反馈 | 错误 + Retry | 选中勾选、GUI.enabled、底部纯文本 | 轮廓、颜色、scrim、短时文本 | 统一 normal/pressed/selected/disabled/loading/success/warning/error |
| 图标/纹理 | Unity 模板/skin | 无 UI icon | 内容 sprite + whiteTexture | 使用完整语义 UI art set，内容 art 与 chrome 分界 |
| 默认 Unity skin | 全量依赖 | 按钮/Box 依赖 | Button background 仍受 IMGUI，面板自绘 | 最终 release surface 不允许默认 skin |
| safe area | 未使用 | safe-area 顶部锚定 | safe-area 居中等比投影 | Bootstrap 纳入 resolver；保持每页既有 draw/hit authority |
| 跨页一致性 | 第三套 | Shell 内部部分一致 | Battle 内部部分一致 | 单一 theme + 单一 active art set |

## 风格板需要覆盖的语义资源槽

以下是从现有组件反推的最小候选清单，用于任务 1.2 风格板。它是审计输入，不提前替代任务 2.3 的最终有限槽位定义。

### Surface / container

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
- `surface.scrim`（若最终由纯色 token 实现，风格板仍需展示）

### Action family

- `action.primary`
- `action.secondary`
- `action.quiet`
- `action.danger`

四类 action 均需在同一 component gallery 中展示 `normal`、`hover/focused`、`pressed`、`disabled`、`transitioning/loading`；是否拆为独立纹理槽由后续 art-set 契约决定，不能由页面自行选择。

### Card / slot / state marks

- `card.selectable`
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

### Common icons

- `icon.resource-sun`
- `icon.resource-core`
- `icon.resource-wave`
- `icon.control-pause`
- `icon.control-continue`
- `icon.control-speed`
- `icon.control-start-wave`
- `icon.control-retry`
- `icon.control-return`
- `icon.control-close`
- `icon.tool-pot`

武器与植物主图继续属于 gameplay content art，不应被 UI art set 重新拥有；风格板只需展示它们放进 `slot.tool` / `slot.nursery` 后的边距、底板、选中、禁用和拖拽处理。

## 任务 1.2 风格板的优先比较点

1. 先让 `screen-background → raised panel → selectable card/action` 的深度关系在 Lobby、Battle、Settlement 三个代表切片中一致。
2. 用同一套 primary/secondary/quiet/danger anatomy 重做 Lobby Start、Battle Start Wave/Refresh/Pause、Settlement Retry/Return 和 modal actions 的视觉示例。
3. 用同一排 typography specimen 对照当前 31px Shell 标题、20px Battle 标题、10/12px Battle 补充文字，确定中文层级与最小可读尺寸。
4. 明确 selected、disabled、loading、success、warning、error、drag legal/illegal 的双重线索，不只给色值。
5. 同一语义槽至少给出两套可互换 treatment，确保换资源不改变组件、布局或 Presenter 绑定。

## 未解决证据缺口

### Bootstrap 运行态截图

当前没有不改运行时代码即可稳定停留在 `AppFlowCoordinator.OnGUI` 初始化或 blocking error 表面的正式入口：

- `scripts/accept-webgl-portrait.ps1` 会等待 `fruitDefenseUnityInstance`、目标画布和 acceptance route ready，因而越过短暂初始化表面；
- `ConfigureAcceptanceFlow` 只接受 `victory` / `defeat`，不能挂起 platform/config/profile 初始化，也不能注入可重试启动失败；
- 直接浏览器抓到的 `bootstrap/00-after-navigation.png` 是 Web 模板加载器，不是 runtime Bootstrap；
- 任务边界禁止为截图增加一次性 runtime hook，因此本轮没有伪造 Bootstrap 证据。

可复现检查：

```powershell
rg -n -C 8 "ConfigureAcceptanceFlow|private void OnGUI" Assets/Scripts/App/AppFlowCoordinator.cs
```

后续任务 3.5 若必须取得稳定 Bootstrap initializing/error/retry 的真实 WebGL 证据，应在正式 acceptance catalog 中提供可验证、只在 acceptance launch 生效的状态驱动，并在任务结束时保留为稳定自动化能力或按项目规则删除一次性 helper；不能把 Unity Web 模板进度条当作 Bootstrap UI。
