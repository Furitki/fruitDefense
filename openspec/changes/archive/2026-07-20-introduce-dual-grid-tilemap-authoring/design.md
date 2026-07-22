## Context

The current battlefield owns a deterministic logical cell map, while its presentation is drawn directly through IMGUI. The project already includes Unity's Tilemap module but has no reusable Tilemap authoring layer. Authors therefore have no way to paint a compact logical terrain map and derive all edge/corner transitions automatically.

This change introduces a presentation-only, reusable Dual-Grid prototype alongside the current battle flow. It must work in edit mode, be callable at runtime, avoid a dependency from gameplay simulation to scene objects, and remain usable with final art supplied later.

## Goals / Non-Goals

**Goals:**

- Resolve every visual vertex from the four surrounding logical cells with one documented 4-bit convention.
- Let one configured component generate a visual Tilemap from a logical Tilemap and keep it current during editor painting.
- Support separate ground, road, and wall layers by reusing the component with different source/output Tilemap pairs and tile-set assets.
- Provide deterministic full and local refresh APIs, validation, procedural placeholder art, and a developer demo scene.
- Provide a reproducible editor-only path from seamless grass/soil sources to topology-safe, seam-validated final-art test tiles.
- Preserve the existing release scenes, battle rules, saved state, platform adapters, and ordinary WebGL baseline.

**Non-Goals:**

- Replacing `BattlefieldMapDefinition`, route traversal, or the current IMGUI battlefield in this change.
- Defining final terrain art, multi-terrain blending priority, animated tiles, colliders, navigation, or an in-game map editor.
- Adding the demo scene to the release build or claiming mini-game platform readiness.

## Decisions

### Keep logical and generated Tilemaps as separate scene objects

`DualGridTilemap` receives an author-owned logical source Tilemap and an exclusively generated output Tilemap. A source cell is occupied when it contains any `TileBase`; separate terrain categories use separate component pairs. The renderer never stores authoring state in its output.

This makes the ownership boundary obvious, allows source tiles to be simple paint markers, and prevents output edits from becoming accidental source data. Encoding terrain identifiers into one Tilemap was rejected for the prototype because transition priority between more than two materials requires a separate design contract.

### Use a fixed clockwise four-corner mask

An output cell represents a logical-grid vertex. Bits are assigned clockwise as north-west `1`, north-east `2`, south-east `4`, and south-west `8`. Output vertex `(x, y)` samples logical cells `(x-1, y)`, `(x, y)`, `(x, y-1)`, and `(x-1, y-1)` respectively.

The generated Tilemap is offset by negative half a grid cell, placing its cell centers on logical vertices. This preserves square tiles, makes the mapping inspectable, and caps a binary terrain set at 16 lookup states. Rotation-based inference was rejected because asymmetric final art and import pivots make it harder to validate.

### Make the tile set an explicit 16-slot asset

`DualGridTileSet` stores one `TileBase` reference per mask. Masks `1` through `15` are required; mask `0` is optional and remains empty for transparent overlay layers. The same renderer can therefore use ordinary `Tile`, animated/custom `TileBase`, or the procedural demo tile without knowing the art type.

A dictionary was rejected because Unity serializes fixed arrays more predictably, and the mask index is already a compact stable key.

### Combine automatic edit-mode detection with explicit runtime APIs

In edit mode and when automatic refresh is enabled, the component computes a compact signature of source bounds and tile instance IDs, then rebuilds only when that signature changes. Runtime callers can use `SetLogicalTile`, `RefreshLogicalCell`, or `Rebuild` directly; local refresh touches only the four visual vertices affected by one logical cell.

This avoids relying on version-sensitive editor callbacks and still keeps normal Tile Palette painting responsive. Continuous unconditional rebuilds were rejected because larger maps would produce avoidable allocations and scene dirtiness.

### Generate the demo through an editor setup command

An idempotent editor command creates procedural logical/visual tile assets, a configured palette, and `Assets/Scenes/DualGridDemo.unity`. The scene contains a pattern with convex corners, concave corners, a hole, and disconnected cells, plus a camera and usage label. The procedural tiles visualize the actual mask and are replaceable with final art.

The demo stays outside release build settings. Editor smoke validation is also called from `ProjectSetup.SmokeValidate`, so the release acceptance entry detects mask or refresh regressions without depending on the demo assets.

### Store final-art bake intent in an editor-only profile

`DualGridTerrainBakeProfile` owns source textures, output resolution, supersampling, deterministic seed, edge width, soil-rim width, irregularity, and grass-tuft controls. Generated PNG, Sprite, Tile, TileSet, preview, and evidence files are derived artifacts and are never the authoring source of truth.

Keeping these controls in a profile makes regeneration reviewable and lets another binary terrain category reuse the baker without editing code constants. Runtime assemblies continue to depend only on `DualGridTileSet` and ordinary `TileBase` assets.

### Rasterize a normalized pixel-distance edge at four-times resolution

The baker first evaluates the four-corner scalar field plus periodic low-frequency contour displacement. It divides the signed field value by its local gradient magnitude to approximate distance in output pixels. Alpha coverage, exposed soil, and grass blending are then evaluated in separate pixel ranges, so a crisp silhouette does not require a hard or unnaturally narrow material transition.

Each output pixel integrates a four-by-four subpixel grid. Periodic noise and canonical border sampling preserve shared-edge identity. Masks `5` and `10` receive an inward saddle bias so diagonal islands cannot be joined by noise at the center.

### Layer generated grass over a corresponding soil base

The grass TileSet remains a transparent binary overlay. The demo adds a lower soil Tilemap under the connected/manual test area, matching the production ownership model: base ground, generated terrain overlay, detail props, then gameplay objects. This avoids judging a turf edge against camera clear color while preserving the mask gallery's transparent-background diagnostic.

## Risks / Trade-offs

- [Large logical Tilemaps make signature scans expensive in edit mode] → Scan only occupied source bounds, skip work when the signature is unchanged, and provide an option to disable automatic refresh.
- [Generated output can erase hand-authored tiles] → Require a distinct output Tilemap, label it as generated, validate source/output separation, and document that the renderer owns the entire output.
- [Corner orientation can be interpreted differently by art authors] → Expose named mask bits, show numeric/binary mask labels in the asset inspector, and validate all 16 states in the demo.
- [Procedural placeholder sprites differ from final imported art] → Keep the renderer typed to `TileBase` and use placeholder tiles only for demo and validation.
- [Wide scalar-field smoothing makes organic edges look blurred] → Convert the field to approximate pixel distance, keep alpha antialiasing independent from the soil/grass blend, and supersample before writing the final sprite.
- [Random grass detail can break tile seams or reproducibility] → Use a profile seed with periodic deterministic functions and verify every compatible shared alpha edge after each bake.
- [The prototype does not blend multiple terrain IDs in one layer] → Author each binary terrain category as a separate source/output layer until terrain priority is explicitly designed.

## Migration Plan

1. Add and validate the runtime mask, tile-set, and generation component without touching existing battle presentation.
2. Add editor inspectors, smoke validation, procedural demo assets, and the demo scene.
3. Document how to paint a logical layer and replace the placeholder tile set.
4. If later adopted by the release battlefield, open a separate OpenSpec change for map-data migration and visual acceptance.

Rollback removes the prototype scene/assets and new Tilemap code; no gameplay or persisted data migration is required.

## Open Questions

Final terrain art, layer priority, collider generation, and adoption by the release battlefield remain follow-up decisions.
