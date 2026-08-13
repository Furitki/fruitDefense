## 1. Visual-cell source and compatibility

- [x] 1.1 Add the versioned base-plus-optional-landform visual-cell source, compiled queries, finite identifiers, and structured validation
- [x] 1.2 Preserve `SurfaceAt` compatibility while excluding base, landform, and edge changes from gameplay map identity and snapshot authority
- [x] 1.3 Migrate `orchard-01`, `orchard-02`, and `orchard-03` to soil bases plus their existing grass/stone landforms with no refined edge and prove gameplay parity

## 2. Terrain palette and runtime composition

- [x] 2.1 Replace the single soil/overlay palette contract with reusable material bindings for opaque bases and transparent landform TileSets
- [x] 2.2 Add exact directed foreground/background/style edge bindings with strict validation and no reverse or default substitution
- [x] 2.3 Render cell-aligned bases, vertex-aligned landforms, and optional edge outputs in deterministic order with existing projection and clipping
- [x] 2.4 Preserve interaction feedback, route/core/entity readability, safe-area geometry, and current release scene/theme palette resolution

## 3. Layered authoring workflow

- [x] 3.1 Add an authoring component and canonical editable state for one base plus one optional landform and edge choice per cell
- [x] 3.2 Add pure-base, landform-only, and ordered-pair brush operations, including A on B, B on A, erase, Undo, and affected-cell refresh
- [x] 3.3 Add explicit edge-style selection with disabled reasons for unavailable or reversed pairs and validation for invalid mixed components
- [x] 3.4 Extend the non-release Dual-Grid demo and Inspector guidance without changing release build scene order

## 4. Art assets and second-pass edges

- [x] 4.1 Produce two compatible seamless base materials and transparent landform TileSets with shared native size, pivot, sampling, and mask sockets
- [x] 4.2 Produce optional second-pass AI-refined edge assets for both ordered pair directions while preserving protected topology sockets
- [x] 4.3 Import and bind base, landform, and edge assets without runtime raster processing, and retain the final generation/edit prompts with the evidence
- [x] 4.4 Assemble and inspect a seam board covering full, empty, straight, concave, convex, island, hole, and masks 5/10 cases with edge on and off

## 5. Automated validation

- [x] 5.1 Add focused compiler tests for coverage, unknown surfaces, missing bases, unsupported stacks, edge-without-landform, missing directed pairs, and presentation identity
- [x] 5.2 Add authoring tests for all brush modes, pair reversal, pure-base clearing, Undo-safe mutation, generated-output ownership, and incremental refresh
- [x] 5.3 Extend terrain, catalog, project setup, and release-scene smoke validation across all three bundled maps and the layered demo
- [x] 5.4 Run strict OpenSpec validation, Unity batch compilation, focused smoke suites, and `FruitDefense.Editor.ProjectSetup.SmokeValidate`

## 6. WebGL visual acceptance

- [x] 6.1 Build the ordinary WebGL release through `FruitDefense.Editor.WebBuild.Build` and preserve `Bootstrap → Lobby → Battle → Settlement`
- [x] 6.2 Capture a real portrait WebGL canvas showing base-only terrain, both pair directions, optional edge on/off, diagonal topology, and readable gameplay layers
- [x] 6.3 Inspect the captures for seams, sampling fringe, clipping, alignment, controls, safe area, and player-readable affordances and record final evidence
- [x] 6.4 Run strict change and aggregate OpenSpec validation after implementation and leave the change ready for archive
