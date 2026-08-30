## Why

Fruit dragging currently exposes only small legal/illegal badges and partially moves the drag ghost toward every overlapped target, so the source-to-destination relationship and the exact selected drop owner are difficult to read. The battle needs a clear transient connector and target-frame treatment while preserving the existing simulation-owned drop rules and shared projection.

## What Changes

- Show a dashed connector after a plant drag crosses the existing activation threshold, anchored at the plant's original board or nursery location.
- Keep the connector endpoint at the current drag preview when no legal destination is selected, and visually snap it to the authoritative target center when the current destination is legal.
- Draw the approved transparent-center nine-slice UI frame plus the existing legal, illegal, merge, or swap icon on the authoritative drop rectangle; illegal targets reject rather than snap.
- Remove the obsolete partial drag-ghost interpolation toward arbitrary overlapped targets so the ghost continues to follow the pointer consistently.
- Cover board-to-board, nursery-to-board, and board-to-nursery plant dragging without changing equipment installation or flowerpot-expansion dragging.
- Preserve the current runtime UI visual standard: this is a transient drag-state overlay, not a new action role or command; it uses an existing production nine-slice frame binding, the existing `drag-legal`, `drag-illegal`, `merge`, and `swap` icons, and semantic theme colors without drawing a primitive four-edge frame.
- Project the connector into device space before rotating its dash rectangles so PC letterboxing, fractional scale, and large viewport offsets cannot skew or displace the line.
- Keep gameplay legality, drop hit geometry, cooldowns, merge/swap behavior, persistence, and platform adapters unchanged.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `plant-selection-inspection`: Require an active plant drag to show a stable source connector and an authoritative legal-target snap frame while retaining drag-only relocation semantics.
- `runtime-ui-quality-standard`: Define the shared, non-color drag connector/target-frame presentation and its containment, state, and reduced-motion behavior.

## Impact

- Runtime interaction and presentation: `FruitDefenseGame.Interaction`, `FruitDefenseGame.ControlsAndOverlays`, `FruitDefenseGame.BattlefieldRendering`, and `BattleUiPresentationState`.
- Shared geometry and UI drawing: `DragGeometry` and `RuntimeUiGui`, including deterministic design-to-device connector projection.
- Editor validation and ordinary WebGL acceptance for free drag, legal drop, illegal drop, merge, swap, full safe area, and representative inset safe area.
- No new dependency, serialized gameplay data, runtime art slot, scene-flow change, or mini-game platform claim.
