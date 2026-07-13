# pot-expansion Specification

## Purpose
TBD - created by archiving change implement-equipment-expansion. Update Purpose after archive.
## Requirements
### Requirement: 正交相邻扩建
玩家 SHALL 能消耗花盆道具，把共享种植网格中与任一现有花盆上下左右相邻的合法候选地块永久转为花盆；相邻性 MUST 使用网格行列计算。

#### Scenario: 合法扩建
- **WHEN** 玩家选择花盆道具并点击与现有花盆曼哈顿距离为一的候选格
- **THEN** 道具数量减少且该网格单元立即成为对齐的可种植花盆

### Requirement: 非法扩建保护
系统 MUST 拒绝斜角、非相邻、道路、核心或已有花盆网格位置，并且不得消耗道具。

#### Scenario: 点击斜角候选格
- **WHEN** 玩家点击只与现有花盆斜角相邻的候选格
- **THEN** 不创建花盆、不消耗道具并显示无效反馈

### Requirement: 覆盖预览
系统 SHALL 在选择空花盆或扩建候选位置时展示各植物的大致射程，香蕉额外展示预计方向，向日葵不显示攻击圈。

#### Scenario: 预览空花盆
- **WHEN** 玩家选择一个空花盆并切换查看西瓜
- **THEN** 地图显示以该花盆为中心的四格范围预览
