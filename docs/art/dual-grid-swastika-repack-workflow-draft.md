---
id: dual-grid-swastika-repack-workflow-draft
parent: design-kb-home
order: 60
status: draft
---

# Dual-Grid 16-mask 卍字模板、AI 精修与压力验收流程（草稿）

> 状态：可复制实验流程草稿。本文记录如何把一套完整的 `Mask-00..15` 候选按指定模板重排成卍字型审核图、如何让图像模型只精修草/土交界、如何从真实返图派生保护边缘版与纯返图版，以及如何用四类压力图和穷举邻接数据验收。它不代表候选已经美术通过、无缝、拓扑正确、接入运行时或获得发布授权。

## 0. 快速复制入口

要在另一个 Dual-Grid 项目复用本流程，先复制并替换以下参数，不要复制 FruitDefense 的候选结论：

```text
PROJECT_ROOT=<项目根目录>
SOURCE_MASK_ROOT=<Mask-00.png..Mask-15.png 所在目录>
SOURCE_SWASTIKA_ATLAS=<1024x1024 卍字布局编辑目标>
CANDIDATE_ROOT=<新候选独立目录>
REVIEW_SIZE=256
RUNTIME_SIZE=32
GRID_SIZE=4
CANONICAL_ATLAS_SIZE=1024
PROTECTED_REVIEW_WIDTH=32      # 纯返图分支设为 0
CROSSOVER_WIDTH=16            # 纯返图分支设为 0
STRESS_TILE_COUNT=32
VISUAL_INSPECTION_PERFORMED=false
```

最小复制顺序：

```text
检查 16-mask 输入
  -> 按固定映射生成卍字编辑目标
  -> 保存 prompt/input hash
  -> 单次 AI precise-object-edit
  -> 原样保存 raw
  -> 整图 Point 归一化到 1024x1024
  -> 按反向查找表切成 16 个 Review256
  -> 选择“保护边缘”或“纯返图”分支
  -> Point 下采样成 Runtime32
  -> 生成数字图集、卍字图集和四张压力图
  -> 穷举 64+64 合法邻接并记录真实 mismatch
  -> 校验 hash/alpha/映射/受保护文件
  -> 把全部图片交给人工审核
```

复制流程时必须同时复制第 3 节的映射、第 7 节的模型调用契约、第 9 节的压力图公式和第 10 节的验收清单；只复制其中一部分会失去可复现性。

## 1. 目标与边界

本流程接收一套或多套按项目数字 mask 命名的 16 张方形图片，为每套候选生成一张卍字模板布局图，并可把其中一张布局图作为图像模型的精确编辑目标。模型返图随后可以进入“保护边缘”或“纯返图”两个互不覆盖的候选分支，并生成多候选总览与压力测试图。

确定性阶段只允许：

- 按固定查找表选择源 mask；
- 把源像素复制到目标格位；
- 对模型原始返图进行整图 Point/nearest-neighbor 尺寸归一化；
- 按记录的完整像素来源规则派生独立候选；
- 为纯展示用途生成带候选名的缩略总览；
- 生成确定性的压力图；
- 记录路径、尺寸、映射、来源所有权、邻接 mismatch 和 hash，并逐格验证输出像素。

确定性阶段不得：

- 在确定性转换阶段再次让图像模型重绘、补边或润色；
- 根据画面内容猜测 mask；
- 在未记录来源规则的情况下改变单张候选的拓扑、边缘、颜色或 alpha；
- 把总览缩略图当作运行时素材；
- 从拼图成功推断 TileSet 已经无缝、可用或可发布。

如果操作者要求智能体不要看图，智能体只能进行文件、尺寸、hash、映射和像素一致性检查，不得调用图片查看、截图、OCR 或视觉评分工具。最终图片直接交给人工审核。

## 2. 输入约定

### 2.1 项目 mask 位定义

当前 FruitDefense Dual-Grid 使用以下四角位：

| 角 | 位值 |
|---|---:|
| NW | 1 |
| NE | 2 |
| SE | 4 |
| SW | 8 |

因此数字 mask 范围为 `0..15`，输入文件名固定为：

```text
Mask-00.png
Mask-01.png
...
Mask-15.png
```

本文当前样例使用 256×256 的 `Review256` 图片。若输入尺寸不是 256×256，应先停止并明确新的输出契约；不得在重排步骤里静默缩放。

### 2.2 完整性前置检查

每套候选在进入重排前必须满足：

1. `Mask-00.png` 到 `Mask-15.png` 恰好各一张；
2. 16 张图片都能解码；
3. 16 张图片尺寸和像素格式一致；
4. 没有缺号、重号或额外文件被误当成 mask；
5. 输入文件保持原样，不被重排过程覆盖。

任一条件失败时，该候选不得出现在比较总览中。

## 3. 卍字模板映射

### 3.1 视觉格位

模板不是 `Mask-00..15` 的行优先排列。视觉上从上到下、每行从左到右的正确 mask 为：

| 视觉行 | 第 1 列 | 第 2 列 | 第 3 列 | 第 4 列 |
|---|---:|---:|---:|---:|
| 顶行 | 8 | 6 | 13 | 12 |
| 第 2 行 | 5 | 14 | 15 | 11 |
| 第 3 行 | 2 | 3 | 7 | 9 |
| 底行 | 0 | 4 | 10 | 1 |

可直接写成视觉行优先数组：

```text
[
  [ 8,  6, 13, 12],
  [ 5, 14, 15, 11],
  [ 2,  3,  7,  9],
  [ 0,  4, 10,  1]
]
```

这 16 个值必须构成 `0..15` 的严格排列。若出现重复或遗漏，应在写图前失败。

### 3.2 mask 到模板 frame 的反向查找表

如果实现以数字 mask 为循环变量，并需要查出它在模板中的视觉 frame，可使用：

```text
TemplateFrameByMask =
[12, 15, 8, 9, 13, 4, 1, 10, 0, 11, 14, 7, 3, 2, 5, 6]
```

数组索引是数字 mask，值是从视觉左上角开始、按行优先编号的 frame `0..15`。例如：

- mask `0` 位于 frame `12`，即视觉底行第 1 列；
- mask `8` 位于 frame `0`，即视觉顶行第 1 列；
- mask `15` 位于 frame `6`，即视觉第 2 行第 3 列。

视觉格位表与反向查找表表达的是同一个映射，实现只需选择一种作为唯一数据源，另一种应由程序推导或用于断言，避免维护两份可能漂移的常量。

## 4. 单候选确定性拼图

### 4.1 输出尺寸与坐标

对 256×256 输入，单候选输出固定为 1024×1024：

```text
outputWidth  = 4 * 256
outputHeight = 4 * 256
```

本文中的 `row` 是视觉自上而下的 `0..3`，`column` 是视觉自左而右的 `0..3`。对使用左上角原点的普通图像缓冲区：

```text
destinationX = column * 256
destinationY = row * 256
```

若实现使用 Unity/PNG 解码后的左下角原点坐标，必须显式换算：

```text
destinationYFromBottom = (3 - row) * 256
```

不要同时在 mask 映射和 Y 坐标上各做一次翻转。

### 4.2 像素复制算法

参考伪代码：

```text
layout = [
  [8, 6, 13, 12],
  [5, 14, 15, 11],
  [2, 3, 7, 9],
  [0, 4, 10, 1]
]

assert sort(flatten(layout)) == [0..15]
output = new RGBA8 image(1024, 1024)

for row in 0..3:
  for column in 0..3:
    mask = layout[row][column]
    source = decode_rgba8("Mask-{mask:00}.png")
    assert source.size == (256, 256)
    copy_pixels_exact(
      source,
      output,
      destinationX = column * 256,
      destinationY = row * 256)

encode_png(output)
```

`copy_pixels_exact` 的含义是复制解码后的 RGBA 字节，不调用缩放、插值、抗锯齿、颜色调整、重采样或透明度合成。PNG 重新编码后的文件 hash 通常会变化，因此验收比较的是解码像素，而不是要求输出文件包含源 PNG 的压缩字节片段。

### 4.3 推荐命名

单候选输出建议写入忽略的证据目录，不覆盖任何运行时资产：

```text
Builds/Evidence/swastika-repacked-comparison/
  01-<Candidate>-Swastika-1024.png
```

序号只控制人工比较顺序，不参与 mask 映射。候选名应稳定且可追溯到输入目录。

## 5. 可复制工作包

### 5.1 目录模板

每次 AI 精修使用独立根目录，源文件只读，候选之间不得互相覆盖：

```text
<WorkflowRoot>/
  source/
    Mask-00.png ... Mask-15.png
    Source-SwastikaLayout-1024.png
  request/
    prompt.json
    model-call.json
  model/
    Raw.png
    ModelOnly-Normalized-1024.png
  candidates/
    ProtectedHybrid/
      Review256/
      Runtime32/
      Stress1024/
      Tiles/
      manifest.json
    PureModel/
      Review256/
      Runtime32/
      Stress1024/
      Tiles/
      manifest.json
  evidence/
    generation.log
    ready.json
```

`Raw.png` 是模型返回的原始文件；`ModelOnly-Normalized-1024.png` 只能由 `Raw.png` 整图 Point 归一化得到。两个候选分支引用同一份 raw/normalized 文件，不复制成新的“模型输出”。

### 5.2 角色与不可变规则

| 资源 | 角色 | 是否允许覆盖 |
|---|---|---|
| 16 张源 mask | 结构与可选保护边缘来源 | 否 |
| 卍字布局图 | 模型精确编辑目标 | 否 |
| prompt/model-call | 调用契约与 provenance | 否；重试应建立新记录 |
| raw | 真实模型返回 | 否 |
| normalized | raw 的标准尺寸副本 | 可重建，不得局部修改 |
| ProtectedHybrid | 旧边缘与模型中心的明确合成 | 否；迭代建立 sibling |
| PureModel | 100% 模型返图裁片 | 否；不得回填旧像素 |
| stress/evidence | 验收产物 | 可重建，但必须与 manifest 一致 |

在生成前后分别计算所有只读源文件的 SHA-256。任何源 hash 变化都应使流程失败，而不是刷新 manifest 接受变化。

### 5.3 最小 manifest

不同实现可以增加字段，但不得省略以下可追溯信息：

```json
{
  "status": "ready-for-user-visual-review",
  "inputPath": "<SOURCE_SWASTIKA_ATLAS>",
  "inputSha256": "<sha256>",
  "promptPath": "<prompt.json>",
  "promptSha256": "<sha256>",
  "modelCallPath": "<model-call.json>",
  "modelTool": "<tool>",
  "modelCallCount": 1,
  "rawPath": "<Raw.png>",
  "rawSha256": "<sha256>",
  "normalizedPath": "<ModelOnly-Normalized-1024.png>",
  "normalizedSha256": "<sha256>",
  "templateFrameByMask": [12, 15, 8, 9, 13, 4, 1, 10, 0, 11, 14, 7, 3, 2, 5, 6],
  "modelOwnedPixelsPerMask": "<count>",
  "historicalOwnedPixelsPerMask": "<count>",
  "seamSafetyClaimed": false,
  "internalTopologyVisualReviewPending": true,
  "visualInspectionPerformed": false,
  "generatedPngFiles": [],
  "protectedPaths": []
}
```

## 6. AI 精修前置门禁

提交模型前必须完成以下检查：

1. 卍字编辑目标是正方形、可解码、全不透明；
2. 画布恰好包含 4×4 格，不带标签、外边距或总览标题；
3. 第 3 节映射构成 `0..15` 的严格排列；
4. 输入布局的每格与对应源 mask 解码像素完全一致；
5. 输入、prompt 和受保护文件的 SHA-256 已记录；
6. AI 输出目录与源目录不同；
7. 已决定本轮调用次数、失败策略和是否允许视觉检查。

若要求智能体不看图，输入图片可以作为已经由用户提供或此前已进入对话的精确编辑目标传给模型，但智能体不得额外打开、截图、OCR 或描述它。机械解码、尺寸、alpha、hash 和逐像素比较仍然允许。

## 7. 单次 AI 精修流程

### 7.1 可复制提示词模板

以下模板只要求模型精修交界，不授权重排、改拓扑或增加装饰：

```text
Use case: precise-object-edit
Asset type: Unity Dual-Grid pixel-art terrain atlas edit
Input image: Image 1 is the exact structure and edit target.

Primary request:
Refine only the grass/soil intersection edges inside the sixteen cells.
Create organic interlocking grass tufts and small soil notches while keeping
the existing cohesive pixel-art grass and soil material identity.

Structure invariants:
Preserve the exact square canvas, exact 4x4 swastika cell arrangement,
every mask silhouette and corner occupancy, every cell position and registration,
the existing palette/material identity, and full opacity.
Preserve the outer <PROTECTED_REVIEW_WIDTH> pixels of every
<REVIEW_SIZE>x<REVIEW_SIZE> cell exactly.

Editing boundary:
Make changes only to internal grass/soil transition artwork outside protected bands.
Do not alter uniform material regions except immediately along an internal transition.

Avoid:
No gutters, separators, labels, frames, margins, text, UI, props, objects,
reordered cells, rotated cells, mirrored cells, lighting shift, palette shift,
smooth gradients, blur, antialiasing, transparency, watermark, logo,
or extra decoration.

Output:
One square opaque atlas retaining the same 4x4 swastika template layout.
```

即使 prompt 要求模型保护外框，后处理也不能假定模型确实遵守；保护边缘分支必须从可信源逐像素恢复外框。纯返图分支则必须诚实保留模型实际返回的像素和接缝差异。

### 7.2 调用策略

推荐默认契约是一次调用、零自动重试、零静默 fallback：

```text
callCountExecuted = 1
retryCount = 0
fallbackUsed = false
inputRole = exact structure and edit target
```

调用前保存完整 prompt 和输入 hash；调用完成后先原样保存 raw，再进行任何尺寸转换。至少记录：

```text
executedAt
tool / toolMode / useCase
inputPath / inputSha256
promptPath / promptSha256AtCall
rawPath / rawSha256 / rawWidth / rawHeight
rawOpaque / nonOpaquePixelCount
visualInspectionPerformed
```

模型失败、文件不可解码、非正方形或存在非预期透明像素时，应保存失败记录并停止。不得用输入图、旧候选或脚本生成图冒充模型返图。

### 7.3 整图 Point 归一化

模型通常不严格返回 1024×1024。对任意正方形 raw `Ws×Hs`，使用中心寻址的 nearest-neighbor/Point 采样生成 `Wt×Ht = 1024×1024`：

```text
sourceX = min(Ws - 1, floor(((2*x + 1) * Ws) / (2*Wt)))
sourceY = min(Hs - 1, floor(((2*y + 1) * Hs) / (2*Ht)))
normalized[x, y] = raw[sourceX, sourceY]
```

归一化只改变采样尺寸，不是颜色归一化，也不允许：

- 裁边、补边或单格独立对齐；
- bilinear/bicubic、抗锯齿或模糊；
- 调色、锐化、alpha 修复或局部重绘；
- 根据画面内容移动分隔线。

归一化输出必须能由 raw 和上述公式逐像素重建。

## 8. 从模型返图派生两条候选分支

### 8.1 共同切片步骤

对数字 mask `m`：

```text
frame = TemplateFrameByMask[m]
column = frame % 4
visualRow = floor(frame / 4)
modelCrop = normalized[
  x = column*256 .. column*256+255,
  y = visualRow*256 .. visualRow*256+255
]
```

若实现使用左下角原点，使用 `rowFromBottom = 3 - visualRow`，不得在映射和 Y 坐标上重复翻转。

### 8.2 分支 A：保护边缘版

该分支用于保留已经机械验证过的 socket，同时让模型拥有内部交界。对每个 Review256 像素：

```text
edgeDistance = min(x, 255-x, y, 255-y)

if edgeDistance < 32:
    final = historicalReview
else if edgeDistance < 48:
    offset = edgeDistance - 32
    final = SelectWholePixel(historicalReview, modelCrop, x, y, offset)
else:
    final = modelCrop
```

参考实现的 16 像素 crossover 使用固定 8×8 Bayer 阈值表，只在两张图之间选择完整 RGBA 像素，不做通道混合：

```text
Bayer8 = [
   0,48,12,60, 3,51,15,63,
  32,16,44,28,35,19,47,31,
   8,56, 4,52,11,59, 7,55,
  40,24,36,20,43,27,39,23,
   2,50,14,62, 1,49,13,61,
  34,18,46,30,33,17,45,29,
  10,58, 6,54, 9,57, 5,53,
  42,26,38,22,41,25,37,21
]

threshold = (15 - offset) * 4
useHistorical = Bayer8[(y & 7)*8 + (x & 7)] < threshold
```

对 256×256、保护宽度 32、过渡宽度 16 的参考参数：

```text
exact protected historical pixels = 28,672
crossover pixels                  = 11,264
center model pixels               = 25,600
```

manifest 必须记录每个 mask 的 historical/model 所有权计数，并验证所有保护区像素与可信源完全相等。

### 8.3 分支 B：纯返图版

该分支不回填旧边缘：

```text
finalReview = modelCrop
modelOwnedPixelCount = 65,536
historicalOwnedPixelCount = 0
fallbackOwnedPixelCount = 0
protectedReviewWidth = 0
crossoverWidth = 0
```

每张 Review256 必须与对应 normalized crop 逐像素相等。纯返图所有权不等于无缝；该分支默认：

```text
seamSafetyClaimed = false
internalTopologyVisualReviewPending = true
```

自动验收必须记录真实 mismatch，不能因不通过而修边，也不能把 mismatch 隐藏为成功。

### 8.4 Runtime32 与图集

Review256 到 Runtime32 只允许中心 Point 采样，比例为 8：

```text
Runtime32[x,y] = Review256[8*x + 4, 8*y + 4]
```

实现必须明确图像缓冲区是上原点还是下原点。以上公式使用 Unity/左下原点；普通 PNG 左上原点验证时，Y 行需要对应翻转。

每个分支至少输出：

```text
Review256/Mask-00.png .. Mask-15.png
Runtime32/Mask-00.png .. Mask-15.png
ReviewAtlas-1024.png             # 数字 mask 顺序
RuntimeAtlas-128.png             # 数字 mask 顺序
RuntimeAtlas-Upscaled1024.png    # 8x Point
SwastikaLayout-1024.png          # 第 3 节视觉顺序
```

数字图集和卍字图集必须通过逐格像素重建验证，不能只检查尺寸。

## 9. 四类压力图

### 9.1 统一构造规则

压力图使用 32×32 个 Runtime32 tile，输出为 1024×1024。对 tile 坐标 `(tileX,tileY)`，先在逻辑角点上调用布尔函数 `grass(x,y)`，再按项目位定义得到 mask：

```text
mask = 0
if grass(tileX,   tileY+1): mask |= 1  # NW
if grass(tileX+1, tileY+1): mask |= 2  # NE
if grass(tileX+1, tileY  ): mask |= 4  # SE
if grass(tileX,   tileY  ): mask |= 8  # SW

place Runtime32[mask] at (tileX*32, tileY*32)
```

逻辑坐标使用左下原点，`tileX/tileY` 范围为 `0..31`，角点函数可能接收 `0..32`。

### 9.2 精确布尔公式

| 文件 | `grass(x,y)` | 主要覆盖 |
|---|---|---|
| `PureGrassRepeat-1024.png` | `true` | mask 15 的大面积重复与纹理周期 |
| `GrassWithCentralSoilHole-1024.png` | `x < 10 or x > 22 or y < 10 or y > 22` | 草地包围土洞、凹角和闭合内边缘 |
| `SoilWithCentralGrassIsland-1024.png` | `10 <= x <= 22 and 10 <= y <= 22` | 土壤包围草岛、凸角和闭合外边缘 |
| `DiagonalMixed-1024.png` | `checker XOR cross`，见下式 | 高频切换、斜向与混合 mask 暴露 |

`DiagonalMixed` 的完整公式：

```text
checker = (((floor(x/3) + floor(y/3)) & 1) == 0)
cross = ((12 <= x <= 20) or (12 <= y <= 20))
grass(x,y) = checker XOR cross
```

改变阈值、网格宽度或 XOR 规则就形成了不同测试，必须更新文件名或 manifest 规则，不能继续沿用相同证据标识。

### 9.3 压力图验收

四张压力图都必须满足：

1. 尺寸为 1024×1024、可解码、全不透明；
2. 能由 16 张 Runtime32、布尔公式和角位规则逐 tile 精确重建；
3. 输出 SHA-256、解码像素 hash 和生成规则写入 manifest/evidence；
4. 不包含标签、分隔线或人工标注；
5. 作为人工暴露问题的审核图，不单独承担“证明无缝”的职责。

压力图是有代表性的复杂场景；第 10 节的 64+64 穷举邻接才是完整的 socket 数据检查。两者都通过也只代表机械条件成立，美术质量仍由人工决定。

## 10. 自动验收契约

### 10.1 socket 与合法邻接

使用四角位构造两位 socket：

```text
top(mask)    = (NW, NE)
right(mask)  = (NE, SE)
bottom(mask) = (SW, SE)
left(mask)   = (NW, SW)
```

枚举所有 `0..15 × 0..15`：

- `right(leftMask) == left(rightMask)` 时是合法横向邻接；应恰好 64 对；
- `top(lowerMask) == bottom(upperMask)` 时是合法纵向邻接；应恰好 64 对。

纯返图候选比较真实接触边界一层：

```text
left[x=last,y]  vs right[x=0,y]
lower[x,y=last] vs upper[x,y=0]
```

分别在 Review256 和 Runtime32 记录：

```text
legalPairCount
pairMismatchCount
pixelMismatchCount
comparisonDepth
```

保护边缘候选可比较完整保护深度；若宣称 `seamSafetyClaimed=true`，所有合法对的 mismatch 必须为零。纯返图候选不得把 mismatch 作为生成失败而自动修复，应保留数据并固定 `seamSafetyClaimed=false` 等待人工判断。

### 10.2 必检项目

| 类别 | 通过条件 |
|---|---|
| 调用 provenance | prompt、tool、input、raw 的路径和 hash 一致 |
| raw | 存在、可解码、正方形、alpha 契约成立 |
| normalization | raw 可按第 7.3 节逐像素重建 normalized |
| mask 映射 | 查找表严格覆盖 `0..15`，每格来源正确 |
| 保护边缘版 | 外框与可信源相等，过渡像素来自两源之一，中心来自模型 |
| 纯返图版 | 16 个 crop 全等，65,536 模型像素、0 fallback |
| Runtime | 16 张均为 Review 的中心 Point 下采样 |
| 图集 | 数字、卍字和 Runtime-upscaled 均可逐格重建 |
| 压力图 | 四张均可按第 9 节公式逐 tile 重建 |
| alpha/hash | 所有声明文件可解码、alpha 与 manifest 一致、SHA-256 一致 |
| Tile/TileSet | 16 个 Tile 指向对应 Runtime sprite，独立且可发现 |
| 邻接 | 恰好横 64、纵 64 对；真实 mismatch 已记录 |
| 受保护文件 | scene、palette、源候选和其他声明路径 hash 未变 |
| 人工门禁 | `visualInspectionPerformed` 与实际操作一致 |

### 10.3 失败处理

以下情况必须停止或把候选标为仅供审核，不得自动“修好”：

- 模型 raw 缺失、不可解码、不是正方形或 alpha 契约失败；
- prompt/input/raw hash 与调用记录不一致；
- 映射存在重复、遗漏或 Y 轴双重翻转；
- pure 分支出现任何 historical/fallback 像素；
- protected 分支的保护像素不再等于可信源；
- Point、图集或压力图无法精确重建；
- 受保护文件发生变化；
- 纯返图出现 seam mismatch：保留结果、记录数字、`seamSafetyClaimed=false`，交给人工，而不是偷偷修边。

### 10.4 交付清单

一次完整交付至少包含：

```text
输入卍字图
prompt.json
model-call.json
模型 raw
1024x1024 model-only normalized
16 Review256
16 Runtime32
数字 Review/Runtime/Runtime-upscaled 图集
卍字布局图集
4 张压力图
manifest.json
ready evidence
Unity/项目 smoke 日志（若项目使用 Unity）
人工审核状态
```

若智能体被要求不看图，所有图片仍要直接交付给用户，不能只交路径或只交图集。

## 11. 多候选总览

当前八候选比较采用 4 列×2 行，顺序为：

1. GPT
2. A2
3. A3
4. B3
5. Plan A
6. Wang
7. Direct A
8. Direct B

每个面板使用对应的 1024×1024 卍字拼图，再为展示用途按 Point/nearest-neighbor 缩小，并在面板外添加候选名。当前总览文件约定为：

```text
Builds/Evidence/swastika-repacked-comparison/
  00-All-Candidates-Swastika-Overview-2048x1096.png
```

总览中的缩放和文字只服务于横向人工比较。逐像素验收必须针对各自的 `*-Swastika-1024.png`，不能针对总览缩略图。

候选不足八套时可以减少面板，但必须保持明确的候选顺序，并在输出记录中写出 `panelIndex → candidateId → sourceRoot → outputPath`。不得用占位图冒充缺失候选。

## 12. 重排与总览补充机械验证

每张单候选拼图至少执行以下检查：

| 检查 | 通过条件 |
|---|---|
| 映射排列 | 视觉格位恰好包含 `0..15` 各一次 |
| 输入完整性 | 16 张输入存在、可解码、尺寸一致 |
| 输出尺寸 | 1024×1024 |
| 像素一致性 | 每个目标 256×256 格与映射到的源 mask 解码像素逐字节相等 |
| alpha | 每格 alpha 与源图完全一致；流程不自行改写透明度 |
| 输入保护 | 16 张源文件的 SHA-256 在操作前后不变 |
| 输出隔离 | 未改写 TileSet、Tile、scene、terrain palette 或运行时绑定 |

建议为每次运行记录：

```text
candidateId
sourceRoot
layout
maskBitConvention
source file SHA-256 values
source decoded-pixel SHA-256 values
output path
output file SHA-256
output decoded-pixel SHA-256
cell equality result for all 16 frames
visualInspectionPerformed
```

如果本轮遵循“不看图”约束，`visualInspectionPerformed` 必须为 `false`；机械像素比较不算视觉检查。

## 13. 常见错误

| 错误 | 结果 | 防护 |
|---|---|---|
| 把 `00..15` 直接按 4×4 行优先摆放 | 不能形成目标模板 | 固定使用第 3 节查找表并断言排列 |
| 同时翻转模板行和图像 Y 轴 | 输出上下颠倒或恢复成错误顺序 | 明确区分视觉行与底部原点坐标 |
| 使用平滑缩放拼单候选图 | 改写源像素与边缘 | 单候选只允许无缩放像素复制 |
| 从总览裁图作为运行时输入 | 使用了已经缩小并加标签的展示图 | 运行时与审核始终回到源 mask 或 1024 单候选拼图 |
| 仅检查尺寸/hash | 无法发现 mask 放错格位 | 对 16 个目标格逐格做解码像素相等检查 |
| 拼图看起来成立就宣称无缝 | 混淆布局审核与邻接验收 | 无缝、拓扑和运行时接入保持独立门禁 |
| 没有保存模型 raw 就开始裁图 | 无法证明真实返图来源 | 先落盘 raw、prompt 和调用记录，再做归一化 |
| 用 bilinear 缩放模型返图 | 引入混色和不可逆边缘变化 | 整图只用中心寻址 Point 归一化 |
| 保护边缘版相信 prompt 而不回填可信像素 | 模型可能已经改变 socket | 外框逐像素从可信源恢复并验证 |
| 纯返图版回填少量旧边缘 | 不再是 100% 返图 | 要求每 mask 65,536 模型像素、0 fallback |
| 纯返图有 mismatch 就自动修边 | 隐藏模型的真实拼接问题 | 记录 mismatch，保持 `seamSafetyClaimed=false` |
| 只生成压力图、不做 64+64 穷举 | 有代表性场景不能覆盖全部合法邻接 | 压力图与穷举接缝检查同时保留 |
| 修改压力图阈值但沿用旧 manifest | 证据无法复现 | 把四个布尔公式和参数写入 manifest |
| 智能体做机械像素比较后写成“已看图” | 混淆视觉审核与数据验证 | `visualInspectionPerformed` 只反映实际视觉操作 |

## 14. 草稿退出条件

在以下事项完成前，本文件保持 `draft`：

- 将重排与总览逻辑保存为仓库内的可复用脚本，而不是一次性命令；
- 将模型调用记录、整图 Point 归一化、两条候选分支和压力图逻辑保存为仓库内可复用实现；
- 为视觉格位、反向查找表、Y 轴换算、16 格像素一致性、raw-to-normalized 和 Runtime Point 添加自动测试；
- 为四个压力函数、逐 tile mask 推导和 64+64 合法邻接添加自动测试；
- 定义稳定的运行 manifest schema；
- 在干净检出上用一份新的 raw 复现保护边缘版、纯返图版、四张压力图和全部 evidence；
- 明确总览面板、标签和候选排序的可配置规则；
- 明确不同项目的 tile 尺寸、角位约定、坐标原点和保护宽度如何参数化；
- 证明失败分支不会用旧图、输入图或脚本图冒充 AI 返图；
- 将具体候选的任务、验收证据和人工结论继续放在对应 OpenSpec change/evidence 中，而不是写进本通用流程；
- 经人工确认该映射确实是后续美术审核要保留的模板，而不是一次性比较布局。
