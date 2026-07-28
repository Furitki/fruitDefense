---
id: ai-whole-map-refinement-workflow-draft
parent: design-kb-home
order: 50
status: draft
---

# AI 整图地图精修与 Unity 接入流程（草稿）

> 状态：可复现实验流程草稿。它记录当前 `orchard-01` 成功链路，不代表正式美术方案、发布门禁或任意地图通用工具已经完成。

## 1. 目标与复现边界

本流程将真实游戏地图先用现有占位图块渲染成完整粗图，再把粗图作为严格构图参考交给图像模型做整图风格化，最后将模型结果作为只读视觉底图接入 Battle。地图数据、路径、种植格、碰撞和模拟仍由原逻辑对象负责，不能从生成图片反推玩法。

“可复现”在本文中表示：

- 输入地图、粗图生成、文件命名、提示词、归一化、Unity 导入、绑定、验证和回滚步骤可重复执行；
- 每次执行都保存输入、输出、SHA-256、尺寸、透明度、Unity GUID 和运行日志；
- AI 生成具有随机性，不承诺相同提示词能产生逐像素一致的图片；
- 只有确定性阶段应当逐字节可复现，模型阶段通过完整 provenance 追溯，而不是通过哈希复现。

当前参考实现固定为：

- 地图：`orchard-01`
- 逻辑尺寸：8×7 格
- 粗图与运行时整图：1024×896
- 每格参考分辨率：128×128
- Unity：`6000.3.19f1`
- 视觉模型入口：Codex 内置 `image_gen__imagegen`

## 2. 核心安全边界

1. 粗图只是模型输入，不能冒充 AI 输出。
2. 模型原始返回必须原样保留；不得只保存缩放后的运行时版本。
3. 归一化只能做整图缩放，不得裁剪、补边、局部拼接或脚本“修成”另一张图。
4. 生成图片只替换 terrain draw；`BattlefieldMapDefinition` 继续拥有路线、种植合法性、碰撞、出生点、核心位置和模拟。
5. 模型输出是否美观、是否出现视觉拓扑漂移，必须由人审核。尺寸、alpha、hash 或静态引用通过不等于视觉通过。
6. 如果要求智能体不看图，智能体不得调用图片查看、截图或视觉评分工具，只能做文件与运行时机械验证，并将图片直接交给人工审核。
7. 模型失败时必须保留失败结果，不得用 Unity/脚本生成图替代模型结果。

## 3. 当前资产与代码入口

| 职责 | 当前入口 |
|---|---|
| 粗图生成、首个候选归一化与验证 | `Assets/Editor/A5WholeMapAiRefinementGenerator.cs` |
| 卡通候选导入、绑定与 smoke | `Assets/Editor/A5CartoonCandidateImporter.cs` |
| 每地图整图视觉绑定 | `Assets/Scripts/Tilemaps/BattlefieldTerrainPalette.cs` |
| Battle terrain 绘制选择 | `Assets/Scripts/FruitDefenseGame.cs` |
| 当前地形 palette | `Assets/Battlefield/Terrain/OrchardDefaultTerrainPalette.asset` |
| 粗图与首个候选 | `Assets/Battlefield/AIRefinedMaps/A5Orchard01/` |
| 简洁卡通候选 | `Assets/Battlefield/AIRefinedMaps/A5Orchard01Cartoon/` |
| 变更与验收契约 | `openspec/changes/trial-preview-conditioned-dual-grid-atlas/` |

## 4. 端到端流程

### 4.1 准备环境

从仓库根目录运行 PowerShell。将 `$UnityExe` 修改为本机 Unity 6000.3.19f1 的实际路径。

```powershell
$ProjectRoot = (Resolve-Path .).Path
$UnityExe = 'F:\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe'
$EvidenceRoot = Join-Path $ProjectRoot 'Builds\Evidence'
New-Item -ItemType Directory -Force -Path $EvidenceRoot | Out-Null
```

批处理运行前应关闭同项目的交互 Unity，避免项目锁、旧程序集或 Play Mode 重载卡住。不要通过普通 WebGL 成功推断微信/抖音平台已经适配。

### 4.2 从真实地图生成粗图

Unity 菜单入口：

```text
Fruit Defense/A5 Whole Map/1 Generate Rough Reference
```

批处理入口：

```powershell
$arguments = @(
  '-batchmode',
  '-nographics',
  '-projectPath', $ProjectRoot,
  '-executeMethod', 'FruitDefense.Editor.A5WholeMapAiRefinementGenerator.GenerateRoughAndExit',
  '-logFile', (Join-Path $EvidenceRoot 'a5-rough-map-generation.log')
)
Start-Process -FilePath $UnityExe -ArgumentList $arguments -Wait -NoNewWindow
```

必须出现：

```text
FRUIT_DEFENSE_A5_ROUGH_MAP_OK map=orchard-01 size=1024x896 opaque=true
```

确定性输出：

```text
Assets/Battlefield/AIRefinedMaps/A5Orchard01/Orchard01-RoughMap-1024x896.png
Assets/Battlefield/AIRefinedMaps/A5Orchard01/rough-map-manifest.json
Assets/Battlefield/AIRefinedMaps/A5Orchard01/imagegen-prompt.txt
```

`rough-map-manifest.json` 至少要记录：

- mapId、8×7 逻辑尺寸、1024×896 输出尺寸和 128 像素格距；
- route/core/spawn/goal 坐标；
- `gameplayFingerprint`；
- 所有输入纹理和受保护玩法源文件的 SHA-256；
- 输出 SHA-256、非透明像素数和 `visualInspectionPerformed`。

如果地图数据、palette 或占位图块发生变化，应重新生成粗图并形成新的候选版本，不要沿用旧 manifest。

### 4.3 将粗图交给图像模型

当前内置图像工具不能由 Unity 菜单直接调用，因此这是明确的“模型桥接步骤”：操作者或 Codex 将粗图文件作为 reference image 传给 `image_gen__imagegen`，并保存真实返回文件。

参考图角色必须写清楚：

```text
Image 1: structure reference rendered from the real orchard-01 gameplay map
```

提示词至少包含以下契约：

```text
Use case: sketch-to-render
Asset type: whole-map terrain artwork for a Unity tower-defense battle board
Primary request: change only terrain surface style
Composition/framing: preserve the full canvas aspect ratio, orthographic top-down camera,
exact 8x7 registration, route shape, region boundaries, every corner and every turn
Constraints: fully opaque edge-to-edge image; no crop, padding, border, transparency,
perspective drift, moved boundaries, added paths or removed paths
Avoid: UI, text, labels, watermark, grid lines, characters, enemies, towers, plants,
pots, buildings, props, obstacles, core/spawn icons and route arrows
```

当前简洁卡通变体只增加风格描述，不改变结构约束：

```text
original simple playful 2D cartoon tower-defense environment;
chunky rounded silhouettes; bold clean outlines; flat cel shading;
saturated garden greens and warm soil browns; minimal texture noise
```

不要要求复制具体游戏的角色、Logo、UI 或素材。若用知名游戏描述方向，应将其拆成可执行的通用视觉特征，并要求原创结果。

每次模型调用必须创建新的 sibling 目录或版本化文件名，不能覆盖已审核候选。至少保存：

```text
<candidate>/imagegen-prompt.txt
<candidate>/<map>-AIRefined-Raw.png
<candidate>/candidate-manifest.json
```

manifest 至少记录 tool、use case、rough/prompt/raw 路径及 hash、raw 尺寸、透明度和 `visualInspectionPerformed`。

### 4.4 保留原图并生成运行时版本

模型原始返回尺寸可能不是 1024×896。处理规则：

1. 原始文件保持不变；
2. 解码并确认完整画布不透明；
3. 对整张图做一次高质量双线性缩放到 1024×896；
4. 不裁剪、不补边、不做局部合成、拓扑修补或视觉润色；
5. 运行时文件使用新的版本化名称；
6. 将 raw/runtime 尺寸、hash 和规则写入 manifest。

首个固定 A5 路径可通过以下菜单完成归一化与绑定：

```text
Fruit Defense/A5 Whole Map/2 Normalize AI Result + Bind
```

对应批处理方法：

```text
FruitDefense.Editor.A5WholeMapAiRefinementGenerator.NormalizeBindValidateAndExit
```

当前卡通 sibling 使用：

```text
Fruit Defense/A5 Whole Map/Import Cartoon Candidate
Fruit Defense/A5 Whole Map/Bind Cartoon Candidate
```

当前脚本仍固定了目录与尺寸。复制本流程到新地图前，必须先完成第 9 节的参数化待办。

### 4.5 Unity 导入设置

运行时整图使用以下导入设置：

| 设置 | 值 |
|---|---|
| Texture Type | Default |
| sRGB | true |
| Alpha Source | None |
| Alpha Is Transparency | false |
| Mip Maps | disabled |
| Filter Mode | Bilinear |
| Wrap Mode | Clamp |
| Compression | Uncompressed |
| NPOT Scale | None |
| Max Size | 2048 |

导入后必须记录运行时 Texture GUID。静态文件存在不等于 Unity 已引用正确 GUID。

### 4.6 绑定为每地图视觉层

`BattlefieldTerrainPalette` 通过 `wholeMapVisuals` 保存 `mapId → Texture2D → enabled` 绑定。当前 `OrchardDefaultTerrainPalette` 只为 `orchard-01` 启用一个整图视觉。

卡通候选的批处理绑定与 smoke：

```powershell
$arguments = @(
  '-batchmode',
  '-nographics',
  '-projectPath', $ProjectRoot,
  '-executeMethod', 'FruitDefense.Editor.A5CartoonCandidateImporter.BindValidateSmokeAndExit',
  '-logFile', (Join-Path $EvidenceRoot 'a5-cartoon-bind-smoke.log')
)
Start-Process -FilePath $UnityExe -ArgumentList $arguments -Wait -NoNewWindow
```

必须同时出现：

```text
FRUIT_DEFENSE_A5_CARTOON_BOUND_OK
FRUIT_DEFENSE_SMOKE_OK
```

绑定验证必须比较实际选中的 `Texture2D` 与目标 runtime asset，而不是只检查列表中存在一个 1024×896 纹理。

### 4.7 处理直接运行 Battle 的兼容路径

正式流程 `Bootstrap → Lobby → Battle` 会从关卡主题解析 terrain palette。直接打开 `Battle.unity` 时，standalone compatibility simulation 没有 resolved Theme；如果仍强制依赖 Theme，terrain palette 会被跳过，画面回退到旧几何地图。

当前修复规则：

- 有 resolved Theme：严格按 `theme.TerrainPaletteId` 解析；
- 无 resolved Theme 的直接 Battle 验收：只允许选择唯一注册的默认 palette；
- 找不到或验证失败：打印 `FRUIT_DEFENSE_TERRAIN_PALETTE_MISS` 并回退；
- 直接运行成功：打印 `FRUIT_DEFENSE_STANDALONE_TERRAIN_PALETTE_FALLBACK`。

整图模式下，空闲状态不再覆盖旧的种植格矩形与文字；只有选择花盆工具或拖拽花盆时才重新显示扩建交互提示。交互判定本身没有改变。

### 4.8 Play Mode 实际命中验证

静态 palette 校验不能证明玩家看到新图。最终必须让 Battle 进入 Play Mode，并确认 terrain draw 分支实际命中目标纹理。

菜单入口：

```text
Fruit Defense/A5 Whole Map/4 Open Battle For Manual Review
```

交互 Unity 启动示例：

```powershell
$runtimeLog = Join-Path $EvidenceRoot 'a5-runtime-hit.log'
$arguments = @(
  '-projectPath', $ProjectRoot,
  '-executeMethod', 'FruitDefense.Editor.A5WholeMapAiRefinementGenerator.OpenBattleForManualReview',
  '-logFile', $runtimeLog
)
Start-Process -FilePath $UnityExe -ArgumentList $arguments
```

必须出现并核对精确纹理名与尺寸：

```text
FRUIT_DEFENSE_A5_RUNTIME_DRAW_HIT map=orchard-01 \
palette=palette.orchard.default \
texture=<selected-runtime-texture-name> size=1024x896
```

以下情况都不能判为已实装：

- 只有 `BOUND_OK`，没有 `RUNTIME_DRAW_HIT`；
- Unity 仍使用修改前的 `Assembly-CSharp.dll`；
- 编辑器卡在 `Reloading assemblies for play mode`；
- 命中的是旧纹理名或 TileSet fallback；
- 人工仍在 Scene 视图或另一个旧 Unity 进程中验收。

### 4.9 人工视觉验收

人工在 Unity `Game` 页签检查：

1. 道路、草地、土地区域是否与玩法地图一致；
2. 转角、边缘和交界是否出现模型漂移；
3. 是否存在意外文字、UI、角色、塔、障碍或新增路径；
4. 运行时缩放是否造成拉伸、模糊或明显形变；
5. 交互提示出现时是否仍能正确对应逻辑格；
6. 视觉风格是否达到本轮目标。

视觉拒绝只否决该候选，不应修改玩法数据。下一轮应创建新的 sibling 候选，并一次只调整一个主要提示词变量。

## 5. 机械验收清单

| 阶段 | 必须满足 |
|---|---|
| 粗图 | 可解码、1024×896、完全不透明、地图 fingerprint 与受保护源 hash 已记录 |
| 模型输入 | reference 路径与 prompt 原文已保存 |
| Raw | 来自真实模型调用、文件原样保留、尺寸/alpha/hash 已记录 |
| Runtime | 1024×896、完全不透明、仅整图缩放、独立 hash/GUID |
| Unity Import | sRGB、no alpha、no mipmap、Bilinear、Clamp、Uncompressed、NPOT None |
| Binding | mapId 和目标 Texture2D 精确匹配，TileSet fallback 仍存在 |
| Gameplay | map fingerprint、路线、碰撞、种植规则和 simulation 不变 |
| Play Mode | 日志出现目标纹理的 `FRUIT_DEFENSE_A5_RUNTIME_DRAW_HIT` |
| Visual | 由人工审核，不由自动化或智能体替代 |

## 6. 常见失败与诊断顺序

| 现象 | 优先检查 |
|---|---|
| 游戏仍是旧几何地图 | 搜索 `TERRAIN_PALETTE_MISS`；直接 Battle 是否缺 Theme |
| 静态绑定通过但画面没变 | 是否存在 `RUNTIME_DRAW_HIT`，命中的纹理名是否正确 |
| Unity 不吸收代码修改 | DLL/日志时间戳、项目锁、是否卡在 Play Mode assembly reload |
| 能看到整图但仍像格子地图 | 是否仍在空闲状态绘制 planting cell rectangles/labels |
| AI 图移动道路或边界 | 人工拒绝候选；不能用图片改变逻辑地图来迁就它 |
| AI 接口失败 | 保留失败；不得用粗图或脚本输出冒充 AI 结果 |
| Raw 尺寸不同 | 保留 raw，整图无裁剪缩放到运行时尺寸并记录规则 |
| 交互格与图片错位 | 检查 `Projection.GridRect`、画布比例和 8×7 注册，不改碰撞来追图片 |

## 7. 回滚

首选非破坏性回滚：

1. 在 `OrchardDefaultTerrainPalette` 中禁用或移除 `orchard-01` 的 whole-map binding；
2. 保留原始 TileSet、粗图、raw、runtime、prompt 和 manifest；
3. 重新进入 Play Mode；
4. 确认出现 `FRUIT_DEFENSE_TILESET_TERRAIN_FALLBACK`；
5. 再次运行 project smoke。

回滚不需要修改 `Battle.unity`、地图数据或关卡目录。

## 8. 当前可核对样例

当前简洁卡通样例：

```text
Rough SHA-256:
565059d42741e6b725cac944f7906ddc4b82fdac02da2bdbb9e52f553181dbd9

Raw SHA-256:
635222935f91d6726105d5daaf6f823d67d6e0f3d9b576c98fc4cc7f78d38e8a

Runtime SHA-256:
53fa5bd6208295fce8d0f122900bb2e36d98fd92b41d52823c530c9c77a87693

Runtime Unity GUID:
e9d9fd1fab5978c47a5511d151c9b28c

Gameplay fingerprint:
gameplay-map.11a0da7a46ecceaf
```

这些值用于核对当前样例，不应成为未来候选必须相同的固定值。

## 9. 泛化前待办

当前流程可以复现本次 `orchard-01` 试验，但要成为正式通用工具仍需：

- 将 mapId、grid size、cell pixels、输出目录、候选名和目标 palette 从代码常量改为 profile；
- 把 raw → runtime 的整图归一化提取为所有候选共用的 Unity 工具；
- 自动生成统一 schema 的 candidate/integration manifest；
- 为 palette 选择和目标 Texture2D 增加 Play Mode 自动测试，不只依赖日志；
- 将 direct-Battle fallback 限制为明确的本地开发模式，并覆盖多 palette 场景；
- 增加候选启用/禁用菜单，避免手工编辑 YAML；
- 将模型桥接步骤封装为明确的操作者协议，但继续保留人工视觉门；
- 为任意地图建立独立的粗图尺寸与相机映射契约；
- 在视觉批准后再讨论是否进入正式 release palette，不从本试验推断平台发布能力。

完成这些待办前，本文件保持 `draft`。
