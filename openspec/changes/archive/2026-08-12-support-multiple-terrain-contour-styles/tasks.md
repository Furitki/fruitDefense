## 1. Presentation identity and palette contracts

- [x] 1.1 Add stable square and organic contour identifiers plus presentation-only contour serialization/query support on visual cells
- [x] 1.2 Validate required landform contours, empty base-only contour fields, and unsupported connected/shared-vertex style mixtures without changing gameplay fingerprints
- [x] 1.3 Split palette registration into base surface, surface-plus-contour landform, and directed pair-plus-contour edge bindings with compatibility accessors only where required
- [x] 1.4 Decouple opaque-base UV scale and validation from an arbitrary landform `ReferenceTileSet`, allowing organic 32-pixel and square 256-pixel TileSets to coexist while enforcing per-TileSet dimensions and normalized socket compatibility

## 2. Runtime resolution and bundled migration

- [x] 2.1 Resolve landform and edge masks by exact surface plus contour identity while retaining the existing sixteen-mask bit order and projection
- [x] 2.2 Render every registered contour binding in stable order and reject missing contour-specific landform or edge assets without fallback
- [x] 2.3 Bump map and published-catalog schemas, migrate the three bundled battlefield visual cells and palette setup to explicit square contours, and prove gameplay fingerprint and deterministic fixture parity

## 3. Authoring workflow

- [x] 3.1 Add square/organic contour choices to the canonical map authoring data and terrain laboratory without exposing internal mask or TileSet concepts
- [x] 3.2 Make new gameplay-aligned landforms default to square and implement one-gesture connected-component contour changes or actionable refusal with Undo
- [x] 3.3 Disable unavailable contour/edge combinations, retain organic assets, and prevent silent contour, pair-direction, or edge substitution
- [x] 3.4 Replace separate pure-material presets with a contextual pure-only option on ordinary terrain brushes and present square brush previews in one row
- [x] 3.5 Bind contextual pure-only previews and writes to the selected brush's configured opaque endpoints, including mask `0/15` endpoints for the isolated full-composite trial
- [x] 3.6 Remove duplicate landform-only cards from the primary chooser, retaining only the two directed composition brushes while preserving low-level layered authoring compatibility
- [x] 3.7 Remove the `方形` / `自然` switch from the ordinary terrain laboratory while preserving each target's configured contour and low-level contour compatibility

## 4. Square hand-painted art pipeline

- [x] 4.1 Create a deterministic high-resolution square sixteen-mask topology guide and validator whose isolated-cell assembly is a rounded square and whose masks `5/10` remain disconnected
- [x] 4.2 Generate and retain imagegen source/provenance for one square grass-on-soil hand-painted transition using the approved reference and no scripted replacement artwork
- [x] 4.3 Package the accepted source into native-size square landform/edge sprites, lock compatible sockets without restoring the legacy bilinear silhouette, and register the new TileSets
- [x] 4.3a Supply and register a square stone-road landform TileSet required by the bundled-map migration; polished pair-edge art remains scoped to grass on soil
- [x] 4.4 Produce assembled square, organic, and coexistence boards at editor and real Battle scale for visual review
- [x] 4.5 Preserve the retained ribbon's real grass-lip variation and replace the binary outer-shadow cutoff with a deterministic translucent falloff without changing topology or sockets

## 5. Validation and release evidence

- [x] 5.1 Add focused smoke coverage for serialization, palette keys, mask resolution, one-cell square bounds, strips, turns, holes, diagonals, style compatibility, Undo, and no fallback
- [x] 5.2 Run strict validation for this change and all OpenSpec changes, then run `FruitDefense.Editor.ProjectSetup.SmokeValidate` with retained logs
- [x] 5.3 Build ordinary WebGL and capture a real portrait Battle showing readable square hand-painted terrain plus retained organic evidence and unchanged scene flow
- [x] 5.4 Record final hashes, artifact dimensions, gameplay parity, visual findings, and any remaining limitations in change verification evidence
- [x] 5.5 Add hard-edge regression metrics, regenerate the acceptance boards, and rerun focused, aggregate, and strict OpenSpec validation

## 6. Top-down edge visual correction

- [x] 6.1 Replace the side-view soil-wall/contact/shadow packaging contract with a narrow direction-neutral grass feather over the existing soil base, and make the square grass interior use its registered seamless texture
- [x] 6.2 Add native and 46/72-pixel display-scale validation that rejects dark contact strokes, opaque outside-soil paint, second outer contours, and mipmap-free minification
- [x] 6.3 Regenerate the sixteen square landform/edge sprites and review boards, then confirm isolated cells, strips, turns, holes, and diagonals remain seamless and read as top-down ground
- [x] 6.4 Rerun focused terrain smoke, aggregate project smoke, ordinary WebGL acceptance where available, and strict OpenSpec validation; retain updated evidence and findings

## 7. Authorized production brush installation

- [x] 7.1 Import the A grass-soil and B stone-water Runtime32 families byte-for-byte into versioned production asset folders with retained source manifests and deterministic Sprite settings
- [x] 7.2 Build complete mask-00..15 TileSets, bind their opaque endpoints as the four base surfaces, and register exact square refined edges for grass-on-soil and stone-on-water
- [x] 7.3 Expose both semantic combinations as one-click canonical map editor brush presets without generating pixels or silently inventing a water landform
- [x] 7.4 Add aggregate smoke coverage for source provenance, all sixteen masks, import settings, endpoint identity, exact palette bindings, reverse complementation, and editor availability
- [x] 7.5 Run the production brush installer, focused and aggregate Unity smoke, strict OpenSpec validation, and ordinary WebGL acceptance; retain logs while leaving agent visual inspection false

## 8. Reusable brush-package authoring integration

- [x] 8.1 Add pipeline-owned `BrushImport.json` metadata and profile validation so future candidates carry all Unity registration semantics
- [x] 8.2 Replace the A/B-specific installer with an idempotent generic candidate importer and persistent `TerrainBrushDefinition` registry assets
- [x] 8.3 Merge registered definitions into palette setup and make the canonical map editor enumerate the registry instead of hard-coded production shortcuts
- [x] 8.4 Register the same definitions in one terrain-laboratory preview gallery, show every registered composition and its available directions simultaneously, support valid one-direction pairs such as stone-on-water, and refuse unsafe non-empty target reinterpretation
- [x] 8.5 Add focused pipeline/import/palette/editor/laboratory smoke coverage, update the single pipeline document, and run strict validation without an unnecessary release build

## 9. Registered-brush clarity correction

- [x] 9.1 Make runtime tile size descriptor-owned, add deterministic Runtime64 repackaging from unchanged Review256 masks, and retain Runtime32 only for the fixed stress atlas
- [x] 9.2 Update the generic Unity importer and persistent brush definitions to configure and validate the declared runtime size while replacing obsolete same-brush runtime folders safely
- [x] 9.3 Center a square artwork area inside every unified-gallery card and add mechanical aspect-ratio/layout coverage
- [x] 9.4 Repackage and reinstall both authorized brushes, run pipeline and focused Unity smoke plus strict OpenSpec validation, retain evidence, and perform no agent visual review
