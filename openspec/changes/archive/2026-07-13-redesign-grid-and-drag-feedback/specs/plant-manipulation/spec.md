## ADDED Requirements

### Requirement: Continuous drag feedback
The system SHALL show a lifted source, a plant preview following the pointer, and a distinct hovered destination throughout a plant drag on mouse and pointer/touch input.

#### Scenario: Dragging across the battlefield
- **WHEN** the player drags a nursery or field plant across several pots
- **THEN** the plant preview follows the pointer and the currently hovered pot alone receives the strongest target emphasis

### Requirement: Drop outcome feedback
The system SHALL communicate legal placement, legal movement, cancellation, and invalid-drop reasons before release and SHALL animate successful placement or return after release.

#### Scenario: Releasing outside a pot
- **WHEN** the player releases a dragged plant outside every legal pot
- **THEN** the plant remains at its source and receives cancellation/return feedback

## MODIFIED Requirements

### Requirement: 二合升级
两个同类型同星级植物 SHALL 合成为目标位置上一株高一星植物，最高四星；拖动期间 MUST 预览合成后的星级，成功后 MUST 显示升级反馈。

#### Scenario: 合成同类植物
- **WHEN** 玩家把一星豌豆拖到另一株一星豌豆上
- **THEN** 目标位置预览二星，释放后来源植物消失、目标位置保留一株二星豌豆并播放升级反馈

#### Scenario: 拖到不可合成植物
- **WHEN** 玩家把植物拖到不同类型、不同星级或满四星植物上
- **THEN** 目标显示不可合成状态和原因且释放后两株植物均保持原状

## REMOVED Requirements

### Requirement: 出售植物
**Reason**: Product direction no longer allows any fruit to be sold.

**Migration**: Remove all sell controls, drag destinations, commands, economy events, rewards, and copy; players must place, move, or merge plants instead.
