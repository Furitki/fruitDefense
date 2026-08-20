## 1. Shared interaction foundation

- [x] 1.1 Add validated semantic motion amplitude/timing tokens and an allocation-free unscaled-time evaluator for press, pop, fade-slide, and stagger samples.
- [x] 1.2 Add the authoritative IMGUI press tracker with pointer capture, drag suppression, cancellation, stable control IDs, and focused lifecycle tests.
- [x] 1.3 Update the production RuntimeUiTheme asset and validation so new motion values and reduced-motion policy have one authoritative configuration.

## 2. Shared drawing and route application

- [x] 2.1 Extend shared action/card/metric/status drawing helpers to animate visual rectangles and opacity while retaining the original hit geometry.
- [x] 2.2 Apply bounded route reveal, card selection, and Start press/pop feedback to Lobby; remove its duplicate pointer-press implementation.
- [x] 2.3 Apply hierarchical result/metric/action reveal and shared press feedback to Settlement; remove its duplicate pointer-press implementation.
- [x] 2.4 Apply local shared pop/press samples to Battle status, selection, resource, wave, and modal action feedback without changing its board drag owner or simulation.

## 3. Resource and documentation contract

- [x] 3.1 Record the analyzed APK identity, extraction status, candidate resource rules, and provisional semantic-slot replacement contract without importing protected payloads.
- [x] 3.2 Update the runtime UI guide with motion language, restraint, reduced-motion, ownership, and reference-resource provenance rules.

## 4. Verification and evidence

- [x] 4.1 Add editor smoke coverage for motion endpoints/checkpoints, replay cancellation, reduced-motion equivalence, press release/drag cancel, and unchanged authoritative hit rectangles.
- [x] 4.2 Run `FruitDefense.Editor.ProjectSetup.SmokeValidate` and the focused UI/editor suites; fix all failures attributable to this change.
- [x] 4.3 Build ordinary WebGL with `FruitDefense.Editor.WebBuild.Build` and capture live 402×874 Lobby, Battle, and Settlement motion/resting evidence plus a representative desktop host view.
- [x] 4.4 Compare the canonical before/after evidence and record measurable improvements, remaining limits, build identity, and whether any reference-derived raster entered production.
