## Why

Combat floating text currently reverses downward near the BattleStage upper edge, which makes ordinary damage and defeat feedback read as falling rather than rising. Dense feedback also exhausts its current collision-placement envelope too early, causing avoidable overlap.

## What Changes

- Remove the upper-edge inward-redirection branch: damage and defeat floating text always follows the upward motion direction, including when its target is near the top of BattleStage.
- Expand the bounded, deterministic dense-feedback collision envelope beyond the current 24 candidates / 140 logical-point vertical range, while keeping all labels inside BattleStage and retaining the horizontal anchor limit.
- Add focused editor regressions for the fixed upward direction and the enlarged dense-placement envelope.
- Preserve the current runtime UI visual standard: this affects transient Battle feedback only, not semantic action roles, content forms, interaction states, layout rectangles, or input behavior.
- Non-goals: simulation, persistence, combat damage, event positions, entity movement, BattleStage geometry, safe-area projection, WebGL platform scope, and ArtSet assets.

## Capabilities

### New Capabilities

- `combat-floating-text-motion`: Deterministic presentation-only rules for upward damage/defeat feedback and bounded collision separation on the shared BattleStage projection.

### Modified Capabilities

None.

## Impact

- `Assets/Scripts/Presentation/CombatFloatingTextSdfOverlay.cs`
- `Assets/Scripts/Presentation/CombatFloatingTextStyles.cs`
- `Assets/Editor/Tests/CombatFeedbackSdfRenderSmoke.cs`
- Battle's existing presentation buffer and shared battlefield projection remain the owners of event and position data.
