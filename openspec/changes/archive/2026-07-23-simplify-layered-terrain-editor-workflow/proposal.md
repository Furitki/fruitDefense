## Why

The layered terrain system is functional, but its current Inspector exposes internal concepts such as `A`, `B`, foreground/background, TileSets, and generated outputs. A map author therefore has to understand the implementation before they can perform the common tasks of painting a pure base, composing one terrain over another, or choosing an optional refined edge.

## What Changes

- Add one task-oriented terrain painter entry point that locates the active layered map and keeps raw Tilemap and TileSet configuration out of the ordinary authoring flow.
- Replace internal `A/B` controls in the primary workflow with palette-provided names, thumbnails or swatches, and four explicit presets: pure grass, pure soil, grass on soil, and soil on grass.
- Show base-edge versus AI-refined-edge selection only for brushes that contain a landform; selecting AI refinement uses an already-authored directed edge asset and never invokes image generation while painting.
- Add a collapsed advanced workflow for landform-only painting, landform-only erase, whole-cell clearing, and developer configuration.
- Keep an always-visible active-brush summary, concise Scene-view guidance, one-gesture Undo, and author-facing disabled reasons for missing bases or unavailable directed edge assets.
- Preserve the existing one-base-plus-one-optional-landform data contract, exact directed-pair validation, generated output ownership, runtime rendering, gameplay identity, release scene order, and accepted WebGL presentation.
- Record the candidate product direction in `docs/design/pending-design-review.md`; it remains unapproved and does not update the formal game-design overview through this change.

## Capabilities

### New Capabilities

- `layered-terrain-painter-workflow`: Defines the author-facing terrain painter entry, semantic preset selection, advanced layer operations, guidance, validation feedback, and editor acceptance contract.

### Modified Capabilities

None. The existing layered terrain source, brush operations, Dual-Grid topology, runtime presentation, and level-map contracts remain unchanged.

## Impact

- Unity editor tooling around `Assets/Editor/LayeredTerrainTilemapEditor.cs`, potentially including a dedicated EditorWindow or Scene overlay backed by the existing `LayeredTerrainTilemap` operations.
- Authoring metadata needed to expose stable display names, thumbnails or swatches without using `A/B` as user-facing labels.
- Focused editor smoke coverage for preset-to-layer mappings, contextual edge controls, invalid-operation feedback, erasure semantics, selection, and Undo.
- Editor visual evidence for the simplified workflow plus existing portrait WebGL output parity. There is no new player-visible flow, gameplay, persistence, combat, platform-adapter, or runtime image-generation behavior.
