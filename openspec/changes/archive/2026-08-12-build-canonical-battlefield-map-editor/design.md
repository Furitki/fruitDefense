## Context

The runtime already has the correct domain boundary: `BattlefieldLayeredMapSource` owns complete visual cells, gameplay cells, ordered routes, marker groups, and typed markers; `BattlefieldLayeredMapCompiler` validates and compiles that aggregate; `LevelCatalogSource` combines a map with waves, rules, and a theme. The missing piece is an authoring and publication path. Bundled maps are still constructed in `BundledLevelCatalogFactory`, while `LayeredTerrainTilemap` edits only a non-release Tilemap scene and accepts every `WorldToCell` coordinate.

The editor must therefore author the existing canonical aggregate rather than promote Tilemap scene state into another map model. Invalid work-in-progress must remain saveable, but only the canonical compiler and catalog validator may authorize publication or Battle launch.

## Goals / Non-Goals

**Goals:**

- Create, edit, save, reopen, compile, publish, and playtest a complete battlefield map without changing C#.
- Keep one bounded authoring asset whose serialized fields round-trip deterministically into the existing canonical layered source.
- Make gameplay topology, ordered route/markers, and semantic terrain understandable on one shared two-dimensional grid.
- Prevent out-of-bounds painting and incomplete publication while preserving invalid drafts for iterative work.
- Publish deterministic runtime data that can join the level catalog through a reviewed existing wave/rule/theme template.
- Preserve bundled levels, simulation identity, projection, snapshots, release scenes, and platform boundaries.

**Non-Goals:**

- Adding multi-route execution, trigger behavior, item spawning, new cell capabilities, combat rules, economy, or progression.
- Making visual surfaces authoritative for planting, movement, collision, route order, or markers.
- Replacing the existing compiler, `BattlefieldProjection`, terrain palette, or Dual-Grid renderer.
- Turning the material laboratory into a player-facing or release scene.
- Shipping arbitrary user-authored scripts, property bags, runtime raster processing, or hot code loading.

## Decisions

### Store draft data in one canonical map authoring asset

Add `BattlefieldMapAuthoringAsset`, a Unity `ScriptableObject` with serialized schema/map identity, dimensions, map-units-per-cell, complete visual and gameplay cell arrays, one current primary route, typed marker groups, and typed markers. Serializable records use stable string IDs and convert to `BattlefieldLayeredMapSource`; they do not introduce new runtime enums or infer data from sprites.

The asset exposes bounded mutation methods used by both the editor and tests. Resizing is an explicit destructive operation with Undo and a default-fill policy. New assets fill every visual cell with soil and every gameplay cell with an empty validated record so missing coverage cannot arise from ordinary creation.

Keeping scene Tilemaps as the source was rejected because they cannot express ordered routes, typed markers, stable IDs, publication metadata, or a portable deterministic aggregate. Creating separate assets for each layer was rejected because sizes and identities could drift.

### Use a dedicated bounded 2D editor canvas

Add `CanonicalBattlefieldMapEditorWindow` under `Fruit Defense/地图工具/关卡地图编辑器`. The window edits a selected `BattlefieldMapAuthoringAsset` and draws a top-down grid from one layout helper used for cell rendering, pointer-to-cell hit testing, overlays, selection, and diagnostics. It does not capture the 3D Scene view.

The toolbar has four workspaces: gameplay, route/markers, presentation, and validation. Every workspace shows map identity, dimensions, hovered coordinate, active tool, diagnostics count, dirty state, and publish status. Grid scrolling/zooming changes only editor view state. Presentation draws semantic cells through the selected template level's real theme and `BattlefieldTerrainPalette`, using the same base/landform/edge mask rules as Battle; missing bindings are blocking diagnostics rather than successful placeholder colors.

Single-cell painting is the minimum interaction. Rectangle, flood-fill, and eyedropper operate on the same bounded asset mutation API and use one Undo group per gesture or batch. If delivery pressure requires staging, these tools may share one generic cell-set mutation implementation rather than independent code paths.

### Edit the current execution profile explicitly

Gameplay tools toggle only the finite existing capabilities and collision channels. The route tool owns the ordered `route.main` list: appending requires cardinal adjacency, selecting an existing route cell truncates or removes a tail only through an explicit command, and invalid imported data remains visible as diagnostics. Spawn and goal markers are synchronized to the route endpoints as an explicit route operation; the core and initial-pot candidates are placed as typed markers with stable generated IDs and editable group selection count.

The editor does not add multi-route UI because the runtime rejects more than one active route. A free-form marker payload inspector was rejected because it would bypass the finite compiler contract.

### Keep presentation independent, with explicit suggestions

Presentation cells retain one base, optional landform, and optional exact directed edge style. The editor offers semantic materials, not A/B or masks. Dual-Grid masks remain derived at preview/runtime. Edge style applies through a cell-set operation and exact palette validation; missing reverse-direction art is never borrowed.

`Apply recommended presentation` is an explicit, previewable, undoable command that writes stone-road presentation for route cells, grass for plantable cells, and soil elsewhere. It never runs automatically after gameplay edits and never changes gameplay when presentation is painted.

### Separate draft validation from publication authorization

Every edit may update a cached diagnostic list. Authoring checks report asset-shape and editor-specific problems; canonical compilation reports layer, route, capability, marker, and identity problems; catalog publication adds duplicate level/map IDs, template references, theme/palette compatibility, and content-version checks. Diagnostics carry severity, stable code, field, and optional cell/marker identity.

Draft assets may be saved with errors. `Publish` and `Playtest` are enabled only when compilation and catalog validation succeed. Warnings such as isolated visual regions or presentation/gameplay mismatch remain visible but do not become gameplay rules.

### Use one publication manifest and a deterministic generated catalog resource

Add a single `BattlefieldMapPublicationManifest` ScriptableObject as the only mutable authority for the published set. Each ordered entry references one draft authoring asset, owns a stable unique `levelId`, and selects exactly one existing `templateLevelId`. The template atomically supplies its wave-set, rule-set, and theme; the first version does not expose independent wave/rule/theme mixing.

Publication always rebuilds from the manifest's complete current contents. It never reads the previous generated resource as input and never scans arbitrary drafts. It deep-copies normalized map data plus template references into one derived `PublishedBattlefieldMapCatalog` resource ordered by manifest order then stable ID. Removing an entry and rebuilding removes it; deleting the generated resource and rebuilding produces equivalent content. Runtime catalog assembly reads this resource, appends valid published maps and level definitions to the three bundled entries, and runs the normal compiler.

Before replacing the generated resource, the exporter resolves the template theme to the real editor/runtime `BattlefieldTerrainPalette` registration and validates every base surface, optional landform, and exact directed edge at its map coordinate. A missing surface binding, reverse-only edge, or palette not registered for the release Battle scene blocks publication with map/cell/pair diagnostics. Failed publication leaves the last valid resource unchanged; duplicate IDs never replace existing definitions.

Official Playtest first performs or verifies a successful manifest rebuild, saves/imports the generated resource, forces normal catalog reassembly from that resource, selects the stable published `levelId` through AppFlow, and opens the normal Battle route. An optional unsaved/in-memory canvas preview may aid editing but is labeled non-published preview and cannot satisfy Playtest or acceptance.

### Reclassify the existing painter as a material laboratory

Rename its menu/title and documentation to `地貌素材实验室`. It may continue to use the acceptance scene, infinite diagnostic arrangements, islands, holes, and masks because those are valid art tests. Its validation proves terrain assets and painter operations only. Project smoke and evidence must no longer describe the scene as an author-ready map or satisfy canonical map-editor acceptance.

## Risks / Trade-offs

- [Unity serialization of polymorphic domain types is fragile] → Use explicit serializable authoring records and one conversion boundary into the existing immutable source.
- [Runtime resource loading could make catalog order nondeterministic] → Load one generated catalog resource and sort normalized entries by explicit order then stable ID before compilation.
- [Manifest and generated output can drift] → Treat the manifest as the only publication authority, rebuild the derived resource from scratch, prohibit reverse import, and test deletion/rebuild equivalence.
- [Catalog validation lacks Unity palette assets] → Make the editor publisher resolve the template theme through the real palette registry and validate each semantic material/directed edge before generating runtime data.
- [Invalid drafts can be mistaken for published content] → Use separate draft assets and generated published output, visible status, and atomic publish-on-success.
- [The editor becomes too dense] → Use four workspaces on one shared canvas, contextual inspectors, and no raw Tilemap/TileSet controls.
- [Route endpoint synchronization may hide data changes] → Make it one named undoable route operation and display the affected spawn/goal IDs.
- [Presentation suggestions could erode layer independence] → Run only on explicit command, show a preview/count, and write presentation without changing gameplay.
- [A broad first implementation can regress release flow] → Preserve bundled-source construction, append only validated generated entries, and require aggregate smoke plus normal WebGL acceptance.
- [Existing dirty worktree contains related terrain changes] → Keep new files isolated where possible and adapt around existing edits without reverting them.

## Migration Plan

1. Add the authoring asset, serializable records, bounded mutation API, source conversion, diagnostics, and round-trip tests.
2. Add the bounded editor window and minimum complete workflows for gameplay, route/markers, presentation, Undo, and validation.
3. Add the publication manifest, real-palette validation, deterministic full rebuild, and catalog-source composition while keeping all three bundled levels unchanged.
4. Add normal Battle playtest by reloading the generated resource, selecting its stable level identity through AppFlow, and using shared projection.
5. Reclassify the existing layered painter and its evidence as a material laboratory.
6. Create a fresh authored acceptance map, save/reopen it, publish it, compile it, launch it in Battle, and capture editor plus portrait runtime evidence.
7. Run focused smoke, `FruitDefense.Editor.ProjectSetup.SmokeValidate`, strict OpenSpec validation, Unity compilation, and ordinary WebGL build/acceptance.

Rollback removes the generated published catalog resource and editor menu while leaving draft assets intact. Bundled catalog construction remains the fallback and no save/profile migration is required.

## Open Questions

None for implementation. The candidate product direction remains in `docs/design/pending-design-review.md` until the user separately approves synchronization into the stable game-design overview.
