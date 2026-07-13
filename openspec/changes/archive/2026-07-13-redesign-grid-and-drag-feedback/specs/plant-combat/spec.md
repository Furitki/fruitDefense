## ADDED Requirements

### Requirement: Doubled attacking-fruit range
The base runtime range of pea, watermelon, banana, and durian SHALL be exactly twice its pre-change value, while sunflower range MUST remain zero.

#### Scenario: Reading one-star base ranges
- **WHEN** a one-star attacking fruit is created
- **THEN** pea and watermelon have range 44, banana has range 38, and durian has range 18

## MODIFIED Requirements

### Requirement: 攻击范围反馈
系统 SHALL 在植物被选中或拖动时显示其真实攻击范围，香蕉额外显示预计方向；显示范围 MUST 与当前星级的运行时索敌范围一致。

#### Scenario: 选择植物
- **WHEN** 玩家选中一株攻击植物
- **THEN** 战场显示以花盆中心为原点且与当前星级实际索敌距离一致的半透明范围

#### Scenario: 拖动植物
- **WHEN** 玩家拖动一株攻击植物并悬停在合法空花盆上
- **THEN** 战场以目标花盆中心预览该植物放置后的真实攻击范围
