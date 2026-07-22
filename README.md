# FruitDefense

FruitDefense 是一个使用 Unity 6 开发的竖屏水果塔防项目。玩家在大厅发起挑战，在战斗中刷新、种植、移动和合成水果，搭配装备抵御波次敌人，随后进入结算并选择返回大厅或重新挑战。

## 快速开始

1. 安装 Unity `6000.3.19f1`，并包含 WebGL Build Support。
2. 使用 Unity Hub 打开仓库根目录。
3. 打开 `Assets/Scenes/Bootstrap.unity`，按 Play 运行完整流程。
4. 冷启动应进入 Lobby；开始关卡后进入 Battle，胜负结束后进入 Settlement。

需要验证完整 P0 基线时，可在 Unity 菜单中运行 `Fruit Defense/Validate P0 Release Gate`。WebGL 构建与验收入口见 `Assets/Editor/WebBuild.cs` 和 `scripts/accept-webgl-portrait.ps1`。本地 Web/PC 构建及默认安全的线上发布入口见 `docs/build-and-release-pipelines.md`。

## 当前玩家流程

`Lobby → Battle → Settlement → Return / Retry`

- Lobby 当前默认进入 `orchard-01`；关卡选择、成长和设置区域仍是明确的未开放预留面。
- Battle 包含 15 波敌人、生命与阳光、波间倒计时、暂停和 1/2 倍速。
- Settlement 展示胜负、到达波次和剩余生命，并支持返回大厅或使用新会话重新挑战。

## 战斗操作

1. 刷新五株水果，将水果拖入花盆完成种植。
2. 在花盆之间移动水果，或合并同种同星水果进行升星。
3. 将装备拖到兼容水果上，将花盆拖到可扩建格增加种植位置。
4. 点击开始下一波；空格控制暂停，数字键 `1`/`2` 切换速度。
5. 点击操作保留为拖拽之外的辅助输入方式。

当前基础内容包括五种水果、四类敌人、三种装备、花盆扩建、波次奖励，以及由稳定内容 ID 驱动的技能组合。精确的发布状态和验收结果以基线文档为准。

## 文档入口

| 文档 | 职责 |
|---|---|
| [游戏策划总纲](docs/design/game-design-overview.md) | 游戏定位、核心循环、内容结构、版本方向和待定策划问题 |
| [P0 发布基线](docs/p0-release-baseline.md) | 当前 P0 运行形态、版本化产物和已验证门禁 |
| [P1 首波门禁](docs/p1-first-wave-gate.md) | 当前平台适配与 P1 后续工作的放行条件 |
| [平台验证](docs/platform/) | 抖音、微信的工具链、设备与兼容性证据 |
| [构建与发布管线](docs/build-and-release-pipelines.md) | 本地 Web/PC 构建、证据清单和线上 WebGL 发布门禁 |
| [OpenSpec 变更](openspec/changes/) | 单个需求的方案、设计、任务和验收契约 |

## 工程入口

| 路径 | 内容 |
|---|---|
| `Assets/Scenes/Bootstrap.unity` | 发布流程的启动场景 |
| `Assets/Scenes/Lobby.unity` | 大厅入口与未来系统预留面 |
| `Assets/Scenes/Battle.unity` | 战斗场景与当前表现层 |
| `Assets/Scenes/Settlement.unity` | 战斗结算、返回和重试 |
| `Assets/Scenes/DualGridDemo.unity` | 双网格瓦片开发演示，不进入发布场景列表 |
| `Assets/Scripts/Core` | 无场景依赖的规则、状态与配置 |
| `Assets/Scripts/Tilemaps` | 双网格逻辑掩码、瓦片配置与生成组件 |
| `Assets/Scripts/Shell` | 大厅与结算流程 |
| `Assets/Resources/Content` | 随包发布的版本化战斗内容 |
| `Assets/Editor` | 项目配置、内容导出、构建和验证入口 |

### Dual-Grid 瓦片制作

在 Unity 菜单运行 `Fruit Defense/Dual Grid/Create or Refresh Demo`，然后打开 `Assets/Scenes/DualGridDemo.unity`。选择 `Logical Paint - author here`，使用 Tile Palette 绘制任意逻辑占位瓦片；开启 `Automatic Refresh` 时，`Generated Visuals - do not edit` 会按周围四格自动选择 0–15 掩码瓦片。

高清卡通草地测试集由 `Assets/DualGridDemo/CartoonGrass/CartoonGrassBakeProfile.asset` 保存制作参数，通过 `Fruit Defense/Dual Grid/Generate Cartoon Grass Tile Set` 重新生成。烘焙器组合无缝草地与泥土纹理，以像素距离场分离外轮廓、裸露土层和草土过渡，并使用 4× 子像素积分、确定性草簇及断开的对角拓扑生成 16 张 512×512 Sprite、Tile、底层泥土 Tile 和 `CartoonGrassDualGridTileSet`。生成结果不得手工修改。

每次烘焙会验证 64 组横向与 64 组纵向合法邻接的 RGBA 边缘、`0101/1010` 中心断开和相同 Profile 的重复输出哈希，并将报告写到 `Builds/Evidence/cartoon-grass-dual-grid-seam-test.json`。演示右侧使用“泥土基底 + 透明 Dual-Grid 草地覆盖”分层，左侧掩码廊仍保留透明背景以便诊断 Alpha。

人工绘制检查可运行 `Fruit Defense/Dual Grid/Open Manual Paint Test`：场景视图左键拖动绘制逻辑草地，按住 `Shift` 再左键拖动可擦除，生成层会即时刷新。

最终美术通过 `Fruit Defense/Dual-Grid Tile Set` 创建 16 槽配置：`NW=1`、`NE=2`、`SE=4`、`SW=8`，其中 1–15 必填，透明叠加层可将 0 留空。地面、道路和墙体各使用一组独立的逻辑/输出 Tilemap 与组件；输出 Tilemap 完全由生成器管理，全量重建会清空其中的手工修改。完整契约见 [`introduce-dual-grid-tilemap-authoring`](openspec/changes/introduce-dual-grid-tilemap-authoring/)。

## 版本说明

项目固定使用 Unity `6000.3.19f1`。不要使用其他编辑器版本改写 `ProjectSettings/ProjectVersion.txt`；平台 SDK、构建哈希和兼容性状态不在 README 中维护，请查看对应门禁文档。
