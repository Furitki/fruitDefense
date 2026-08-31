---
id: dual-grid-tile-generation-pipeline
parent: design-kb-home
order: 55
status: active
---

# Dual-Grid 地图图块生成技术分支

本文是 [地图图块统一生产管线](map-tile-production-pipeline.md) 下的 Dual-Grid 技术分支，只负责拓扑输入、16 Mask、确定性派生、压力测试和 Unity 笔刷包。所有地图图块必须先遵守统一管线的风格候选、游戏内方格预演、两次人工门禁、资产隔离、单关接入和真实 WebGL 验收；本文不能单独授权生产或发布。

A、B 两条路线只在模型输入和 16 Mask 取得方式上不同；模型返图之后的命名、Runtime 下采样、图集、压力图、证据和机械 QA 共用同一套脚本。若本文与统一管线的阶段顺序、审核边界或发布门禁冲突，以统一管线为准。

本管线只生产版本化候选和审核证据。机械 QA 通过不等于美术通过、接缝安全或已经接入 Unity。人工审核和接缝门禁通过前，输出只能位于 `Builds/Evidence`，不得复制到生产 `Resources`、发布场景或可玩目录。

## 1. Dual-Grid 技术流程

下列流程产生的 Raw、Mask、图集和压力图在统一管线第二次人工门禁前都只是候选证据，不得因完成 Finalize 或 Validate 就提前提升为生产资源。

```text
复制并填写材质 profile
  -> Prepare 机械生成无网格拓扑和提示词
  -> 图像模型单次整图重绘，原样保存 Raw
  -> Finalize 按路线归一化、裁切或重建 Mask00..15
  -> 共用后处理生成 Review256、描述符指定的 Runtime64、数字序图集和卍字图
  -> 独立保留 Runtime32 作为固定尺寸压力图采样源
  -> 共用压力测试生成 1 张 1024×1024 紧凑总图
  -> 穷举横向 64 对、纵向 64 对合法邻接并记录真实 mismatch
  -> Validate 复核 hash、尺寸、alpha 和解码像素
  -> 人工视觉审核
  -> 审核通过后另行进入 Unity 导入/发布流程
```

模型调用是明确的桥接步骤，不在确定性脚本内部。脚本不得生成伪 Raw，也不得用旧图或程序图替代失败的模型结果。

## 2. 两条入口路线

| 项目 | A：材质母图路线 | B：拓扑外圈路线 |
|---|---|---|
| 适合 | 需要从一块完整材质样本提取表面和交界 | 需要整张控制复杂风车头、上下文和裁切 |
| Prepare 输出 | 1024×1024 无网格拓扑 | 1536×1536 无网格 6×6 overscan 拓扑 |
| 结构 | 中央连续 3×3 landform，四边半格 base 边距 | 7×7 顶点格生成 6×6 frame，中央 4×4 是 canonical 卍字 |
| 模型动作 | 把整块母图重绘为两种材质 | 把整张 overscan 拓扑重绘为两种材质 |
| Finalize | 归一化为 1024；采集 landform、base、四向交界带；使用机器拓扑权威图重建 Mask00..15 | 归一化为 1536；固定裁 `x=256,y=256,w=1024,h=1024`；按 canonical 布局切 Mask00..15 |
| 拓扑归属 | `SquareContourTopologyGuide.png` 最终拥有 16 Mask 拓扑 | 6×6 overscan 和固定中央裁区拥有 frame 分布；模型仍需人工检查内部轮廓漂移 |

### 2.1 A 路线固定拓扑

左上原点，NW=`1`、NE=`2`、SE=`4`、SW=`8`：

```text
04 12 12 08
06 15 15 09
06 15 15 09
02 03 03 01
```

它表达一块完整草皮式母图：中央连续 3×3 landform，四周各有半格 base 边距。它不是最终 16 Mask 图集；Finalize 必须从母图提取材质，再结合机器拓扑权威图重建完整 `Mask-00..15`。

### 2.2 B 路线固定拓扑

7×7 顶点占用：

```text
0000000
0001000
0101110
0011100
0000100
0001000
0000000
```

生成的 6×6 mask：

```text
00 00 04 08 00 00
04 08 06 13 12 08
02 05 14 15 11 01
00 02 03 07 09 00
00 00 04 10 01 00
00 00 02 01 00 00
```

固定裁取中央行列 `1..4` 后得到 canonical 卍字：

```text
08 06 13 12
05 14 15 11
02 03 07 09
00 04 10 01
```

上端和右端的“头”必须来自 6×6 外圈中的完整上下文，不允许只画 4×4 后由模型猜测。

## 3. 稳定入口

| 职责 | 文件 |
|---|---|
| PowerShell 入口 | `scripts/dual-grid-tile-pipeline.ps1` |
| 确定性图像核心 | `scripts/dual_grid_tile_pipeline.py` |
| A 草土样例配置 | `scripts/dual-grid-profiles/A-grass-soil.json` |
| B 石水样例配置 | `scripts/dual-grid-profiles/B-stone-water.json` |
| A 机器拓扑权威图 | `Assets/LayeredTerrain/GrassSoil/Square/Topology/SquareContourTopologyGuide.png` |

运行环境需要 PowerShell、Python 3 和 Pillow 10–12。若缺少 Pillow：

```powershell
python -m pip install "Pillow>=10,<13"
```

## 4. 新建一套地图图块

先复制最接近的 profile，修改以下字段：

- `id`：候选的稳定标识；
- `route`：`A` 或 `B`；
- `landformLabel`、`baseLabel`：两种材质名称；
- `stylePrompt`：只描述材质和风格，不改结构契约；
- A 路线可调整 `boundaryBandWidth`，默认 12；除非拓扑规格升级，不改尺寸和 mask 常量。
- `candidateMode`：`route-default`、`pure-model` 或 `protected-hybrid`；
- `protectedReviewWidth`、`crossoverWidth`：保护边缘分支的可信外框和交叉带宽度。
- `unityBrush`：Unity 一键导入所需的稳定笔刷 ID、目录名、显示名、前景/背景 Surface、轮廓、边缘样式和端点 Mask。新增笔刷只改 profile，不改 Unity 导入器。

每轮使用新的版本化输出目录：

```powershell
$RunRoot = "Builds/Evidence/dual-grid-runs/<date>-<candidate>"
```

### 4.1 Prepare

A 示例：

```powershell
./scripts/dual-grid-tile-pipeline.ps1 `
  -Stage Prepare `
  -Profile scripts/dual-grid-profiles/A-grass-soil.json `
  -OutputRoot "$RunRoot/A-grass-soil"
```

B 示例：

```powershell
./scripts/dual-grid-tile-pipeline.ps1 `
  -Stage Prepare `
  -Profile scripts/dual-grid-profiles/B-stone-water.json `
  -OutputRoot "$RunRoot/B-stone-water"
```

Prepare 生成：

```text
source/Source-A-PatchTopology-1024.png
或 source/Source-B-OverscanTopology-1536.png
source/topology.json
request/pipeline-prompt.txt
```

拓扑图必须完全不透明，只含两种 semantic 颜色，不带网格、标签、边框、gutter 或文字。

### 4.2 模型桥接

把 `source/*.png` 作为精确结构输入，把 `request/pipeline-prompt.txt` 作为提示词基础。可增加材质参考图，但不得删除提示词中的结构约束。真实返回原样保存，不要提前裁剪或修补。

模型阶段必须写入 `request/model-call.json`，至少记录：

```json
{
  "tool": "<实际工具>",
  "toolMode": "<实际模式>",
  "useCase": "precise-object-edit",
  "inputPath": "<本 run 的 source topology>",
  "inputSha256": "<sha256>",
  "promptPath": "<本 run 的 pipeline-prompt.txt>",
  "promptSha256AtCall": "<sha256>",
  "callCountExecuted": 1,
  "retryCount": 0,
  "fallbackUsed": false,
  "rawPath": "<真实 Raw.png>",
  "rawSha256": "<sha256>",
  "executedAt": "<UTC ISO-8601>",
  "visualInspectionPerformed": false
}
```

Finalize 会强制核对 input、prompt 和 raw 的路径与 hash，并要求一次调用、零重试、无 fallback。历史回归可以显式使用 `-AllowMissingModelCall`，manifest 会记录 `legacy-regression-explicitly-allowed`；该开关不得用于新候选或生产候选。

模型失败、Raw 不可解码、不是正方形或存在透明像素时必须保存失败证据并停止。不得用拓扑输入、旧候选或脚本生成图冒充 Raw。

### 4.3 Finalize

A 示例：

```powershell
./scripts/dual-grid-tile-pipeline.ps1 `
  -Stage Finalize `
  -Profile scripts/dual-grid-profiles/A-grass-soil.json `
  -OutputRoot "$RunRoot/A-grass-soil" `
  -RawImage "<真实模型 Raw.png>" `
  -ModelCall "$RunRoot/A-grass-soil/request/model-call.json"
```

B 示例：

```powershell
./scripts/dual-grid-tile-pipeline.ps1 `
  -Stage Finalize `
  -Profile scripts/dual-grid-profiles/B-stone-water.json `
  -OutputRoot "$RunRoot/B-stone-water" `
  -RawImage "<真实模型 Raw.png>" `
  -ModelCall "$RunRoot/B-stone-water/request/model-call.json"
```

如果目标文件已存在但内容不同，脚本会拒绝覆盖。只有明确重建同一个 run 时才允许增加 `-Force`；常规迭代应新建版本目录。

Raw 归一化只能使用整图中心寻址 Point 公式：

```text
sourceX = min(Ws-1, floor(((2*x+1)*Ws)/(2*Wt)))
sourceY = min(Hs-1, floor(((2*y+1)*Hs)/(2*Ht)))
normalized[x,y] = raw[sourceX,sourceY]
```

禁止单格对齐、裁边、补边、bilinear/bicubic、抗锯齿、调色、锐化、alpha 修补或基于画面内容移动边界。Validate 会从 Raw 独立重算 normalized 并逐像素比较。

### 4.4 Validate

```powershell
./scripts/dual-grid-tile-pipeline.ps1 `
  -Stage Validate `
  -OutputRoot "$RunRoot/A-grass-soil"
```

Validate 会按 manifest 重新检查所有 PNG 的文件 hash、解码 RGBA hash、尺寸和 alpha。任何缺图或像素变化都会失败。

### 4.5 Package：生成 Unity 一键导入契约

Finalize 会自动生成 `candidate/BrushImport.json`。历史候选需要补包而不重做图像时，运行：

```powershell
./scripts/dual-grid-tile-pipeline.ps1 `
  -Stage Package `
  -Profile scripts/dual-grid-profiles/<profile>.json `
  -OutputRoot "$RunRoot/<candidate>"
```

`BrushImport.json` 是 Unity 注册语义的唯一输入，包含 `brushId`、`assetFolderName`、显示名、前景/背景 Surface、`contourStyleId`、`edgeStyleId`、前后景端点 Mask、`runtimeTileSize` 和 Runtime 相对路径。Package 会核对 profile、管线 manifest 和完整 16 Mask，不重新生成或修改任何图片。

### 4.6 Repackage：从已验收 Review 提升运行时清晰度

不重新调用模型、不改 Review256、拓扑或压力图，只升级运行时采样时，使用新的版本化输出目录：

```powershell
./scripts/dual-grid-tile-pipeline.ps1 `
  -Stage Repackage `
  -Profile scripts/dual-grid-profiles/<profile>.json `
  -SourceRoot "$AcceptedRun/<candidate>" `
  -OutputRoot "$NewRun/<candidate>"
```

Repackage 会先完整 Validate 来源 run，再复制其证据链；随后按 profile 从原封不动的 Review256 生成 Runtime64、更新 manifest/BrushImport 并再次 Validate。输出目录必须是不存在的新目录，禁止覆盖历史验收 run。

此外会独立重算并比较：

- Raw → normalized；
- B 的中央固定裁区；
- A 的 normalized → Main Board；
- Review → Runtime 中心采样；
- 数字序图集和 canonical 卍字图；
- 单张压力总图及其中四个固定分区；
- Review 与 Runtime 的 64+64 合法邻接数据；
- 每个 Mask 的像素所有权合计与 fallback 数量。

## 5. 共用确定性产物

```text
<RunRoot>/
  source/
    Source-*.png
    topology.json
  request/
    pipeline-prompt.txt
    model-call.json
  model/
    Raw.png
    Normalized-*.png
    CentralCrop-1024.png       # B
    Samples/                   # A
  candidate/
    BrushImport.json
    Main-Board-1024.png        # A
    Review256/Mask-00..15.png
    Runtime64/Mask-00..15.png  # Unity 正式运行资源，profile/描述符控制尺寸
    Runtime32/Mask-00..15.png  # 仅供固定 1024 压力图机械重建
    ReviewAtlas-1024.png
    RuntimeAtlas-256.png
    RuntimeAtlas-Upscaled1024.png
    SwastikaLayout-1024.png
    Stress-All-1024.png
    manifest.json
  evidence/
    ready.json
```

Runtime64 使用 Review256 的确定性 Lanczos 缩小；固定压力图仍使用中心采样：

```text
Unity 底部原点：Stress32[x,y] = Review[8*x+4, 8*y+4]
PNG 顶部原点：  Stress32[x,y] = Review[8*x+4, 8*y+3]
```

## 6. 候选像素分支

### 6.1 Route Default

A 默认使用母图材质采样、四向交界带和机器拓扑 alpha 重建完整 Mask00..15。B 默认将中央 4×4 纯返图按 canonical frame 切片。每个 Mask 都记录：

```text
routeDerivedPixels
historicalPixels
fallbackPixels
totalPixels
```

任一 Mask 的三类来源合计必须为 65,536，`fallbackPixels` 必须为零。

### 6.2 Pure Model

纯返图分支不回填历史像素：

```text
routeDerivedPixels = 65536
historicalPixels = 0
fallbackPixels = 0
```

纯返图不等于无缝。真实 mismatch 必须保留，`seamSafetyClaimed=false`。

### 6.3 Protected Hybrid

当已有经过机械验证的可信 `Mask-00..15` socket 时，可选择 `candidateMode=protected-hybrid`，并在 Finalize 传入：

```powershell
-TrustedMaskRoot "<可信 Review256 目录>"
```

默认参数：外围 32px 完全来自可信历史 Mask，接下来的 16px 使用固定 8×8 Bayer 阈值在可信像素和 route-derived 像素之间选择完整 RGBA，中心来自 route-derived 候选。流程不做通道混合。

```text
edgeDistance = min(x,255-x,y,255-y)

edgeDistance < 32:
  historical
32 <= edgeDistance < 48:
  threshold = ((15-(edgeDistance-32))*64)/16
  historical when Bayer8[y&7,x&7] < threshold, otherwise route-derived
edgeDistance >= 48:
  route-derived
```

可信 Mask 必须为完整 16 张、256×256、全不透明；脚本逐像素记录所有权。ProtectedHybrid 只保护 socket，不替代人工检查内部拓扑和风格。

## 7. 单图全验压力测试

四个压力场分别由 17×17 逻辑顶点场生成 16×16 个 Runtime tile。每个 Runtime tile 始终保持原生 32×32 像素，所以每个场景为 512×512：

1. 纯 landform 重复；
2. landform 中央 base 洞；
3. base 中央 landform 岛；
4. 对角混合、棋盘和十字叠加。

逻辑顶点函数固定为：

```text
pureLandform(x,y) = true

landformWithCentralBaseHole(x,y) =
  x < 5 or x > 11 or y < 5 or y > 11

baseWithCentralLandformIsland(x,y) =
  5 <= x <= 11 and 5 <= y <= 11

checker = (((floor(x/2)+floor(y/2)) & 1) == 0)
cross = ((6 <= x <= 10) or (6 <= y <= 10))
diagonalMixed(x,y) = checker XOR cross
```

PNG 脚本使用左上原点的 17×17 逻辑顶点场。每个输出 tile 由四个逻辑顶点按 NW=`1`、NE=`2`、SE=`4`、SW=`8` 计算 Mask，再原样放置对应 Runtime32。四个场景合计必须覆盖全部 `Mask00..15`。修改阈值、XOR、尺寸或坐标原点即形成新测试版本，不得沿用旧 manifest。

Finalize 不再输出四个独立文件，而是组装一张 `Stress-All-1024.png`。固定分区如下，坐标均为 PNG 左上原点的 `[x,y,width,height]`：

| 分区 | 场景 | 矩形 |
|---|---|---|
| 左上 | `pureLandform` | `[0,0,512,512]` |
| 右上 | `landformWithCentralBaseHole` | `[512,0,512,512]` |
| 左下 | `baseWithCentralLandformIsland` | `[0,512,512,512]` |
| 右下 | `diagonalMixed` | `[512,512,512,512]` |

总图内部禁止缩放 Runtime tile，也禁止滤波、标签、边框、网格、间距或覆盖层，因此四个分区都能逐像素裁回确定性的 512 图。manifest 的 `stressAtlas` 是唯一图例，记录总图路径、尺寸、`tileGridSize=16×16`、`logicalVertexSize=17×17`、`runtimeTileSize=32×32`、布局版本，以及每个分区的 id、公式、矩形、解码 RGBA hash 和 alpha 状态。

Validate 先从 Runtime32 独立重建四个场景和完整总图，再同时执行：

- 1024×1024 整图逐像素比较，用于发现交换分区、额外像素或布局漂移；
- 四个固定分区分别裁切、逐像素比较并核对各自 hash，用于定位具体失败场景。

紧凑规格只把每个场景的重复密度从 32×32 降到 16×16，不缩放任何 Runtime tile。四场景总放置数由 4096 降为 1024，仍提供横向 960 次和纵向 960 次可见接缝；完整 socket 覆盖继续由独立的 64+64 穷举保证。

脚本同时穷举所有合法邻接：横向 64 对、纵向 64 对。记录：

- `legalPairCount`；
- `pairMismatchCount`；
- `pixelMismatchCount`。

`mismatch > 0` 必须保留真实数值。管线固定写入 `seamSafetyClaimed=false`，不得因为图集、尺寸或 alpha 通过就自动宣布无缝。

单张压力总图必须能由 Runtime32 和上述公式整图及逐分区重建。总图用于一次查看四个代表性场景；64+64 穷举邻接才覆盖全部 socket 数据，但二者都不能替代人工美术审核。

## 8. 自动门禁与失败处理

Finalize 前置门禁：

1. topology 与 prompt 存在且 hash 与调用记录一致；
2. Raw 来自真实模型调用、可解码、正方形、全不透明；
3. 调用记录为一次调用、零重试、无 fallback；
4. 输出目录与只读输入隔离；
5. A 的机器拓扑权威图存在且 hash 可记录；
6. ProtectedHybrid 的可信 16 Mask 完整且未被改写。

以下情况必须失败或保持“仅供人工审核”，不得自动修图：

- mask 排列重复、遗漏或 Y 轴双重翻转；
- Raw → normalized 无法按中心公式重建；
- Runtime、图集、压力总图或任一压力分区不能逐像素重建；
- ProtectedHybrid 的历史保护像素或所有权不符合契约；
- PureModel 出现 historical/fallback 像素；
- 源拓扑、prompt、model-call 或可信 Mask 在运行中改变；
- 任意声明 PNG 的 hash、尺寸或 alpha 与 manifest 不一致；
- seam mismatch 非零：保留数字、维持 `seamSafetyClaimed=false`，交给人工。

如果要求智能体不看图，`visualInspectionPerformed=false`；机械解码、hash、尺寸、alpha 和逐像素比较不算视觉检查。交付时仍需把主要过程图和单张压力总图直接交给人工。

## 9. 多候选总览

候选比较使用脚本的 `Overview` 阶段：

```powershell
./scripts/dual-grid-tile-pipeline.ps1 `
  -Stage Overview `
  -OutputRoot "$RunRoot" `
  -Columns 2 `
  -Candidate @(
    "A=$RunRoot/A/candidate/SwastikaLayout-1024.png",
    "B=$RunRoot/B/candidate/SwastikaLayout-1024.png"
  )
```

输出 `overview/All-Candidates-Swastika.png` 和 `overview/manifest.json`，记录 `panelIndex → candidateId → sourcePath → sourceSha256`。总览中的缩放和标签仅供人工横向比较，禁止作为 Runtime、切片或逐像素验收输入。

## 10. Unity 接入门禁

本管线在人工视觉批准前只生产 Evidence。获批后进入独立 Unity 接入步骤：

1. 将 16 张 Runtime 或批准的原生尺寸 Mask 导入版本化素材目录；
2. 固定 Sprite PPU、Filter、Wrap、Compression、MipMap 和 alpha 契约；
3. 创建或更新 16 槽 `DualGridTileSet`，Mask00..15 不得错位；
4. 验证每个 Tile 指向正确 Sprite/GUID；
5. 运行项目统一 `Fruit Defense/Validation/...` 门禁和实际地图 Play Mode；
6. 只有接缝、拓扑、人工视觉和运行时命中都通过，才允许进入发布清单。

静态文件存在、普通 WebGL 成功或压力总图可生成，都不能单独证明 TileSet 已实装或小游戏平台已适配。

## 11. 当前回归基线

单图压力回归根目录：

```text
Builds/Evidence/dual-grid-stress-atlas-16x16-regression-20260730/
```

使用现有 A/B 真实 Raw 回归得到：

| 路线 | PNG 检查数 | Review256 与原实验 | Runtime32 与原实验 | Validate |
|---|---:|---:|---:|---:|
| A 草＋土 | 47 | 16/16 解码像素一致 | 16/16 解码像素一致 | pass |
| B 石头＋水 | 41 | 16/16 解码像素一致 | 16/16 解码像素一致 | pass |

A 的新无网格拓扑位于：

```text
Builds/Evidence/dual-grid-stress-atlas-16x16-regression-20260730/
  A-grass-soil/source/Source-A-PatchTopology-1024.png
```

回归只证明确定性步骤可复现，未执行视觉检查，也不改变原 A/B 候选的 `seamSafetyClaimed=false` 状态。

当前 Runtime64 清晰度修正根目录：

```text
Builds/Evidence/dual-grid-runtime64-20260731/
```

两条路线的 Review256 16/16 与上述回归逐字节一致，`Stress-All-1024.png` 也逐字节一致；只新增描述符声明的 Runtime64 和对应 256 像素运行图集。机械验证通过不升级视觉检查或接缝安全声明。

## 12. 自动测试

仓库测试入口：

```powershell
python -m unittest scripts.tests.test_dual_grid_tile_pipeline -v
```

至少覆盖 A/B 静态拓扑、中心寻址归一化、ProtectedHybrid 完整像素来源、model-call 强制契约、16×16 压力密度、17×17 顶点公式、16 Mask 合集、总图固定布局、四区确定性像素和篡改失败。样本回归继续使用 `Prepare/Finalize/Validate` 检查全部派生图。

## 13. 禁止事项

- 不把带网格、标签或 gutter 的图交给模型；
- 不从模型返图猜测 mask 排列；
- 不把 A 母图的 16 个位置块冒充完整 Mask00..15；
- 不在 B 路线跳过 6×6 overscan 而直接生成 4×4；
- 不覆盖已审核或待审核 run；
- 不用脚本生成图冒充 AI Raw；
- 不把机械 QA 当作美术、拓扑或接缝验收；
- 不在人工批准前把候选写入生产资源。

## 14. 已授权候选的一键正式导入

候选通过机械门禁、并由用户明确要求实装后，使用 Unity 菜单 `Fruit Defense/地图工具/导入 Dual-Grid 笔刷包...`，选择 run 根目录或其中的 `candidate` 目录。通用导入器读取 `BrushImport.json`，只复制描述符指定的 Runtime Mask、原始 `manifest.json` 和导入描述，不会重新生成、补画、调色或视觉修复。

当前两个正式笔刷为：

| 笔刷 | 前景 / 底层 | 生产 TileSet | 纯材质端点 |
|---|---|---|---|
| A 草地 + 泥土 | `surface.grass` / `surface.soil` | `Assets/LayeredTerrain/CompositeBrushes/GrassSoil/GrassSoilCompositeTileSet.asset` | Mask15 草地；Mask00 泥土 |
| B 石头 + 水 | `surface.stone-road` / `surface.water` | `Assets/LayeredTerrain/CompositeBrushes/StoneWater/StoneWaterCompositeTileSet.asset` | Mask15 石头；Mask00 水面 |

导入器按 `runtimeTileSize` 配置 PPU；当前正式包为 64 PPU、居中 FullRect、Bilinear、无压缩、无 Mipmap。它重建 16 槽 `DualGridTileSet`，并创建或更新一个 `TerrainBrushDefinition`。描述文件声明的前后景端点使用 Repeat，其余合成 Mask 使用 Clamp。重复导入同一 `brushId` 会更新原资产、不新增重复入口；新分辨率成功绑定后，只移除该笔刷自己的旧 Runtime 分辨率目录。

`TerrainBrushDefinition` 是后续唯一注册表：`OrchardDefaultTerrainPalette` 从中合并底层端点与精确组合边缘；关卡地图编辑器和地貌素材实验室都按稳定 ID 顺序自动列出同一批笔刷，不再为草土、石水或后续组合新增硬编码按钮。地图编辑器按钮只选择语义组合，实际写格仍走原有区域工具、Undo、地图编译与发布门禁。

地貌实验室在同一个可滚动预览库中按稳定 ID 顺序同时显示全部已注册笔刷；每张卡直接用该定义的正式 16-Mask TileSet 拼出组合预览，并标明单向或双向能力，不需要先切换目标才能发现其他笔刷。卡片在标题栏上方预留居中的正方形画面区，只做等比缩放，不再把横纵方向拉成不同倍率。点卡片后，前景映射为 Material A、背景映射为 Material B，并从正式 Palette 解析可复用地貌；卡片下方的当前方向工具负责实际试画。为避免把旧 A/B 标记静默解释成另一组语义，非空实验画布仍完整显示预览库，但切换会被拒绝并提示先点击“清空实验画布”。石覆水只新增水面底层，不虚构透明水地貌，因此卡片标为单向并只开放“石头覆水”；没有真实水地貌时，“水覆石”保持禁用。

聚焦 Smoke 会复核：描述文件与 manifest 身份、16 张 Runtime64、64 PPU、导入参数、声明端点、旧 Runtime32 生产目录清理、注册表唯一性、Palette 精确绑定、地图编辑器与实验室共同枚举、统一预览库的全量稳定布局与正方形画面区、草土双向可用、石覆水单向规则以及非空画布拒绝。安装不把 `seamSafetyClaimed=false` 改成 true；本轮按要求不执行智能体视觉检查，视觉结论仍由人工负责。普通笔刷编辑不再默认触发 WebGL 构建；只有进入发布门禁时才运行发布构建。
