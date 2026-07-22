## 1. Pixel profile and sample inputs

- [x] 1.1 Add `DualGridPixelTerrainProfile` with opaque source, even-size, integer-band, palette, ownership, and validation rules.
- [x] 1.2 Generate PixelGrass grass/soil source textures with the imagegen skill and add idempotent profile creation that never draws or overwrites source art.

## 2. Discrete terrain generation

- [x] 2.1 Implement final-grid sixteen-mask topology for empty, full, single, adjacent, opposite, and three-corner states.
- [x] 2.2 Implement integer edge/rim morphology and modulo Point sampling with binary alpha and palette-preserving output.
- [x] 2.3 Implement two-bit canonical border sockets and explicit central separation for masks `5` and `10`.

## 3. Unity assets and evidence

- [x] 3.1 Write deterministic PNGs and configure pixel-safe Sprite import settings for all masks and sample sources.
- [x] 3.2 Create/update sixteen Tile assets and the PixelGrass `DualGridTileSet` with stable slot assignments.
- [x] 3.3 Emit a native-resolution sixteen-mask atlas and JSON report with profile and pixel hashes.

## 4. Validation and acceptance

- [x] 4.1 Validate compatible RGBA borders, binary alpha, palette membership, full/empty masks, opposite-corner centers, and deterministic repeat pixels.
- [x] 4.2 Validate generated importer settings and TileSet assignments, and expose a public generated-asset validator.
- [x] 4.3 Call pixel validation from the existing Dual-Grid smoke entry without changing release scenes or the high-resolution baker.
- [x] 4.4 Generate the PixelGrass sample, inspect native-scale evidence, run Dual-Grid/project Unity smoke validation, and validate the OpenSpec change.
