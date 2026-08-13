## Context

The canonical battlefield already uses a square logical grid and draws opaque bases cell by cell. Transparent landforms and optional pair edges are rendered on the established half-cell-offset Dual-Grid, where one visual tile is selected from the four surrounding logical cells. The resolver is suitable for both organic and square contours, but the current palette binds only one landform TileSet per surface, the visual-cell schema has no contour identity, and the bundled generator turns one occupied logical cell into a bilinear diamond-like silhouette.

The existing AI pass produced useful high-resolution painted texture, but packaging reduced it to a narrow 32-pixel edge-only overlay. This change must preserve deterministic sockets and gameplay separation without preserving that failed visual contract.

## Goals / Non-Goals

**Goals:**

- Keep one canonical square map and one NW/NE/SE/SW mask authority while supporting square and organic contour styles.
- Make contour style presentation-only, explicitly authored, palette-validated, and available to the map and terrain-laboratory workflows.
- Make the bundled battlefields use square rounded-cell footprints and retain organic contours for reviewed decorative regions.
- Produce a high-resolution grass-on-soil square transition that reads as two adjoining top-down ground materials at the real portrait battle scale, without a raised-platform rim.
- Prove seam continuity, topology, editor behavior, gameplay identity parity, and ordinary WebGL output.

**Non-Goals:**

- A second gameplay grid, per-pixel collision, runtime raster generation, arbitrary overlay stacks, height simulation, or a general material graph.
- Allowing authors to alternate incompatible contour styles freely inside one connected landform.
- Changing routes, placement, markers, snapshots, simulation, economy, progression, or mini-game platform status.

## Decisions

### Store contour identity beside the optional landform

`BattlefieldVisualCellSource` gains a presentation-only `contourStyleId`. It is empty when no landform exists and required when a landform exists after migration. Stable IDs initially include `contour.square` and `contour.organic`. The gameplay fingerprint continues to exclude the complete visual-cell payload.

An explicit field is preferred to encoding style into `surfaceId` because grass remains semantically grass, and to encoding it into `edgeStyleId` because shape and optional painted treatment are independent. Legacy constructors may interpret a missing field as the legacy organic contour only at the compatibility boundary; bundled authored assets are rewritten with explicit square identity and validated without silent runtime fallback.

### Bind base, contour, and edge responsibilities separately

The terrain palette keeps one opaque base texture per surface, adds contour bindings keyed by `(surfaceId, contourStyleId)`, and extends edge bindings to `(landformSurfaceId, baseSurfaceId, contourStyleId, edgeStyleId)`. A square edge never substitutes for an organic edge. Within the same material pair, contour, and edge style, one ordered binding may serve the reverse order by complementing the four-corner mask; an exact reverse binding remains an optional compatibility override.

This avoids duplicating semantic materials while keeping each geometry-specific asset explicit. Compatibility accessors may expose the old single-landform view only while existing callers migrate.

Opaque base sampling is independent from any landform TileSet's native pixel size. Every base surface supplies its own required texture and stable cell-space UV scale. Validation requires consistent dimensions inside one TileSet and compatible normalized sockets between landform and edge TileSets for the same contour binding, but it does not require organic 32-pixel assets and square 256-pixel assets to share one global native size.

### Reuse the current resolver and projection

Both styles use the existing sixteen mask values and `VisualTileRect`. Mask membership is resolved from equality of both landform surface and contour style. Square artwork interprets each occupied corner as ownership of its corresponding quadrant, with a bounded rounded turn at the dual-tile center; organic artwork retains the current interpolated contour. The renderer therefore gains no second grid or coordinate system.

A shared visual vertex may observe at most one contour style among all landform-bearing cells, regardless of landform material. Four-connected and diagonal/shared-vertex cross-style cases are rejected until a dedicated transition contract exists. Different materials or disconnected components may use different styles in one map.

### Treat the hand-painted transition as a top-down material blend

The square style uses a native 256-pixel target. Its optional grass-on-soil transition covers only a narrow band around the material contact, uses the same tiled grass visual as the landform interior, and fades irregular grass encroachment directly into the already-rendered soil base. It contains no directional exposed-soil wall, dark contact stroke, or second outer shadow contour. The retained generated ribbon remains provenance and supplies only the reviewed irregular lip profile; its side-view soil and shadow rows are not runtime paint.

Deterministic packaging derives color from the registered base-material textures, derives contour variation from the retained lip profile, locks the required tile-border sockets, and keeps the topology class. At the 46-pixel portrait evidence scale, the complete outside feather must remain a small transition rather than collapse into a one-pixel dark line or expand into a visible curb.

The first polished pair is square grass on soil. Because the bundled maps also contain stone-road landforms, migration additionally supplies a square stone-road landform TileSet and binding; it may reuse approved existing painted pixels during this change, but it must satisfy the same square topology and resolution-independent runtime contract. A bundled map is not considered migrated while any explicit square surface lacks its exact binding.

The later explicitly authorized production-brush pass promotes two opaque full-composite families from the deterministic v3 pipeline: A binds grass as the foreground and soil as the background, while B binds stone-road as the foreground and water as the background. Mask 00 and mask 15 become the matching opaque base endpoints, all sixteen runtime files retain their source manifest, and the full-composite family is registered only as the exact square refined edge for its semantic pair. The initial Runtime32 package is mechanically repackaged from the unchanged Review256 masks into Runtime64 using the descriptor-owned runtime size; the 32-pixel family remains only as the fixed 16×16 stress-atlas sampling source. Existing transparent landform TileSets remain the geometry authority underneath the composite pass. The stone-water registration adds the water base but does not invent a water landform TileSet; therefore the installed primary preset is stone-on-water, while an unsupported water landform remains unavailable.

The art pipeline emits a small `BrushImport.json` descriptor beside each candidate. It owns the stable brush id, author-facing labels, foreground/background surface ids, contour id, edge id, endpoint masks, runtime-mask directory, and runtime tile size. A generic Unity importer validates that descriptor plus the retained pipeline manifest, copies all sixteen masks, configures PPU from the declared size, creates the TileSet and endpoint tiles, and creates or updates one `TerrainBrushDefinition` asset. It never contains profile-specific paths or grass/stone conditionals.

Imported definitions form the authoring registry. Palette setup merges their base endpoints and exact edge bindings with the reusable landform bindings, and the canonical map editor enumerates the same definitions instead of hard-coded pair buttons. The terrain laboratory renders every valid definition simultaneously in one stable, scrollable preview gallery rather than hiding definitions behind a row of name-only switches. Each card assembles the real registered composite TileSet into an isolated-cell preview, names the material pair, and reports whether only the declared direction or both directions have real landform dependencies. Selecting a card reconfigures the selected diagnostic target from the release palette and the definition; the existing direction tools then operate on that active pair. A foreground landform is mandatory; a background landform is optional, so stone-on-water is usable while water-on-stone remains disabled rather than inventing a water landform. The gallery remains visible when the canvas is non-empty, but reconfiguration refuses the switch to avoid silently reinterpreting existing A/B marker cells.

Preview cards reserve a square artwork rect above their footer and center it without changing the sprite aspect ratio. The gallery may scale a runtime tile uniformly, but it must never stretch its horizontal and vertical axes independently. At the 46-pixel portrait scale, Runtime64 is minified rather than Runtime32 being enlarged, reducing blur while keeping the accepted Review256 source, semantic masks, topology, and stress-board density unchanged.

The organic assets remain available and are not overwritten. Generated candidates are accepted from assembled boards and real Battle scale, never from prompt compliance alone.

### Make authoring choices explicit and safe

The canonical map editor shows `Square` and `Organic` only when the required surface assets and either direction of the same-contour edge pair exist. The terrain resource-acceptance Overlay instead displays its target's preconfigured contour read-only and does not offer a second contour switch. Square is the default for newly authored gameplay-aligned landforms. Painting across an existing component either applies one style to the whole affected component as one undoable action or is refused with an actionable message; it never leaves an accidental mixed component. Inside one exact `(landform, base, contour)` connected region, an optional edge style is likewise uniform for this first version so a partially enabled edge cannot create a false internal soil band.

### Validate visual behavior without touching gameplay identity

Focused smoke tests cover palette keys, serialization, migration, mask resolution, component rules, single-cell square bounds, strips, turns, holes, diagonal masks, and coexistence. The aggregate smoke compares gameplay fingerprints and deterministic fixtures before and after visual migration. Final acceptance uses the ordinary portrait WebGL Battle surface and retains the release scene flow.

## Risks / Trade-offs

- [Different contour sockets touch at one vertex] -> Reject shared-vertex style mixing until a dedicated transition TileSet is designed.
- [High-resolution assets shimmer when minified] -> Keep the 256-pixel authored source, enable mipmaps for runtime square sprites, and validate the 72-pixel editor and 46-pixel portrait render scales before expanding materials.
- [AI drifts the atlas topology] -> Generate against an explicit square reference, preserve only selected candidates, and gate import on deterministic socket and assembled-board checks.
- [Compatibility fallback hides missing authoring] -> Limit fallback to legacy construction/migration and require explicit contour IDs in canonical authored assets.
- [Feather becomes a halo or hard line after minification] -> Bound native transition width, reject dark/soil pixels in the edge overlay, and inspect scaled luminance continuity around isolated cells, strips, turns, holes, controls, pots, core, and markers.
- [Per-style draw grouping changes order] -> Define stable palette order and test complementary regions for gaps and unintended overlap.
- [A brush definition drifts from its imported files] -> Validate descriptor identity, sixteen masks, endpoint masks, TileSet references, palette registration, and both authoring consumers in one focused smoke.

## Migration Plan

1. Add contour IDs, visual-cell serialization, compiler validation, and compatibility construction; bump the map schema and published-catalog schema so fieldless Unity assets cannot bypass migration.
2. Split palette bindings and migrate renderer/mask queries to surface-plus-contour keys.
3. Add editor selection and connected-component validation.
4. Retain the current organic TileSets, add square grass and stone-road TileSets, and bind geometry-specific edges.
5. Rewrite the three bundled authored maps with explicit square contours and verify gameplay identity parity.
6. Run focused and aggregate smoke, build ordinary WebGL, and capture real portrait evidence.

Rollback selects the retained organic bindings and restores the previous visual-cell compatibility representation. Gameplay and persistence data require no rollback.

## Open Questions

- Whether a later change should support authored square-to-organic transition tiles at a shared vertex remains outside this first implementation.
- Material pairs beyond the now-authorized grass/soil and stone/water production brushes remain separately authorized art work.
