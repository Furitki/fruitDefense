## Why

The existing Dual-Grid terrain baker is intentionally optimized for high-resolution hand-painted textures through continuous distance fields, supersampling, antialiasing, and bilinear filtering. Pixel-art terrain needs a separate discrete pipeline so its topology, palette, opaque pixel clusters, and seams remain exact at native resolution.

## What Changes

- Add a pixel-art-specific Dual-Grid bake profile and editor baker that produce all sixteen masks directly on an integer pixel grid.
- Define stable side sockets for every pair of corner states so compatible mask borders are pixel-identical without continuous edge rewriting.
- Build the land silhouette, soil rim, shadow, and grass surface through integer mask operations with binary alpha and deterministic palette colors.
- Import generated sprites with Point filtering, no mipmaps, no compression, Full Rect meshes, and one tile-sized pixels-per-unit mapping.
- Generate a reusable `DualGridTileSet`, native-scale atlas evidence, and machine-readable validation for seams, palette, alpha, diagonal separation, determinism, and importer settings.
- Add imagegen-produced representative pixel-art grass/soil sources, a demo profile, and derived assets without replacing or modifying the existing high-resolution `CartoonGrass` baker and assets.
- Keep low-resolution non-pixel-art textures, gameplay adoption, persistence, combat rules, colliders, navigation, and release-scene flow outside this change.

## Capabilities

### New Capabilities

- `pixel-art-dual-grid-authoring`: Defines integer-grid pixel terrain generation, side-socket continuity, palette and alpha constraints, pixel-safe import settings, generated TileSet ownership, and editor validation.

### Modified Capabilities

None.

## Impact

- Adds editor-only pixel-art profile, baker, sample inputs/outputs, and validation under the existing Dual-Grid authoring area.
- Reuses `DualGridMask`, `DualGridTileSet`, and `DualGridTilemap`; runtime mask resolution and public APIs remain unchanged.
- Extends required editor smoke validation through the existing Dual-Grid validation entry while leaving release scenes and ordinary WebGL runtime content unchanged.
- Adds no package, platform-adapter, gameplay-state, save-data, backend, or content-catalog dependency.
