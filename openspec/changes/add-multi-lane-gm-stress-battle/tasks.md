## 1. Canonical multi-route execution

- [x] 1.1 Extend the compiled battlefield aggregate with validated named route lookup, explicit primary-route identity, and standard versus GM topology profiles.
- [x] 1.2 Add required route identity to live enemies and update movement, targeting positions, projectiles, feedback anchors, and rendering to use route-aware map helpers.
- [x] 1.3 Make standard wave spawning assign the validated primary route explicitly and include route identity in deterministic gameplay checksums.
- [x] 1.4 Add focused deterministic tests for simultaneous lanes, invalid route IDs, standard single-route parity, and equivalent frame partitions.

## 2. GM stress simulation and lifecycle

- [x] 2.1 Add a development-only 8-by-7 GM battlefield factory with eight vertical route/spawn/goal pairs and sixteen bottom-row plant pots.
- [x] 2.2 Add an explicit GM battle-session mode that disables automatic waves, core/lives failure, victory, settlement, result submission, snapshot export, and snapshot restore.
- [x] 2.3 Implement deterministic per-lane FIFO enemy queues, four enemy selectors, batch sizes 1/10/50, all-lanes fan-out, fixed-step draining, escaped count, and the 500-unit active-plus-pending cap.
- [x] 2.4 Implement unlimited one-star placement/replacement for all five bundled plants on the sixteen GM pots with no economy or inventory side effects.

## 3. GM runtime presentation and access

- [x] 3.1 Add a modular GM controller/presenter that reuses approved fonts, action/card states, feedback, and one safe-area-derived draw/hit layout.
- [x] 3.2 Present eight independent spawn pads, enemy/plant/batch selection, all-lanes action, active/pending/escaped/cap metrics, and pause/speed controls without standard economy or settlement surfaces.
- [x] 3.3 Add the stable `Fruit Defense/Playtest/GM 压力测试关` Editor launch workflow without adding GM data to production Resources or released catalogs.
- [x] 3.4 Add a separate Development WebGL build and GM entry route that leaves the normal `Builds/WebGL` output and release route unchanged.

## 4. Isolation and automated validation

- [x] 4.1 Add Editor validation for eight route/marker pairs, sixteen pots, queue ordering/fan-out/cap, free plant replacement, no-terminal behavior, and snapshot/result rejection.
- [x] 4.2 Add isolation validation proving the GM fixture is absent from released level catalogs, publication manifests, production Resources, profile selection, and the normal WebGL build path.
- [x] 4.3 Run focused tests, compilation, and `FruitDefense.Editor.ProjectSetup.SmokeValidate`, fixing all regressions attributable to this change.

## 5. WebGL visual and load acceptance

- [x] 5.1 Build the separate Development WebGL artifact and launch its explicit GM route on a real portrait canvas.
- [x] 5.2 Capture supported safe-area geometry and staged 1/10/50/high-density evidence showing lane alignment, independently operable controls, live counts, and no clipping.
- [x] 5.3 Record responsiveness, frame pacing, allocation behavior, and the bounded 500-unit result, and resolve any acceptance failure before completing the change.

## 6. Registered tile-brush terrain and republish

- [x] 6.1 Author the GM visual cells as soil base plus square refined grass landform on the bottom two rows, using the existing production grass/soil brush contract without changing gameplay topology.
- [x] 6.2 Extract one shared layered-terrain GUI renderer, require the registered orchard palette in GM composition, and remove the GM solid-color terrain path without adding a fallback.
- [x] 6.3 Add focused validation for the exact brush binding, map visual identities, shared-renderer use, palette failure behavior, and unchanged GM projection/topology.
- [x] 6.4 Run the GM aggregate and project smoke, rebuild the separate Development WebGL artifact, republish it, and capture a real portrait canvas showing the tile-brush terrain.

## 7. Plant deployment and combat regression correction

- [x] 7.1 Record drag-only GM deployment parity and shared plant-combat execution in the change specification and design.
- [x] 7.2 Replace click-to-place GM controls with unlimited plant drag sources using the normal drag threshold, preview overlap, target cue, cancellation, and release semantics.
- [x] 7.3 Correct route-length-dependent combat distance calibration and add regressions proving all four damage plants attack, the producer ability advances, and tap/missed-drop commands are atomic.
- [x] 7.4 Run focused and aggregate validation, rebuild the Development WebGL artifact, and republish the corrected GM route locally and externally without changing the normal release artifact.

## 8. Shared GM combat presentation correction

- [x] 8.1 Replace the GM yellow-square/generic-outline combat path with the normal battle's shared allocation-free plant-motion, projectile, effect, and status renderer backed by the existing combat atlas.
- [x] 8.2 Make the GM combat atlas a required validated dependency and add regressions for every bundled plant's event-to-visual route, including producer activation.
- [x] 8.3 Run focused and aggregate validation and confirm bounded presentation behavior under GM combat density.
- [x] 8.4 Rebuild and republish the Development WebGL GM route locally and externally while proving the normal release artifact remains unchanged.
