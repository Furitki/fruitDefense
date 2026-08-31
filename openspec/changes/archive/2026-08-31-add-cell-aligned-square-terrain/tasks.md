## 1. Trial Art And Import

- [x] 1.1 Generate clean hand-painted grass and soil square base textures from the approved palette reference and store versioned prompt/provenance records with the trial assets.
- [x] 1.2 Add deterministic Unity import setup for opaque Repeat-wrapped trial textures and a trial-only palette without mutating release bindings.
- [x] 1.3 Add a stable editor generator for a battlefield-scale comparison board containing repeated pure-square grass/soil and a spatially separated Dual-Grid reference.

## 2. Authoring And Compatibility

- [x] 2.1 Add named grass-square and soil-square preset selection to the canonical battlefield map editor, using the existing base-only cell representation and clearing all optional layer identifiers.
- [x] 2.2 Add compiler validation that rejects edge or vertex contact between base-only and Dual-Grid uses of the same surface while allowing disconnected uses and unlike square surfaces.

## 3. Automated Verification

- [x] 3.1 Add focused editor smoke coverage for square preset semantics, invalid edge/diagonal representation mixing, valid disconnected regions, and valid unlike square neighbours.
- [x] 3.2 Add trial-art validation for opacity, Repeat wrapping, opposite-edge continuity, trial isolation, and deterministic comparison-board output.
- [x] 3.3 Run the focused square-terrain smoke and the aggregate `FruitDefense.Editor.ProjectSetup.SmokeValidate` suite.

## 4. Runtime And Visual Acceptance

- [x] 4.1 Generate and visually review the real-cell-scale comparison board for seams, coarse hand-painted texture, palette harmony, and clear separation between square and Dual-Grid regions.
- [x] 4.2 Run the ordinary WebGL build and portrait runtime smoke to confirm the isolated trial introduces no release presentation or safe-area regression.

## 5. Approved In-Game Grid Production

- [x] 5.1 Store the approved in-game square-grid preview as review-only evidence and record its prompt, source hash, crop rectangles, scaling method, and output hashes.
- [x] 5.2 Replace the rejected trial textures with deterministic `64 × 64` grass and soil cell exports preserving the approved soft inset frame, palette, and broad texture.
- [x] 5.3 Update the isolated scene and smoke coverage to present the exact `8 × 7` game-grid composition with a `7 × 5` grass region plus separated Dual-Grid reference.
- [x] 5.4 Render and inspect the real-cell-scale evidence, then run focused terrain validation, aggregate editor smoke, ordinary WebGL build, and isolated trial WebGL build without changing release bindings.
