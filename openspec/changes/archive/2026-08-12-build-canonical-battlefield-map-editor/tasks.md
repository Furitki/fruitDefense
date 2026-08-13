## 1. Canonical authoring asset

- [x] 1.1 Add Unity-serializable visual-cell, gameplay-cell, route, marker-group, and marker records using existing stable IDs; keep level/template/order publication data out of the map asset
- [x] 1.2 Add `BattlefieldMapAuthoringAsset` creation, default coverage, bounded cell queries, resize, and deterministic conversion to `BattlefieldLayeredMapSource`
- [x] 1.3 Add bounded mutation APIs for gameplay, presentation, route, and marker operations with unchanged-on-failure behavior
- [x] 1.4 Add structured authoring diagnostics that preserve invalid drafts and merge canonical compiler errors

## 2. Official bounded editor

- [x] 2.1 Add the official `关卡地图编辑器` menu, asset creation/opening, target selection, and persistent editor view state
- [x] 2.2 Add one shared top-down canvas layout for grid drawing, zoom/scroll, hover, selection, overlays, and pointer-to-cell hit testing
- [x] 2.3 Add gameplay workspace tools for reviewed capabilities and collision channels without modifying presentation
- [x] 2.4 Add ordered `route.main` editing, direction/endpoints, spawn/goal synchronization, core placement, and initial-pot marker/group editing
- [x] 2.5 Add semantic presentation tools for base, landform, edge style, single-cell, rectangle, fill, eyedropper, and explicit recommended-presentation application
- [x] 2.6 Group gestures and batch operations into coherent Undo/Redo units and keep current layer, tool, coordinate, diagnostics, dirty state, and publish state visible

## 3. Publication and runtime integration

- [x] 3.1 Add the authoritative publication manifest whose entries exclusively own levelId, templateLevelId, and order, plus the deterministic generated published-map catalog format and atomic full-rebuild exporter that deep-copies only fully valid assets
- [x] 3.2 Validate duplicate bundled/published map and level IDs, template level references, template-owned wave/rule/theme references, real palette registration, every semantic surface, and exact directed edge without modifying the last valid output on failure
- [x] 3.3 Extend normal catalog assembly to append generated authored maps and complete level definitions while preserving the three bundled levels when no generated catalog exists
- [x] 3.4 Add official Playtest gating that rebuilds/imports/reloads the generated resource and launches its stable levelId through normal AppFlow, Battle, shared projection, inherited waves/rules/theme, and settlement flow; keep any in-memory preview explicitly non-published

## 4. Terrain laboratory boundary

- [x] 4.1 Rename the existing terrain painter menu/window/evidence language to `地貌素材实验室`
- [x] 4.2 Remove material-board readiness from official map-editor acceptance while preserving focused material, edge, mask, Undo, and runtime-parity validation

## 5. Automated acceptance

- [x] 5.1 Add authoring-asset tests for blank creation, exact coverage, bounds rejection, resize defaults, deterministic save/reload/source round trip, and fingerprint stability
- [x] 5.2 Add editor-operation tests for gameplay/presentation independence, route adjacency, typed markers, rectangle/fill/eyedropper, recommendation behavior, and gesture Undo/Redo
- [x] 5.3 Add negative publication tests for incomplete coverage, out-of-bounds data, disconnected routes, missing core, marker conflicts, duplicate IDs, and invalid template references
- [x] 5.4 Add publication-manifest tests for full rebuild, idempotence, preserving other entries, cancellation, deleted-output recovery, failed atomic replacement, and draft changes remaining unpublished
- [x] 5.5 Add real-palette tests for missing surface bindings, reverse-only refined edges, and release registry omissions with cell-level diagnostics
- [x] 5.6 Add catalog/runtime tests proving a generated resource is reloaded, normal AppFlow resolves the expected authored levelId/mapId without C# changes, and the three bundled levels remain unchanged with or without generated content
- [x] 5.7 Add editor-versus-Battle terrain parity tests using the template's real palette, including base, landform, mask, and exact directed edge results
- [x] 5.8 Extend `FruitDefense.Editor.ProjectSetup.SmokeValidate` with focused canonical map-editor and publication markers

## 6. Human and release acceptance

- [x] 6.1 Create a fresh bounded acceptance map only through the official authoring asset/editor path, save/reopen it, publish it, and record the final diagnostics
- [x] 6.2 Capture and inspect the complete editor canvas showing gameplay, route/markers, presentation, bounds, and publish-ready state; do not use the terrain laboratory as substitute evidence
- [x] 6.3 Launch the published acceptance level in normal Battle, exercise spawn, traversal, planting, core damage, and settlement, and capture a real portrait runtime view
- [x] 6.4 Run Unity compilation, focused and aggregate smoke validation, `FruitDefense.Editor.ProjectSetup.SmokeValidate`, ordinary WebGL build, and release-flow parity checks
- [x] 6.5 Record verification evidence, run strict change and aggregate OpenSpec validation, and leave the change ready for user review without synchronizing the stable game-design overview
