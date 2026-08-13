## Why

The current so-called map editor only paints layered terrain into an unbounded non-release Tilemap demo, while playable maps are still constructed in C# and the editor cannot author gameplay cells, routes, markers, catalog entries, or a Battle-ready map. This makes floating cells and incomplete layouts valid editor output and lets material-mask evidence pass as map-authoring acceptance.

## What Changes

- Add a canonical battlefield map ScriptableObject authoring source that round-trips the existing versioned three-layer map aggregate without creating a second runtime truth.
- Add a bounded two-dimensional map editor for identity/dimensions, gameplay topology, ordered route and typed markers, semantic terrain, validation, and publication.
- Create every new map with complete default visual and gameplay coverage; reject drawing outside its fixed bounds.
- Add single-cell, rectangle, flood-fill, eyedropper, and explicit topology-to-presentation suggestion operations with gesture-level Undo/Redo.
- Add structured live diagnostics and a draft-versus-publish gate: invalid drafts may be saved, but invalid maps cannot be exported, registered, or launched for playtest.
- Add an explicit publication manifest whose referenced authoring assets can deterministically and atomically rebuild the generated level-map catalog using one existing reviewed template level per entry, without requiring C# edits.
- Validate every published semantic base, landform, and exact directed edge against the template theme's real registered `BattlefieldTerrainPalette`, then make Battle playtest reload the generated catalog and consume the same published level through normal AppFlow.
- Reclassify the current layered terrain painter as a terrain-material laboratory for base/landform/edge and sixteen-mask art validation; it is no longer a successful map-authoring entry or acceptance surface.
- Preserve the current three bundled levels, gameplay identity rules, deterministic simulation, projection, release scene flow, and ordinary WebGL baseline.

## Capabilities

### New Capabilities

- `canonical-battlefield-map-editor`: Defines bounded asset creation, layer-oriented editing, route/marker tools, diagnostics, Undo, preview, publication, and end-to-end author acceptance.

### Modified Capabilities

- `battlefield-layered-map-model`: Adds a Unity authoring asset and deterministic round trip into the existing canonical source/compiler without changing runtime ownership or layer independence.
- `level-map-catalog`: Allows validated editor-authored maps to publish as complete playable level compositions through stable IDs and reviewed wave/rule/theme templates.
- `layered-terrain-painter-workflow`: Reclassifies the existing scene painter and diagnostic board as a terrain-material laboratory rather than the official map editor or map-readiness evidence.

## Impact

- New runtime-safe map authoring asset and portable serialization adapters under `Assets/Scripts/Core` or `Assets/Scripts/Content`.
- New dedicated Unity Editor window, bounded canvas, tools, diagnostics, publication, and playtest commands under `Assets/Editor`.
- Level catalog assembly gains deterministic editor-authored map/level inputs while preserving the three bundled definitions and unknown-ID rejection.
- Focused editor tests, aggregate project smoke, asset round-trip tests, invalid-publication tests, Battle integration, and real portrait WebGL evidence.
- No new combat mechanics, economy, progression, backend, mini-game adapter claim, or runtime image generation.
