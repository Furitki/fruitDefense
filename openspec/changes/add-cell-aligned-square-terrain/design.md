## Context

The battlefield presentation already stores an opaque `BaseSurfaceId` per visual cell and optional Dual-Grid landform, contour, and pair-edge layers. A cell with only a base surface is therefore already representable and rendered as a square; the missing pieces are an explicit production authoring action, a guard against mixing two representations of the same material at a shared vertex, and an isolated art trial that lets the team judge the cleaner visual direction before changing release content.

The current grass and soil base textures are visually noisier and less cohesive than the approved clean, broad-brush reference. The trial must use newly generated, opaque, repeatable base textures while leaving the release palette, playable catalog, gameplay data, and scene flow unchanged.

## Goals / Non-Goals

**Goals:**

- Expose named grass-square and soil-square authoring presets in the canonical battlefield map workflow.
- Reuse the existing base-surface schema and renderer instead of creating square variants for all sixteen Dual-Grid masks.
- Reject maps that let the same surface touch itself across the base-only and Dual-Grid representations, including diagonal contact.
- Produce an isolated, reproducible comparison board with clean grass and soil square textures at gameplay cell scale.
- Verify texture import settings, repeat continuity, authoring semantics, and compiler diagnostics with focused editor automation.

**Non-Goals:**

- Replacing the release battlefield palette or rewriting existing playable maps.
- Removing the existing Dual-Grid system.
- Adding new gameplay, navigation, persistence, decoration, or map-data concepts.
- Treating a successful ordinary WebGL build as mini-game platform authorization.
- Creating a compatibility or migration layer for deprecated terrain representations.

## Decisions

### Reuse base-only visual cells as the square representation

Pure square terrain SHALL be stored as `BaseSurfaceId = <surface>` with empty landform, contour, and pair-edge identifiers. Rendering remains the existing opaque base layer, one texture-aligned quad per gameplay cell.

This is preferred over a second schema or a synthetic sixteen-mask square TileSet because the current model already expresses the desired geometry, keeps serialization and runtime ordering unchanged, and makes the authoring intent explicit without duplicating assets.

### Add explicit presets to the canonical map editor

The canonical map editor SHALL expose named grass-square and soil-square preset actions. Applying a preset updates the selected visual cell in one undoable operation: it sets the chosen base surface and clears landform, contour, and pair-edge fields.

The terrain laboratory remains a visual experiment surface and does not become the source of playable map data. A separate toggle that merely hides layers was rejected because it could preserve stale serialized identifiers and make the cell appear pure without actually being pure.

### Enforce one representation per touching surface

The visual-cell compiler SHALL compare each cell with forward horizontal, vertical, and diagonal neighbours. If the same surface is base-only on one cell and is the landform surface on the other, compilation fails with a focused `surface.shared-representation-mix` diagnostic.

Edge and vertex contact are both prohibited because Dual-Grid corner masks sample a shared 2x2 neighbourhood; diagonal mixing can therefore create the same false internal contour as edge mixing. Disconnected regions are valid, and different base-only surfaces may touch with an intentional hard cell boundary.

### Keep trial art and evidence isolated from release content

The generated grass and soil bitmaps, their prompt/provenance records, the trial palette, and the comparison board SHALL live under a dedicated trial directory. The board SHALL show pure grass and pure soil regions plus a spatially separated Dual-Grid example, and it SHALL render at the real battlefield cell scale.

The release palette and playable map catalog remain untouched. Promotion of a chosen direction is a later explicit change after visual review.

### Validate seamlessness as a production constraint

The trial base textures SHALL be opaque, imported with Repeat wrapping, and free of visible seams when tiled at least 3x3. Automated validation SHALL compare opposite border samples and reject excessive discontinuity; the generated comparison board provides the final visual check for broad patterns or repeated focal marks that a numeric edge test cannot detect.

## Risks / Trade-offs

- [Generated art can claim to be seamless without matching pixels exactly] → Run border-continuity validation, render a repeated board, and regenerate rather than hiding seams with runtime blending.
- [Hard boundaries between two base-only surfaces expose the grid] → Treat this as the intentional square-mode aesthetic; use Dual-Grid for organic transitions.
- [The new representation-mix rule can expose an existing invalid map] → Run the focused compiler smoke and aggregate project smoke before considering the change complete; fix the map rather than adding a fallback.
- [A trial palette can drift from the release palette bindings] → Clone only the required bindings deterministically and validate surface identifiers against the registered palette.

## Migration Plan

1. Add the isolated textures, prompt records, trial palette, and comparison-board generator.
2. Add canonical square preset actions and the compiler representation rule.
3. Run focused authoring/compiler/art validation, aggregate editor smoke, and generate the comparison evidence.
4. Keep the release palette and playable catalog unchanged until the team explicitly selects a direction.

Rollback is a branch revert that removes the trial directory and the new editor/compiler behavior. No serialized production data migration is required.

## Open Questions

- Which of the grass/soil art variants, if any, should replace the release base textures after review?
- Should a later production change expose additional square presets for future surfaces beyond grass and soil?
