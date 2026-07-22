## 1. Profile and authoring controls

- [x] 1.1 Allow zero outline width, add bounded texture-guidance width to `DualGridPixelTerrainProfile`, and preserve backward-compatible configuration entry points.
- [x] 1.2 Expose zero-outline and texture-guidance settings in the generic pixel terrain wizard and update wizard validation/smoke fixtures.

## 2. Texture-guided native rasterization

- [x] 2.1 Add deterministic native-grid luminance guidance to `PixelSource` using the existing Point-sampling phase and a bounded local filter.
- [x] 2.2 Replace quarter-circle land masks with normalized bilinear corner-field classification, bounded texture guidance, and explicit opposite-corner separation.
- [x] 2.3 Make canonical mixed-side socket cuts texture-guided while preserving pixel-identical compatible borders and corner ownership.
- [x] 2.4 Skip synthesized edge-color composition when outline width is zero while preserving soil-rim and explicit-outline behavior.

## 3. Validation and evidence

- [x] 3.1 Extend pixel validation with active-palette rules, exact opposite-corner component counts, bounded guidance-change measurement, and flat-source fallback handling.
- [x] 3.2 Extend profile-specific JSON evidence and editor smoke assertions for texture guidance, outline activity, seams, determinism, importers, and TileSet assignments.

## 4. Sample migration and acceptance

- [x] 4.1 Migrate PixelGrass and StoneFloor profiles to zero outline with bounded guidance and rebake only their owned generated assets and evidence.
- [x] 4.2 Run focused pixel terrain validation, `FruitDefense.Editor.ProjectSetup.SmokeValidate`, and OpenSpec validation; record the final results.
