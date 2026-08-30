## Why

Dense combat feedback can still visibly reselect among nearby candidates. The requested behavior is now to preserve every combat floating-text record without any runtime collision avoidance, so a label remains on its authored lane near its own target.

## What Changes

- Raise the total and ordinary combat floating-text pool limits to 9999.
- Remove runtime collision candidate selection and overlap scoring; retain each label's deterministic authored lane and semantic offset.
- Reverse the floating-text temporal pacing: it appears at full opacity, rises immediately, and fades evenly through its lifetime.
- Update runtime and WebGL acceptance checks so they validate the new capacity and no-avoidance placement contract without changing gameplay, persistence, map projection, or input behavior.

## Capabilities

### New Capabilities

- `combat-floating-text-capacity`: Defines the active floating-text capacity and deterministic no-avoidance placement.

### Modified Capabilities

<!-- None. This presentation-only behavior is not owned by an existing current capability spec. -->

## Impact

- Affects the combat presentation buffer, SDF floating-text overlay, focused smoke tests, and WebGL combat-feedback acceptance harness.
- Preserves the approved runtime UI visual system and BattleStage containment; no simulation state, saved state, or content data changes.
