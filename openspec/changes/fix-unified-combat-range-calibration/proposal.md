## Why

普通关卡目前把遗留战斗距离按当前路线总长度换算，导致短路线的水果实际射程、投射物距离和范围圈同时缩短；同一植物在不同关卡的格数覆盖不一致。GM 已局部采用标准标尺，造成普通战场与 GM 走了两条实现路径。

## What Changes

- 以地图格距相对标准格距换算遗留战斗距离，删除当前路线总长度对该换算的影响。
- 让普通战场与 GM 通过同一个地图距离校准机制取得战斗射程、移速、投射物速度、命中半径与范围展示所需距离。
- 增加普通长/短路线与 GM 地图的回归测试，证明相同格距下相同遗留距离覆盖相同格数，并验证范围展示与目标判定仍共用数值。
- 不改变关卡内容、植物基础数值、星级/装备/状态修正、路线移动长度、UI 布局或范围圈视觉语义。

## Capabilities

### New Capabilities

- `combat-distance-calibration`: 所有战斗地图以统一的每格标尺换算遗留战斗距离，且不随路线总长度变化。

### Modified Capabilities

- `battlefield-map-layout`: 地图路线长度继续由路线节点推导，但不再参与战斗距离的单位校准。

## Impact

- `Assets/Scripts/Core/BattlefieldMap.cs` 的距离换算。
- `Assets/Scripts/Development/GmStress/GmStressBattleRuntime.cs` 的 GM 地图构造。
- 普通地图与 GM 的 Editor smoke 回归测试。
- 范围圈与详情仍使用既有战斗距离，不新增 UI 组件、动作或状态。
