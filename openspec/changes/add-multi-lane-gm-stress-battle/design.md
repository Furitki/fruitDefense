## Context

The battle runtime currently compiles one primary route into the canonical battlefield map and lets every enemy derive its world position from that route. That is sufficient for the three released single-route levels, but it cannot provide trustworthy lane-specific spawning, movement, targeting, effects, floating text, or performance evidence for a Plants-vs.-Zombies-style stress surface.

This change crosses core deterministic simulation, battle composition, immediate-mode runtime UI, editor tooling, and WebGL acceptance. It must preserve the released level catalog and the approved UI visual system while ensuring the GM surface cannot leak into player progression, settlement, production Resources, or the normal WebGL artifact.

## Goals / Non-Goals

**Goals:**

- Make named route identity a first-class part of live enemy state and canonical position resolution.
- Run eight true vertical routes concurrently, with one top spawn pad and one independently addressable spawn queue per lane.
- Expose a development-only, no-failure GM battle with two full bottom plant rows, unlimited plant placement, bounded manual enemy generation, and useful live load metrics.
- Reuse released combat content, feedback, fonts, art, controls, projection, and deterministic fixed-step execution instead of building a fake visualization.
- Keep draw and hit geometry on the same safe-area-derived layout path and keep the normal release flow unchanged.
- Validate the feature through focused Editor tests, aggregate smoke, and a separate Development WebGL canvas run.

**Non-Goals:**

- Shipping a multi-lane player level or changing the three bundled levels.
- Adding same-lane-only targeting. Plants retain their current range/radius targeting and may attack enemies on neighboring lanes.
- Adding GM rewards, economy, inventory, merge, equipment, persistence, snapshot restore, settlement, or profile progression.
- Adding new plant/enemy content, a GM-specific visual theme, or new production ArtSet slots.
- Claiming Douyin or WeChat support from the Development WebGL result.

## Decisions

### 1. Canonical maps expose named route collections; live enemies require a route ID

The compiled battlefield aggregate will own a validated dictionary/list of named routes plus a primary route identifier for standard-level conveniences. Every spawned enemy will receive a non-empty route ID, and movement, world-position lookup, presentation events, targeting, and checksum generation will resolve through that identity.

The current three levels remain on a strict standard profile that accepts exactly one primary route. The GM map uses a separate validation profile that requires eight named cardinal routes, each with its own spawn and goal marker. This keeps publication rules explicit instead of weakening them for a development fixture.

Alternatives considered:

- Presentation-only horizontal offsets were rejected because simulation, projectiles, effects, and feedback would still operate on one route.
- Eight independent simulations were rejected because cross-lane targeting, shared feedback load, and whole-frame performance would no longer match one real battle.
- Inferring lanes from enemy coordinates was rejected because identity would be unstable at intersections and would not participate cleanly in deterministic state.

### 2. Route identity is runtime state, but GM snapshots are unsupported

Route ID participates in deterministic checksums and is preserved by all in-memory state transitions. Standard snapshots do not need a new route field because a standard session is validated to contain exactly one route and restore derives the only valid route from the resolved level. GM sessions explicitly reject snapshot export and restore, result submission, and resume tokens.

This avoids broadening persistence for a tool-only session while still making live multi-route execution deterministic. No compatibility reader or legacy fallback is added.

### 3. GM execution is an explicit battle-session mode

Battle launch composition will use an explicit `Standard` or `GmStress` mode rather than query-string side effects hidden inside the standard session. Standard mode preserves wave spawning and normal terminal states. GM mode disables automatic waves, victory, defeat, core damage, and settlement; enemies reaching their goal are removed and counted as escaped.

The GM controller owns deterministic per-lane FIFO spawn queues. A click enqueues the selected enemy type with batch size 1, 10, or 50. The all-lanes action enqueues that batch for every lane. The controller drains queues at a fixed-step cadence and enforces a combined active-plus-pending cap of 500 so input cannot create unbounded allocations or freeze WebGL.

Alternatives considered:

- Reusing synthetic waves was rejected because it couples the tool to victory/defeat and makes per-lane manual load shaping awkward.
- Allowing unlimited immediate instantiation was rejected because the control itself could stall a frame before the system under test runs.

### 4. The GM battlefield is generated as development composition, never release content

A small development-only factory builds an 8 x 7 cardinal grid in code:

- columns 0-7 are lane identities;
- rows 0-4 form eight vertical enemy routes from top spawn to goal;
- rows 5-6 are sixteen plantable pots;
- each route is named deterministically from its column.

The factory reuses bundled enemy, plant, theme, and combat definitions but does not register a level in `PlayableLevels`, production `Resources`, map publication manifests, profile state, or settlement. Runtime GM code is compiled only for the Editor or a Development Build.

Alternatives considered:

- A production map asset was rejected because it can be accidentally published or discovered by the normal catalog.
- An Editor-only simulation was rejected because the requirement includes a real Development WebGL canvas stress run.

### 5. GM controls reuse the existing battle visual language

The immediate-mode GM presenter derives all draw and hit rectangles from one safe-area layout model. The top edge contains eight lane spawn pads. The existing five selector-card pattern is reused for unlimited plants, while four enemy selectors, the `1/10/50` batch selector, and the all-lanes command use approved Secondary/Quiet action roles. Selected state includes a border/marker in addition to color.

The header shows active, pending, escaped, and cap values. Pause and speed controls remain available. Economy, refresh, inventory, merge, equipment, wave, life, result, and reward surfaces are absent rather than disabled. Existing packaged Chinese fonts and gameplay sprites are reused; no numeric texture atlas is introduced for these low-frequency GM labels.

### 6. Access and builds are deliberately separate

`Fruit Defense/Playtest/GM 压力测试关` launches the GM session in the Editor through an explicit one-shot development launch request. A separate editor build command creates a Development WebGL artifact outside `Builds/WebGL`, with development scripting enabled and the GM entry route available. The normal `FruitDefense.Editor.WebBuild.Build` output and release route remain unchanged.

The Development WebGL acceptance records a real portrait canvas capture plus load observations at representative batches. It is evidence for ordinary WebGL only.

### 7. Verification focuses on topology, determinism, isolation, and bounded load

Focused tests will verify eight distinct route IDs, spawn/goal pairing, sixteen plant pots, per-lane movement, cross-lane world-position correctness, queue ordering, all-lane fan-out, the 500-unit cap, free replacement placement, no terminal state, and rejection of snapshot/result paths. An isolation test will verify the GM level is absent from released catalogs and resources. The aggregate Editor smoke remains the final automated gate before WebGL acceptance.

### 8. GM terrain reuses the registered production grass/soil brush

The GM map authors every cell with soil as its opaque base. Rows 5 and 6 additionally author grass as a square landform with the refined edge style, so the existing `terrain-brush.grass-on-soil` sixteen-mask composition resolves the boundary and the full grass interior. The GM presenter receives the already registered orchard terrain palette from the loaded Battle scene and renders it through the same shared layered-terrain GUI renderer as the standard battle.

The dependency is required and validated during GM initialization. A missing palette, wrong palette identity, unavailable square grass-on-soil edge, or invalid mask set fails the GM session explicitly; there is no solid-color terrain fallback, duplicated brush asset, direct asset-path load, or runtime pixel generation. Spawn-pad outlines and grid guidance remain overlays above the terrain and do not change draw/hit geometry.

### 9. GM plant deployment and combat stay on the normal battle contracts

The five unlimited GM plant cards are drag sources, not click-to-place mode selectors. They use the normal battle's activation threshold, drag preview overlap, target cue, cancellation, and release semantics. A successful GM drop still performs the tool-only free replace command because the normal merge/swap economy is intentionally absent, but a tap or a bare pot click never deploys a plant.

Combat execution is not specialized for GM. A deployed plant resolves the same compiled ability loadout and advances through `GameSimulation`'s fixed-step ability runtime. Legacy combat distances are calibrated from map units per cell and the canonical standard-route scale, not from the active route's total length; otherwise the deliberately short four-segment GM lanes collapse every attack range below one cell.

## Risks / Trade-offs

- [Risk] Existing code may access the former singular route directly and silently place a multi-lane enemy on the primary route. → Replace direct accesses with route-aware map helpers and add tests that place simultaneous enemies on different columns.
- [Risk] Adding route IDs changes deterministic checksums and may invalidate old local evidence. → Regenerate deterministic baselines; do not add compatibility checksum paths.
- [Risk] Five hundred enemies plus feedback can overload allocations or overdraw in WebGL. → Pool/reuse existing runtime presentation paths, drain bounded queues over fixed steps, aggregate GM metrics, and measure real canvas behavior at staged loads.
- [Risk] Development entry code could become reachable in a release build. → Gate runtime composition by Editor/Development compilation and test absence from normal catalogs and the standard WebGL build path.
- [Risk] The compact portrait layout may make eight spawn pads hard to hit. → Use the full board width, derive hit and draw rectangles together, and validate supported safe-area viewports with minimum practical target geometry.
- [Risk] GM-only terrain drawing could drift from the released battle renderer or silently fall back to debug colors. → Extract one shared GUI terrain renderer, require the registered orchard palette at composition time, and validate the exact square grass-on-soil binding before launch.
- [Risk] A short GM route can shrink legacy range/speed conversion and make correctly loaded abilities appear inert. → Calibrate legacy distance per map cell, assert standard-map parity, and execute real damage/producer abilities in the GM regression suite.
- [Risk] Click-to-place GM controls can conceal regressions in the normal drag interaction. → Reuse the shared drag geometry and require drag-only deployment tests, including tap and missed-drop atomicity.
- [Trade-off] Plants may attack across adjacent lanes. This intentionally preserves current combat semantics; the GM level tests multi-route movement and density rather than introducing a new targeting game rule.

## Migration Plan

1. Extend canonical map and enemy state to require route identity, then update all standard spawn paths to use the validated primary route explicitly.
2. Replace singular route position reads with route-aware helpers and update deterministic checksum coverage.
3. Add the isolated GM factory, session mode, commands, and controller.
4. Add the GM presenter, Editor launch command, and Development WebGL build path.
5. Run focused tests and aggregate smoke, then perform real Development WebGL canvas acceptance.

Rollback removes the GM development modules and reverts the route-collection change as one unit. There is no player data migration because GM sessions never persist, and released levels remain single-route.

## Open Questions

None. The initial accepted baseline is eight lanes, rows 0-4 for travel, rows 5-6 for plants, batch sizes 1/10/50, four enemy selectors, five plant selectors, and a 500-unit active-plus-pending cap.
