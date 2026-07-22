## Context

`FruitDefenseGame` currently divides the 402-by-874 logical portrait canvas into a 60-point header, a 398-point battlefield, a separate 220-point build panel, a 92-point details region, and a 56-point operation-status region. The weapon/pot rectangles, nursery rectangles, refresh button, draw calls, and drag hit tests all depend on the separate `BuildRect`, while the bottom two regions reserve space even when no plant is selected and no transient message matters.

The active battlefield projection already owns the map grid and its wave-control strip. This change stays within IMGUI and presentation geometry so the simulation and drag/drop commands remain untouched.

## Goals / Non-Goals

**Goals:**

- Make the battlefield the dominant portrait surface and increase its reference dimensions.
- Dock the weapon/pot row, five nursery slots, and refresh action inside one continuous battle surface.
- Remove persistent bottom guidance and operation-status copy from the default screen.
- Preserve a compact selected-plant information card only when a plant is actually inspected.
- Remove persistent square backplates from flowerpot and fruit icons while making the art nearly fill its logical cell.
- Position map plants above their flowerpots through one per-plant content value instead of kind-specific rendering code.
- Keep every rendered control and its input/drag rectangle derived from the same helpers.
- Validate containment, non-overlap, touch sizes, safe-area scaling, and a real WebGL reference capture.

**Non-Goals:**

- Changing refresh cost, inventory counts, merge rules, expansion rules, combat balance, or wave flow.
- Replacing IMGUI with uGUI or UI Toolkit.
- Redesigning the header, pause/speed controls, art assets, or platform adapters.
- Updating the stable game-design overview for a presentation-only layout refinement.

## Decisions

### Compose one battle surface from explicit subregions

The area below the header becomes one continuous battle surface. At the 402-by-874 reference it contains an enlarged battlefield/map region followed by an embedded tool row, an embedded nursery row, the refresh action, and an optional compact detail card. The outer battle surface provides the visual container; the former standalone build and status panels are removed.

The battlefield region grows from 394-by-398 to the full logical width and about 500 logical points high. The shared `BattlefieldProjection` continues to receive only this map subregion so route, grid, pot, entity, range, and wave-control geometry remain isolated from the embedded trays.

Overlaying the trays directly on plantable cells was rejected because it would hide legal destinations and create ambiguous drag hit tests. Keeping the old separate build panel was rejected because it would not satisfy the requested embedded composition or release enough space from persistent bottom copy.

### Derive draw and interaction rectangles from the embedded trays

`WeaponToolRect`, `PotToolRect`, `NurseryRect`, and `RefreshRect` will derive from named embedded tray rectangles. Drawing, click handling, drag-source discovery, and nursery drop targeting continue to call those same helpers. All tool controls and nursery slots remain at least 44 logical points on their shortest interactive dimension.

### Remove persistent copy but keep contextual inspection

The default layout no longer calls a standalone status-panel renderer and no longer draws generic guidance inside the details region. When no plant is inspected, the contextual detail slot is visually empty. When a plant is inspected, a compact card shows its identity and essential combat values with a close action inside the battle surface.

Completely removing inspection details was rejected because plant identity, attack range, and equipment remain meaningful player information. Retaining generic help or an always-visible operation log was rejected because the user explicitly requested removal of the bottom text display.

### Use frameless, near-cell-size flowerpot and fruit art

Active map flowerpots and fruits no longer draw the opaque brown square and border beneath their atlas sprites. Occupied nursery slots and the flowerpot tool likewise omit their persistent rectangular backplates. Their sprites use a small inset from the logical cell so the art reads larger without overlapping adjacent hit targets.

Selection, valid drop targeting, return feedback, and drag state remain visible through temporary outlines. Empty nursery positions keep their text label, and the full logical cell remains the click or drag target even though the decorative backplate is removed. The flowerpot visual ratio increases from 0.675 to 0.88 of a map tile; the full tile remains the interaction target.

### Configure plant height per content definition

`PlantDefinitionDto.potVisualHeightOffset` is a non-negative logical-point value applied only when plant art is drawn over a map flowerpot. The shared renderer subtracts the configured value from the visual rectangle center: `0` preserves the current center and `1` moves the center upward by exactly one logical point. Nursery icons and drag ghosts remain centered because they are not composited over a flowerpot.

The bundled content configures the initial subtle lift per plant: pea `6`, watermelon `6`, banana `7`, durian `5`, and sunflower `5`. These values remain content data in the factory/authored catalog/JSON; the renderer contains no plant-kind height switch.

### Validate the composition as geometry before visual acceptance

`ValidatePortraitLayout` will assert that the enlarged battlefield and every embedded tray are contained by the battle surface, ordered without overlap, and provide touch-sized controls. Existing safe-area viewport checks remain. Unity smoke catches geometry and interaction regressions before a WebGL build and live 402-by-874 capture confirms the final hierarchy.

## Risks / Trade-offs

- [Frameless icons weaken state feedback] → Keep selection, drag target, and return feedback as transient outlines around the unchanged logical hit cells.
- [A height offset pushes art too far outside its cell] → Validate finite non-negative values, keep bundled offsets subtle, and verify the dense reference layout visually.

- [The wider/taller map still scales primarily from the eight-column grid width] → Use the full logical width, remove unnecessary outer margins, and assert the projected tile size does not regress.
- [Embedded controls visually blend into the map] → Use distinct but related tray fills inside one shared outer frame and retain labels for tools and nursery slots.
- [Removing the status panel hides command feedback] → Keep direct button states, target colors, return pulses, merge cues, and modal outcomes; do not reintroduce a persistent bottom log.
- [Compact inspection copy clips in Chinese] → Keep the card to essential one-line identity and one-line values, with bounded rectangles covered by reference-viewport validation.
- [Existing acceptance coordinates drift] → Update named acceptance states and coordinates from the new shared geometry before recording evidence.

## Migration Plan

1. Introduce the battle-surface and embedded-tray rectangles with validation assertions.
2. Repoint tool, nursery, refresh, and drag-source geometry to the new rectangles.
3. Replace the standalone build/status drawing sequence with embedded controls and conditional details.
4. Run Unity compilation and `FruitDefense.Editor.ProjectSetup.SmokeValidate`.
5. Rebuild WebGL and capture the reference portrait states.

Rollback restores the prior region constants and drawing sequence. No game-state or save-data migration is required.

## Open Questions

None.
