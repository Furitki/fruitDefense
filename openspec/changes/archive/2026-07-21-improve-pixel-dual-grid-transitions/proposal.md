## Why

The native pixel Dual-Grid baker currently hard-codes quarter-circle corner silhouettes and a mandatory solid outline, so generated terrain looks mechanically rounded and visually separated from whatever ground is rendered beneath it. The authoring pipeline needs a deterministic, pixel-safe transition shape that follows source-texture structure while preserving the existing sixteen-mask and exact socket contracts.

## What Changes

- Replace fixed quarter-circle silhouettes with bounded texture-guided contours derived on the final native pixel grid.
- Make the outer outline optional and default new profiles to a connection-safe no-outline edge while retaining explicit outlines for terrain that needs a hard boundary.
- Preserve binary alpha, source-palette-only composition, opposite-corner separation, deterministic output, and pixel-identical compatible borders.
- Expose and validate the new contour and edge settings through the generic pixel terrain profile and wizard.
- Extend evidence and smoke validation to prove contour variation, no-outline output, seam compatibility, and deterministic rebakes across the existing PixelGrass and StoneFloor profiles.
- Keep gameplay, persistence, release scenes, runtime route flow, and platform adapters unchanged.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `pixel-art-dual-grid-authoring`: Generated native-pixel silhouettes become texture-guided and the configured solid outline becomes optional without weakening topology, palette, determinism, or socket guarantees.
- `generic-pixel-terrain-authoring`: The wizard exposes valid contour guidance and optional-outline settings and validates the resulting profile-specific evidence.

## Impact

- Affects `DualGridPixelTerrainProfile`, `DualGridPixelTileSetGenerator`, the pixel terrain wizard, their editor smoke coverage, generated PixelGrass/StoneFloor assets, and profile-specific validation evidence.
- Does not change `DualGridMask`, `DualGridTileSet`, `DualGridTilemap`, gameplay simulation, saved data, release scene order, or WebGL platform behavior.
- Required aggregate validation remains `FruitDefense.Editor.ProjectSetup.SmokeValidate`; no new runtime dependency or external package is introduced.
