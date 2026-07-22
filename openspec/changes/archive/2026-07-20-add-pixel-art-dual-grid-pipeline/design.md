## Context

The existing `DualGridTextureTileSetGenerator` produces high-resolution terrain through a continuous scalar field, normalized pixel distance, four-times supersampling, smooth alpha coverage, material interpolation, and Bilinear sprite import. That contract is appropriate for the current 512-pixel `CartoonGrass` assets but conflicts with pixel art, where every output pixel, palette entry, and connectivity decision must be intentional.

The runtime mapping is already style-neutral: `DualGridMask` resolves sixteen corner states, `DualGridTileSet` stores the visual lookup, and `DualGridTilemap` generates the half-cell-offset output. This change therefore adds an editor-only sibling pipeline and leaves runtime code and high-resolution generated assets untouched.

## Goals / Non-Goals

**Goals:**

- Bake all sixteen Dual-Grid masks directly at an even native pixel resolution without antialiasing or resampling.
- Preserve exact source texels through repeated Point sampling and restrict generated colors to the source palettes plus explicitly configured edge colors.
- Make compatible borders pixel-identical through side sockets determined only by the two corner bits on that side.
- Keep masks `5` and `10` disconnected through an explicit transparent center block.
- Generate pixel-safe Sprite, Tile, TileSet, atlas, and machine-readable validation artifacts deterministically.
- Exercise the pipeline from the required Dual-Grid/project editor smoke surface without changing release scenes.

**Non-Goals:**

- Supporting low-resolution non-pixel-art textures or adding another continuous/coverage baker.
- Replacing, parameterizing, or regenerating the high-resolution `CartoonGrass` baker and assets.
- Hand-authoring a final production terrain style, runtime shader blending, tile variants, animated terrain, colliders, navigation, or release-battlefield adoption.
- Changing gameplay, persistence, content catalogs, platform adapters, or the `Bootstrap -> Lobby -> Battle -> Settlement` flow.

## Decisions

### Add a separate pixel profile and baker

`DualGridPixelTerrainProfile` records two opaque pixel source textures, an even tile size, integer outline and soil-rim widths, an opaque edge color, deterministic seed, terrain id, and output ownership. `DualGridPixelTileSetGenerator` is a separate editor class rather than a mode inside the high-resolution baker.

The two pipelines have contradictory invariants: the high-resolution baker requires supersampling and Bilinear filtering, while pixel art forbids both. A shared mode enum would spread conditional behavior through validation, sampling, composition, and import code and make accidental style regression more likely.

### Rasterize topology on the final integer grid

Every output pixel is classified once at its center. Empty and full masks are constant. Single-corner masks use a quarter-circle footprint that reaches the midpoint socket on its two sides; three-corner masks are the complement of the empty-corner footprint; adjacent pairs use an exact half-plane; opposite pairs use the union of two quarter-circle footprints.

This small marching-squares template family keeps the macro topology readable while avoiding continuous distance evaluation. Even tile sizes place each transition between the two central pixel rows or columns. Opposite-corner unions leave the central two-by-two block transparent.

### Treat each side as a two-bit pixel socket

After the interior is composed, each outer border is rewritten from the two corner bits that terminate that side. The socket owns occupancy and edge-band color along the whole border. Masks with the same side bits therefore emit the same RGBA sequence regardless of their other two corners.

Locking the border contract is preferred to rotating a few finished tiles because asymmetric source texels and edge colors need not be rotation-safe. The runtime TileSet remains an explicit sixteen-slot asset.

### Use integer morphology and palette-preserving composition

The land silhouette has binary alpha. For each occupied pixel, a bounded Chebyshev search finds its integer distance to transparent space using clamped border continuation. The closest band receives the configured edge color, the following band receives a Point-sampled soil texel, and the remaining surface receives a Point-sampled grass texel. Source coordinates repeat with integer modulo and are never scaled or interpolated.

The output palette is validated as a subset of the two opaque source palettes plus the configured edge color and transparent black. This allows authored pixel textures with multiple colors while preventing accidental blended colors.

### Use imagegen-owned sample art and generate only derived assets in code

The representative grass and soil source PNGs are created through the imagegen skill and stored as authoring inputs in the project. The editor command never draws or synthesizes fallback source art: it requires those files, imports them with pixel-safe settings, creates the profile when absent, and then generates sixteen mechanically derived PNG/Sprite/Tile assets plus a `DualGridTileSet`. Re-running the command updates owned derived files without replacing the imagegen sources or the profile.

Generated artifacts live under `Assets/DualGridDemo/PixelGrass`. The existing `CartoonGrass` folder and bake profile are not read or written.

### Validate native pixels and Unity import behavior

The baker validates all horizontal and vertical compatible pairs, binary alpha, palette membership, central separation, deterministic repeat output, TileSet slot assignment, Point filtering, disabled mipmaps, uncompressed import, Full Rect sprite mesh, Clamp wrapping, and tile-sized pixels per unit. It writes a native-resolution 4-by-4 atlas and JSON evidence.

The public validator is called from the existing Dual-Grid smoke entry so `FruitDefense.Editor.ProjectSetup.SmokeValidate` remains the aggregate acceptance surface. No demo scene or release build setting is changed.

## Risks / Trade-offs

- [Procedural quarter-circle templates may look too geometric for final art] -> Keep generated source textures and masks replaceable; a later change can add authored template inputs while retaining the same socket validator.
- [The same material phase repeats on every visual tile] -> Accept deterministic repetition for the pipeline sample; position-seeded variants or world-space fill require a separate runtime design.
- [Border rewriting can create a one-pixel change in contour direction near a socket] -> Validate at native scale and keep outline/rim widths small relative to tile size.
- [A very small tile cannot fit the configured edge bands] -> Require an even size of at least eight pixels and validate that the combined bands leave interior pixels.
- [Texture import platform overrides could reintroduce compression] -> Validate the effective importer contract in editor smoke and keep generated pixel assets outside release content until separately adopted.

## Migration Plan

1. Generate the sample grass/soil sources with imagegen, then add the profile, baker, and derived output in a new folder.
2. Generate and validate the representative PixelGrass TileSet without changing existing scene references.
3. Add the public pixel validation to the existing Dual-Grid smoke suite.
4. If a later release map adopts pixel terrain, open a separate change for scene/content migration and device acceptance.

Rollback removes the PixelGrass assets and the new editor-only classes; runtime data and existing high-resolution TileSets require no migration.

## Open Questions

- Final production pixel tile size, palette, hand-authored socket shapes, and terrain variants remain art-direction decisions.
- Whether production pixel terrain uses procedural masks or imports sixteen hand-authored templates remains a later choice; both can share this change's socket and importer validation contract.
