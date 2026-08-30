## Why

Battlefield entities and board-target drag feedback are currently drawn after the transparent-center gameplay-stage frame without one shared clip/occlusion contract. At edge cells and fractional PC scale, otherwise valid logical rectangles can therefore overwrite the frame rail or emit pixels outside the stage even though draw and hit projection remain aligned.

## What Changes

- Add one presentation-owned battlefield viewport/rail containment contract shared by terrain, entities, combat effects, board drag feedback, and the gameplay-stage frame, with battlefield pixels meeting the frame opening instead of leaving an artificial inset gap.
- Hard-clip battlefield-owned pixels to the stage viewport and keep the protected gameplay-stage rail above battlefield content in the final composition.
- Keep the cross-region plant drag connector as an explicit transient overlay while preventing the plant ghost and board target treatment from overwriting the stage rail.
- Add deterministic geometry and final-pixel validation for edge cells, transient motion, full/inset portrait safe areas, and the 1280×720 fractional-scale PC matrix.
- Preserve the current Sunny Orchard visual standard, board/hit rectangles, drag legality, gameplay rules, persistence, scene flow, and platform adapters.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `runtime-ui-quality-standard`: Require battlefield-owned rendered pixels and board drag feedback to obey one stage containment and protected-rail layering contract.
- `webgl-visual-acceptance`: Require real-canvas evidence that edge-state battlefield content cannot contaminate the gameplay-stage frame or escape the stage across supported viewport projections.

## Impact

- Runtime presentation: `FruitDefenseGame.BattlefieldRendering`, `FruitDefenseGame.ControlsAndOverlays`, and shared IMGUI rendering helpers.
- Geometry/validation: `BattlefieldProjection`, Battle layout/editor smoke coverage, and WebGL image-analysis gates.
- No new dependency, serialized gameplay data, Canvas/uGUI migration, runtime art slot, simulation change, or platform support claim.
