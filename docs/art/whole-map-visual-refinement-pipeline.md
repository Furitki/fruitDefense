---
id: whole-map-visual-refinement-pipeline
parent: design-kb-home
order: 56
status: active
---

# AI 整图地图精修与 Unity 接入管线

本文定义完整地图视觉底图的生成、审核和 Unity 接入契约。它与 Dual-Grid 图块生成是两条独立管线：整图候选只替换视觉绘制，不生成 Mask，不拥有玩法拓扑，也不能反向修改路线、碰撞、种植合法性、点位或战斗模拟。

当前仓库没有通用的整图生成/绑定自动化入口。本文先作为后续实现必须遵守的正式契约；任何整图候选要进入生产资源，必须通过独立 OpenSpec 变更实现参数化工具、绑定、回滚和 Play Mode 验收。普通图片生成或静态绑定不能冒充已实装。

## 1. 权威边界

整图视觉不得拥有或推导以下数据：

- `mapId`、逻辑网格尺寸和地图版本；
- 怪物路线、刷怪点、路线目标和核心；
- 种植能力、碰撞、道具或触发点；
- 战斗随机种子、内容版本、快照和模拟结果。

这些信息继续由版本化地图聚合、玩法拓扑、路线和点位数据拥有。整图只消费已发布地图的投影视觉参考。视觉拒绝只能否决候选，不得通过移动玩法数据迁就图片。

## 2. 版本化工作包

每个候选使用独立目录：

```text
Builds/Evidence/whole-map-runs/<date>-<mapId>-<candidate>/
  source/
    RoughMap.png
    rough-map-manifest.json
  request/
    pipeline-prompt.txt
    model-call.json
  model/
    Raw.png
    Runtime.png
  integration/
    candidate-manifest.json
    unity-import.json
    runtime-hit.json
  evidence/
    mechanical-qa.json
    visual-review.json
    ready.json
```

不得覆盖已审核或待审核候选。失败重试创建 sibling run，并保留失败的 prompt、调用记录和 Raw。

## 3. 从真实地图生成粗图

粗图必须由当前版本化地图、正式方格投影和选定表现主题生成，不能用手画参考替代。`rough-map-manifest.json` 至少记录：

```text
mapId
mapVersion
logicalGridWidth / logicalGridHeight
cellPixels
outputWidth / outputHeight
gameplayFingerprint
route / spawn / goal / core identifiers
terrain theme or palette identity
all protected gameplay source hashes
rough path / hash / decoded-pixel hash
opaque pixel count
visualInspectionPerformed
```

粗图必须可解码、全不透明、无额外边框，并与地图画布宽高比严格一致。地图数据、主题或投影发生变化时必须生成新候选，不能刷新旧 manifest 接受变化。

## 4. 模型桥接

模型输入角色固定为：真实地图渲染得到的精确结构参考。提示词至少约束：

```text
Use case: sketch-to-render
Asset type: whole-map terrain artwork for a Unity tower-defense board
Preserve: full canvas aspect ratio, orthographic top-down camera, exact logical-grid
registration, route silhouette, terrain-region boundaries, every corner and turn
Change only: terrain surface style
Avoid: crop, padding, border, transparency, perspective drift, moved or added paths,
UI, text, labels, watermark, characters, enemies, towers, plants, pots, buildings,
props, obstacles, spawn/core icons and route arrows
Output: one fully opaque edge-to-edge image and no explanation
```

风格描述只能补充颜色、形状、材质和细节密度，不能放松结构契约。使用知名作品描述方向时，应拆成通用视觉特征并要求原创，不复制角色、Logo、UI 或具体素材。

`model-call.json` 必须记录真实工具、模式、use case、input/prompt/raw 路径与 hash、一次调用、重试、fallback、原始尺寸、alpha、执行时间和视觉检查状态。模型失败、Raw 不可解码或存在非预期透明时停止；不得用粗图、旧候选或脚本输出冒充 Raw。

## 5. Raw 与 Runtime

Raw 原样保存。Runtime 只能对整张图执行一次明确的尺寸归一化：

1. 保持完整画布，不裁剪、不补边；
2. 不做局部对齐、拓扑修补、调色或重绘；
3. 普通连续色整图默认使用高质量 bilinear；像素风候选必须在 profile 中显式改为 Point；
4. 输出尺寸由 `logicalGrid × cellPixels` 唯一得到；
5. manifest 记录算法、Raw/Runtime 尺寸、hash 和解码像素 hash；
6. 机械 QA 从 Raw 独立重算 Runtime 并逐像素比较。

整图管线的缩放规则与 Dual-Grid Runtime32 中心采样不同，二者不得共用默认插值方式。

## 6. Unity 导入契约

连续色整图的默认导入设置：

| 设置 | 默认值 |
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
| Max Size | 不小于 Runtime 最大边 |

像素风整图可以使用 Point，但必须在候选 profile 和 `unity-import.json` 中明确记录。导入后记录 Texture GUID、资产路径和实际 importer 设置。文件存在不等于 Unity 引用了正确 GUID。

## 7. 按地图绑定

绑定必须是显式、版本化且可回滚的：

```text
mapId -> presentation theme/palette -> whole-map Texture2D -> enabled
```

要求：

- 一个绑定只服务一个明确 `mapId` 和地图版本；
- 找不到绑定时回退正式 TileSet 表现，不改变玩法；
- 多 palette 场景不得选择“任意第一个”候选；
- 正式 `Bootstrap → Lobby → Battle → Settlement` 与本地直接 Battle 验收必须明确区分；
- 直接 Battle fallback 只能存在于明确的本地开发模式；
- 禁用整图绑定即可非破坏性回滚到 TileSet。

当前仓库尚未提供这一通用绑定层。未来实现时必须放在稳定的 `Assets/Editor/Tools` 工作流中，并在 `Assets/Editor/Tests` 增加自动验证。

## 8. 运行时命中证明

静态引用通过不能证明玩家看到了目标图片。Play Mode 必须记录并核对：

```text
mapId
mapVersion
theme/palette id
selected texture asset path and GUID
selected texture name and dimensions
runtime draw branch
gameplayFingerprint before/after
```

以下情况都不能判为已实装：

- 只有绑定成功，没有 Runtime draw hit；
- 命中旧纹理、旧程序集或 TileSet fallback；
- 在 Scene 视图、错误 Unity 进程或错误地图中检查；
- gameplay fingerprint、路线、碰撞或种植规则发生变化；
- 只完成普通 WebGL 构建，没有目标平台自己的转换和设备证据。

## 9. 人工视觉验收

人工在实际 Game 视图检查：

1. 道路、草地、土壤、水面等区域是否与玩法地图一致；
2. 转角、边缘和交界是否出现模型漂移；
3. 是否出现意外文字、UI、角色、塔、防御物或新增路径；
4. Runtime 缩放是否造成拉伸、模糊或形变；
5. 交互提示是否仍对应逻辑格；
6. 风格是否达到候选目标。

视觉审核结果写入独立 `visual-review.json`。智能体被要求不看图时只能进行机械验证，必须记录 `visualInspectionPerformed=false` 并把图片直接交给人工。

## 10. 机械验收

| 阶段 | 必须满足 |
|---|---|
| Rough | 来自真实地图；尺寸、alpha、fingerprint、受保护 hash 完整 |
| Model call | input/prompt/raw 路径与 hash 一致；调用、重试、fallback 真实 |
| Raw | 原样保留、可解码、alpha 契约成立 |
| Runtime | 仅整图缩放，可从 Raw 逐像素重建 |
| Unity Import | GUID 和实际 importer 设置与 manifest 一致 |
| Binding | mapId、地图版本、主题和目标 Texture2D 精确匹配 |
| Gameplay | fingerprint、路线、碰撞、种植和模拟不变 |
| Play Mode | 目标纹理的 Runtime draw hit 已记录 |
| Visual | 由人工审核，不由尺寸或 hash 替代 |

## 11. 诊断顺序

| 现象 | 优先检查 |
|---|---|
| 仍显示旧地貌 | Runtime draw hit、主题解析、绑定是否 enabled |
| 静态绑定通过但画面不变 | 命中的纹理名、GUID、程序集和 Unity 进程 |
| 图片与交互格错位 | 投影画布、宽高比、grid size、cellPixels；不要改碰撞迁就图片 |
| AI 移动道路或边界 | 人工拒绝候选，新建 sibling run |
| Raw 尺寸不同 | 保留 Raw，按 profile 整图归一化 |
| 模型调用失败 | 保留失败，不得回填粗图或旧图 |
| 整图覆盖了交互提示 | 调整表现层绘制顺序，不改变玩法判定 |

## 12. 回滚

首选非破坏性回滚：

1. 禁用当前 mapId 的整图绑定；
2. 保留 Rough、prompt、model-call、Raw、Runtime、manifest 和审核记录；
3. 重新加载资产并进入 Play Mode；
4. 确认命中正式 TileSet fallback；
5. 重跑地图、战斗和发布前 smoke。

回滚不得修改玩法地图、路线、关卡目录或碰撞数据。

## 13. 实现门禁

要把本契约变成可用生产工具，独立变更至少需要实现：

- mapId、grid size、cellPixels、输出目录、候选名和 palette 的 profile；
- 从正式地图生成 Rough 和 gameplay fingerprint；
- Raw → Runtime 共用归一化及独立验证；
- candidate/integration manifest schema；
- 精确 mapId/palette/Texture2D 绑定与启用/禁用；
- direct-Battle 本地模式边界；
- Play Mode 目标纹理命中测试；
- 无损回滚和正式 TileSet fallback；
- 人工视觉审核门禁。

这些能力完成前，整图候选只能作为 Evidence，不得写入发布 palette 或被宣称为当前运行时能力。
