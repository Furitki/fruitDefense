## Context

The portrait layout reserves a 50-point `ActionRect` near the bottom for two persistent buttons: wave start and restart. The battlefield also draws a non-interactive prompt that tells the player to start the wave on the right, but no such button exists there. The pause overlay delegates to a one-action modal and therefore offers only continue. Reset cleanup is duplicated between the bottom action row and terminal modal path.

The new placement depends on the enlarged battlefield rectangle from `restructure-battlefield-map-and-tiles`, but it reuses existing simulation operations: `StartWave`, `TogglePause`, and `Reset`.

## Goals / Non-Goals

**Goals:**

- Remove both persistent bottom session buttons and the obsolete action-row region.
- Put the wave-start action inside the battlefield on the right with phase-specific copy.
- Offer continue and restart together inside the pause modal.
- Keep victory/defeat restart behavior and keyboard pause behavior.
- Reclaim vertical space for gameplay while preserving safe-area and touch-target requirements.
- Centralize reset cleanup and verify every visible control state in WebGL.

**Non-Goals:**

- Changing between-wave duration, automatic next-wave behavior, wave definitions, speed rules, or pause simulation semantics.
- Redesigning the header, build economy, victory, or defeat content beyond required placement coordination.
- Adding restart confirmation or save/resume behavior.

## Decisions

### Derive the battlefield wave action from phase

Add a shared `WaveActionRect` (or equivalent layout helper) within the battlefield's lower-right safe content region. Draw and hit-test use the same rectangle.

| Phase | Battlefield action |
| --- | --- |
| Ready | Visible button: `开始波次` |
| Playing | No start button; show active wave/enemy status |
| BetweenWaves | Visible button: `立即开始下一波` while automatic countdown continues |
| Victory / Defeat | No battlefield start button |

The button calls the existing `StartWave` operation. The current centered non-interactive start prompt and the entire `ActionRect` are removed. Keeping a disabled playing-state start button was rejected because it occupies valuable board space without offering an action.

### Use a two-action pause modal

The paused, non-terminal state renders two independent touch targets: `继续游戏` invokes the existing pause toggle, and `重新开始` invokes the centralized reset path. The modal layout supports one or two actions so terminal victory/defeat modals can retain their existing single restart action.

Restart is immediate because the user requested two direct pause actions and the current game has no saved run requiring a destructive-data warning. Adding a nested confirmation was rejected as unnecessary friction for the current scope.

### Centralize full run reset cleanup

Create one presentation-level restart helper that calls `GameSimulation.Reset` and clears inspected plant state, selected tools, drag session, nursery reward timing, pulses, and transient status needed to prevent stale UI after reset. Pause restart and victory/defeat restart call the same helper.

This removes the duplicated reset branch formerly embedded in `DrawControls` and avoids leaving the reset game paused or carrying a stale selection.

### Reflow portrait regions after removing the action row

Delete `ActionRect` from the portrait region list and geometry validation. The map/layout change claims the freed 50 logical points plus any contextual-detail savings for the larger battlefield. The status surface remains available but must not recreate a second persistent wave/restart row.

All battlefield buttons remain at least 44 logical points on their shortest interactive dimension and inside the safe area. Header pause and speed controls keep their existing placement unless minor reflow is required for the resized board.

### Extend state-based acceptance

Update the WebGL acceptance script to derive or maintain stable coordinates for the new board action and both pause actions. Required evidence covers ready, active wave, between-wave countdown with early-start button, paused, continued, and restarted state. Acceptance asserts that the old bottom buttons are absent.

## Risks / Trade-offs

- [Wave action overlaps enlarged map entities] -> Reserve a projection-owned lower-right control inset and validate it against route/cell bounds.
- [Removing the persistent restart makes it harder to discover] -> Keep the header pause action persistent and label both modal actions explicitly.
- [Pause restart leaves transient presentation state] -> Route every restart surface through the same cleanup helper and assert a clean ready state.
- [Acceptance coordinates become stale after map resizing] -> Base them on named layout regions where possible and verify expected screen state after every click.

## Migration Plan

1. Add state/geometry checks for the desired ready, playing, between-wave, paused, and reset states.
2. Add the battlefield wave-action helper and replace the non-interactive prompt.
3. Generalize the modal action layout and add continue/restart to the paused state.
4. Centralize restart cleanup and route pause plus terminal restart through it.
5. Remove `ActionRect`, reflow regions with the map change, and remove old bottom-button validation/acceptance coordinates.
6. Build WebGL and capture every required state at 402 by 874 before publication.

Rollback restores the bottom action row and one-action pause modal together; gameplay state requires no migration.

## Open Questions

None. Button labels and phase behavior are fixed by the requested player flow.
