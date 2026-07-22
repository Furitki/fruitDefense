## Why

The portrait runtime duplicates session actions in a large bottom row, while the battlefield itself only shows a non-interactive prompt and the pause modal cannot restart the run. Moving these actions to their actual context frees space for the enlarged battlefield and makes wave and restart behavior easier to understand.

## What Changes

- Remove the entire bottom two-button row containing the persistent wave and restart actions.
- Replace the battlefield's right-side wave prompt with a real button labeled `开始波次` in the ready phase and `立即开始下一波` during the between-wave countdown.
- Hide the wave-start action while a wave is actively playing and show battle status instead.
- Move the normal-run restart action into the pause modal, which presents `继续游戏` and `重新开始` as two distinct buttons.
- Keep restart available from victory and defeat settlement modals.
- Reclaim the removed action-row height for the battlefield layout supplied by `restructure-battlefield-map-and-tiles`.
- Update portrait geometry checks and WebGL captures for ready, active-wave, between-wave, paused, and restarted states.

## Capabilities

### New Capabilities

- `battle-session-controls`: Contextual wave-start controls, pause actions, restart placement, and the removal of duplicated bottom actions.

### Modified Capabilities

None. The repository has no promoted baseline specifications for this behavior.

## Impact

- Portrait region definitions, board status controls, and modal actions in `Assets/Scripts/FruitDefenseGame.cs`.
- Existing `GameSimulation.StartWave`, pause, and reset entry points are reused; wave timing and combat rules remain unchanged.
- Portrait layout smoke and `scripts/accept-webgl-portrait.ps1` interaction coordinates/evidence require updates.
- The resized battlefield is coordinated with `restructure-battlefield-map-and-tiles`; loading and deployment behavior are unaffected.
