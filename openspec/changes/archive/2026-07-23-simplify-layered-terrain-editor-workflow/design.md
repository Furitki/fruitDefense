## Context

`LayeredTerrainTilemap` already owns the canonical base, landform, and edge authoring layers and exposes validated `PaintBase`, `PaintLandform`, `PaintPair`, `EraseLandform`, and `EraseCell` operations. Its custom Inspector currently mixes those paint controls with every logical Tilemap, generated output, marker, base tile, landform TileSet, and directed edge reference. The four Project-window TileSet assets are implementation inputs, but their visibility makes them look like the intended authoring entry point.

This change is an editor-only usability layer over the completed `add-layered-terrain-brush-composition` behavior. It must not alter visual-cell serialization, runtime terrain resolution, mask topology, accepted art, gameplay identity, release scenes, or platform behavior. The candidate product direction is recorded separately in `docs/design/pending-design-review.md` and remains outside the formal game-design overview until explicitly approved.

## Goals / Non-Goals

**Goals:**

- Make the four common outcomes selectable without understanding `A/B`, foreground/background, TileSets, or generated outputs.
- Provide one discoverable map-tool entry, automatic but unambiguous target selection, visible active-brush feedback, and direct Scene painting.
- Keep precise landform-only and erasure operations available without crowding the primary workflow.
- Source author-facing names and previews from configuration while preserving the internal two-slot material contract.
- Preserve exact directed-edge validation and expose actionable reasons instead of substituting assets.
- Validate the editor workflow automatically and confirm that authored terrain still produces the accepted runtime/WebGL output.

**Non-Goals:**

- Changing the one-base-plus-one-optional-landform model, adding a third layer, or generalizing the runtime material graph.
- Generating, editing, or repairing raster art while the author paints.
- Replacing Unity Tilemaps, the existing Dual-Grid resolver, or the canonical authoring component.
- Changing gameplay, saves, snapshots, deterministic simulation, release navigation, combat UI, or mini-game platform support.
- Synchronizing the pending direction into `docs/design/game-design-overview.md` before explicit user approval.

## Decisions

### Use a dedicated painter window as the ordinary entry point

Add `LayeredTerrainPainterWindow` under the project map-tools menu. On opening, it resolves the selected `LayeredTerrainTilemap`; when exactly one valid component exists in the open scene it selects that component automatically, and when several exist it requires an explicit target choice. It never edits an arbitrary component because it happened to be found first.

The existing custom Inspector becomes a compact status and launch surface. Raw serialized references remain available under a collapsed `Developer configuration` section rather than being removed, because setup, diagnosis, and recovery still require them. A dedicated window is preferred to expanding the Inspector because it remains open while authors change selections and provides enough room for visual presets and guidance. A Scene overlay alone was rejected for the first version because it would compress configuration, validation, and error text into a small transient surface.

### Represent common work as four outcome presets

The primary palette presents four cards generated from configured material A and B metadata:

| Preset | Existing operation |
| --- | --- |
| Pure A | `PaintBase(cell, A)` |
| Pure B | `PaintBase(cell, B)` |
| A on B | `PaintPair(cell, A, B, refinedEdge)` |
| B on A | `PaintPair(cell, B, A, refinedEdge)` |

Display names turn those cards into `Pure grass`, `Pure soil`, `Grass on soil`, and `Soil on grass` for the accepted sample, but the painter never branches on those literal strings. An editor-facing palette/profile supplies each internal slot's stable display name, thumbnail or swatch, and optional descriptive text. Missing display metadata is a configuration warning and falls back to a neutral `Material A/B` label only in the developer surface, not as a successful author-facing setup.

This maps directly onto validated existing operations instead of introducing a second mutable terrain model. The window owns selection state only; the Tilemaps remain the single authoring truth.

### Keep edge choice contextual and exact

Pure-base presets hide edge controls. Pair and landform-only modes show `Base edge` and `AI-refined edge`; the latter initially follows the existing enabled default and means selecting a pre-authored edge TileSet, never invoking AI. The selection persists for the current editor session.

When the exact directed edge is unavailable, `AI-refined edge` is disabled with the reason returned by `CanPaintPair`. `Base edge` remains selectable. The painter does not silently reverse the pair, silently change the selected edge mode, or rasterize a fallback.

### Separate advanced layer edits and explicit erase tools

A collapsed `Advanced layer operations` section provides landform A, landform B, erase-landform, and clear-cell tools. Erase-landform preserves the base; clear-cell removes all three authoring markers. Those tools remain explicit modes instead of overloading Shift-drag, so the destructive scope is visible before painting.

Left-drag paints the active tool. `Escape` ends the active paint session, and Unity's normal Undo/Redo remains authoritative. A drag from mouse-down through mouse-up forms one Undo group even when it crosses multiple cells. Re-entering one cell during the same gesture does not create duplicate mutations.

### Share one editor-only paint session controller

Move Scene input, hover outline, active-brush label, gesture grouping, and dirty marking behind one editor-only session/controller consumed by the painter window. This avoids separate static state in the custom Inspector and prevents two tools from painting concurrently. The controller delegates all mutations to public `LayeredTerrainTilemap` operations and never writes generated outputs directly.

The controller subscribes to Scene-view callbacks only while the painter is active and reliably unsubscribes on window disable, target loss, play-mode transition, script reload, or explicit stop. Domain reload may reset the active gesture but must not mutate terrain.

### Validate editor UX separately from runtime parity

Focused editor tests cover target resolution, configured labels/previews, the four preset mappings, contextual edge choices, unavailable directed pairs, empty-base landform rejection, explicit erase scopes, one-gesture Undo, and teardown. A captured Unity editor view proves the controls are understandable without selecting raw assets.

Because the editor window does not ship to players, WebGL does not validate the window itself. The ordinary WebGL build and portrait terrain capture instead prove that maps authored through the new front end preserve the accepted runtime result and release flow.

## Risks / Trade-offs

- [A dedicated window can lose its scene target after reload] → Re-resolve by serialized scene object when valid, otherwise stop painting and require an explicit target.
- [Hiding raw references can make configuration failures harder to diagnose] → Keep a validation summary and an explicit collapsed developer section with the exact failing reason.
- [A/B is still present internally] → Treat it as a bounded storage slot only; all normal labels and previews come from configured authoring metadata.
- [Multiple scene maps could be edited accidentally] → Auto-select only the current selection or a sole valid candidate; require a dropdown choice when ambiguous.
- [Scene callbacks can survive a stale window state] → Centralize subscription lifecycle and test disable, play-mode, reload, and destroyed-target cleanup.
- [Visual cards consume Inspector space] → Keep the primary painter in a resizable window and make the custom Inspector a launch/status surface.
- [Editor-only work could be mistaken for runtime UI work] → Keep the change scoped to `Assets/Editor` plus authoring metadata and use WebGL only for output parity evidence.

## Migration Plan

1. Add and configure author-facing material metadata for the existing A/B sample without changing canonical Tilemaps or terrain art.
2. Introduce the shared editor paint-session controller and preserve the current paint operations as its only mutation boundary.
3. Add the dedicated painter window, four preset cards, contextual edge selector, advanced operations, and Scene feedback.
4. Reduce the custom Inspector to validation, painter launch, rebuild, and collapsed developer configuration.
5. Add focused smoke coverage, capture the editor workflow, then rebuild and inspect ordinary portrait WebGL output for parity.

Rollback removes the painter window/session and restores the current custom Inspector controls. Authored maps remain valid because the underlying component, logical Tilemaps, material slots, and generated outputs are unchanged.

## Open Questions

- The pending design-review document still requires explicit approval before any part of this workflow is promoted into the formal game-design overview.
- Additional materials beyond the current two-slot authoring component require a separate contract change; this proposal only ensures that adding display metadata later does not leak `A/B` into the author-facing UI.
