## 1. Discovery and preview data

- [x] 1.1 Add a cached editor-only discovery service that finds every `DualGridTileSet`, removes duplicates, sorts by asset path, invalidates on project changes, and supports explicit refresh.
- [x] 1.2 Add representative preview-source resolution for the four single-corner masks with a safe asset-preview fallback.

## 2. Inspector gallery workflow

- [x] 2.1 Render a responsive whole-layer TileSet card gallery beneath the existing object field with names, previews, selected state, invalid state, tooltips, and refresh control.
- [x] 2.2 Implement one-click valid TileSet assignment with Undo, immediate generated-layer rebuild, scene dirtiness, and unchanged manual paint state.

## 3. Validation and acceptance

- [x] 3.1 Extend `DualGridTilemapSmoke` to validate deterministic discovery, known generated TileSets, preview readiness, gallery assignment, and rebuilt output.
- [x] 3.2 Run focused/aggregate Unity editor smoke, visually inspect the live Inspector gallery, and run strict OpenSpec validation.
