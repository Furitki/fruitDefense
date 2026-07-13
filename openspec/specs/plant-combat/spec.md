# plant-combat Specification

## Purpose
TBD - created by archiving change implement-zombie-battle. Update Purpose after archive.
## Requirements
### Requirement: 通用索敌
攻击植物 MUST 优先选择射程内剩余路径最短的僵尸，并以低生命和早生成顺序打破平局。

#### Scenario: 多目标索敌
- **WHEN** 多个僵尸同时位于植物射程内
- **THEN** 植物攻击离出口最近且符合平局规则的僵尸

### Requirement: 五种植物行为
系统 SHALL 实现豌豆单体弹道、西瓜落点范围伤害、香蕉直线往返穿透、榴莲近战范围伤害和向日葵周期生产。

#### Scenario: 攻击形态可区分
- **WHEN** 五种植物分别进入其触发条件
- **THEN** 每种植物按自身形态产生攻击或阳光且不会退化为相同的单体即时伤害

### Requirement: 星级成长
系统 SHALL 按规则将二至四星植物应用伤害、攻速、范围或向日葵产出成长，且不改变攻击形态。

#### Scenario: 合成后战斗属性
- **WHEN** 一株植物从一星提升为二星
- **THEN** 其数值使用二星倍率且原有攻击形态保持不变

### Requirement: 攻击范围反馈
系统 SHALL 在植物被选中或拖动时显示其真实攻击范围，香蕉额外显示预计方向；显示范围 MUST 与当前星级的运行时索敌范围一致。

#### Scenario: 选择植物
- **WHEN** 玩家选中一株攻击植物
- **THEN** 战场显示以花盆中心为原点且与当前星级实际索敌距离一致的半透明范围

#### Scenario: 拖动植物
- **WHEN** 玩家拖动一株攻击植物并悬停在合法空花盆上
- **THEN** 战场以目标花盆中心预览该植物放置后的真实攻击范围

### Requirement: Doubled attacking-fruit range
The base runtime range of pea, watermelon, banana, and durian SHALL be exactly twice its pre-change value, while sunflower range MUST remain zero.

#### Scenario: Reading one-star base ranges
- **WHEN** a one-star attacking fruit is created
- **THEN** pea and watermelon have range 44, banana has range 38, and durian has range 18
