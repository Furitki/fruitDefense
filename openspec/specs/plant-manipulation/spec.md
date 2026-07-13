# plant-manipulation Specification

## Purpose
TBD - created by archiving change implement-plant-economy. Update Purpose after archive.
## Requirements
### Requirement: 种植与取消
玩家 SHALL 能将苗圃植物拖放或选择到空花盆；无效落点或返回苗圃 MUST 保留原状态。

#### Scenario: 放入空花盆
- **WHEN** 玩家把苗圃植物放到一个空花盆
- **THEN** 该植物离开苗圃并出现在目标花盆

### Requirement: 二合升级
两个同类型同星级植物 SHALL 合成为目标位置上一株高一星植物，最高四星；拖动期间 MUST 预览合成后的星级，成功后 MUST 显示升级反馈。

#### Scenario: 合成同类植物
- **WHEN** 玩家把一星豌豆拖到另一株一星豌豆上
- **THEN** 目标位置预览二星，释放后来源植物消失、目标位置保留一株二星豌豆并播放升级反馈

#### Scenario: 拖到不可合成植物
- **WHEN** 玩家把植物拖到不同类型、不同星级或满四星植物上
- **THEN** 目标显示不可合成状态和原因且释放后两株植物均保持原状

### Requirement: 场上移动
玩家 SHALL 能把植物移至空花盆；战斗中移动后该植物 MUST 在两秒内不能再次移动但仍可攻击。

#### Scenario: 战斗中移动
- **WHEN** 玩家把可移动植物放到另一个空花盆
- **THEN** 植物换位、重新索敌并进入两秒移动冷却

### Requirement: Continuous drag feedback
The system SHALL show a lifted source, a compact plant preview offset from the pointer, and a distinct hovered destination throughout a plant drag on mouse and pointer/touch input. The preview MUST NOT cover the pointer hotspot or the center of the hovered destination and MUST remain non-interactive for hit testing.

#### Scenario: Dragging across the battlefield
- **WHEN** the player drags a nursery or field plant across several pots
- **THEN** the offset preview follows the pointer while the currently hovered pot alone receives the strongest target emphasis and remains visually readable

#### Scenario: Dragging near a viewport edge
- **WHEN** the pointer approaches an edge where the default preview offset would move content off-screen
- **THEN** the preview offset flips or clamps to remain visible without covering the destination

### Requirement: Drop outcome feedback
The system SHALL communicate legal placement, legal movement, cancellation, and invalid-drop reasons before release and SHALL animate successful placement or return after release.

#### Scenario: Releasing outside a pot
- **WHEN** the player releases a dragged plant outside every legal pot
- **THEN** the plant remains at its source and receives cancellation/return feedback
