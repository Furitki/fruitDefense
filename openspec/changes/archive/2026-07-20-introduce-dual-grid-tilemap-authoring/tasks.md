## 1. Runtime mask and configuration

- [x] 1.1 Add the named four-corner mask enum and deterministic resolver for all sixteen states.
- [x] 1.2 Add the serializable sixteen-slot `DualGridTileSet` asset with required-mask validation.

## 2. Tilemap generation

- [x] 2.1 Implement safe full rebuilds from occupied logical bounds into a distinct generated output Tilemap.
- [x] 2.2 Implement half-cell output alignment, local four-vertex refresh, and logical-cell mutation APIs.
- [x] 2.3 Implement opt-in edit-mode source-signature detection without changing unrelated Tilemaps.

## 3. Authoring and demo

- [x] 3.1 Add custom inspectors that label mask slots, report configuration problems, and expose align/rebuild actions.
- [x] 3.2 Add procedural placeholder tiles plus an idempotent setup command for `Assets/Scenes/DualGridDemo.unity`.
- [x] 3.3 Generate the demo assets/scene and keep the demo outside release build settings.
- [x] 3.4 Document logical painting, layer reuse, final-art replacement, and generated-output ownership from the README engineering entry point.

## 4. Validation and evidence

- [x] 4.1 Add Dual-Grid editor smoke coverage for sixteen masks, bounds, validation, alignment, rebuild, source/output safety, and incremental updates.
- [x] 4.2 Wire Dual-Grid validation into `FruitDefense.Editor.ProjectSetup.SmokeValidate` and run the required editor smoke suite.
- [x] 4.3 Render and inspect visual evidence of the demo pattern and confirm release scene order is unchanged.
- [x] 4.4 Build the ordinary WebGL baseline to verify the new runtime code introduces no release regression.

## 5. Production-quality terrain edge pipeline

- [x] 5.1 Add an editor-only reusable terrain bake profile for material sources, output ownership, supersampling, pixel edge widths, deterministic contour, and tuft controls.
- [x] 5.2 Replace scalar-width smoothing with normalized pixel-distance evaluation and four-times subpixel integration while preserving exact compatible borders.
- [x] 5.3 Add deterministic grass-tuft protrusions, exposed-soil material separation, and disconnected topology for opposite-corner masks.
- [x] 5.4 Add the generated soil base Tile and lower demo Tilemap, and keep the ready-to-use Scene-view manual paint/erase workflow.
- [x] 5.5 Regenerate assets, validate compatible seams/topology/import settings, render visual evidence, run Dual-Grid/project smoke validation, and update the README workflow.
