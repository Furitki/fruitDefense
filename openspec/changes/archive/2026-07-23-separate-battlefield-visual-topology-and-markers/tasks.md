## 1. Dependency and parity baseline

- [x] 1.1 Archive the completed `integrate-dual-grid-terrain-into-battlefield` change, confirm its main capability exists, and run strict OpenSpec validation before applying this delta
- [x] 1.2 Capture deterministic pre-migration fixtures for all three bundled maps covering grid dimensions, plantable cells, ordered routes, entry/exit/core, initial-pot groups, route descriptors/lengths, terrain surface masks, gameplay map identity, and representative battle outcomes

## 2. Layered map source and compiler

- [x] 2.1 Add versioned source definitions for semantic visual surfaces, gameplay cells, stable named routes, typed marker groups, and typed markers with stable IDs and no arbitrary property bag
- [x] 2.2 Add finite capability/collision identifier registries and compile validated source identifiers into deterministic runtime masks without Unity physics or presentation dependencies
- [x] 2.3 Implement the layered map compiler, immutable presentation/gameplay views, stable route/marker indexes, and structured validation results for dimensions, coverage, IDs, references, and marker combinations
- [x] 2.4 Implement current-execution-profile validation for exactly one primary enemy route with matching spawn, goal, core, endpoint, adjacency, and initial-pot capability rules
- [x] 2.5 Add the temporary legacy-to-layered adapter and compiled compatibility views for `IsPlantable`, `IsRoute`, route cells/nodes/descriptors, entry, exit, core, and initial-pot groups

## 3. Bundled catalog migration

- [x] 3.1 Migrate `orchard-01`, `orchard-02`, and `orchard-03` to layered sources using stable surface, capability, collision, `route.main`, marker-group, and marker identities
- [x] 3.2 Extend level theme/catalog source and compiled contracts with stable terrain-palette identities and layered map views while preserving existing level, map, wave-set, rule-set, and theme IDs
- [x] 3.3 Extend catalog validation for unknown surfaces/capabilities/collision channels, missing palettes, route/marker references, unsupported execution profiles, and gameplay/presentation separation
- [x] 3.4 Prove the migrated bundled maps match every pre-migration gameplay and terrain fixture, then remove duplicate live legacy authoring data so only the layered source remains canonical

## 4. Simulation, identity, and snapshot compatibility

- [x] 4.1 Resolve current enemy spawn, route goal, core, and initial flowerpots through compiled routes/markers while preserving exact initialization, route sampling, life loss, expansion, hit testing, and deterministic outcomes
- [x] 4.2 Implement canonical gameplay map fingerprinting over gameplay cells/collision, ordered routes, marker groups, markers, and gameplay references, with semantic surfaces/palettes/assets excluded
- [x] 4.3 Preserve bundled map IDs and supported snapshot restoration through the layered migration, including deterministic round-trip continuation and rejection when gameplay topology/markers differ
- [x] 4.4 Migrate remaining simulation/topology consumers off exclusive `BattlefieldCellRole` authoring and remove the obsolete exclusive-role source path after compatibility validation passes

## 5. Theme-owned terrain presentation

- [x] 5.1 Add `BattlefieldTerrainPalette` assets/registry that map stable semantic surface IDs to the existing soil base and generated Dual-Grid TileSets, and bind palette IDs through resolved level themes
- [x] 5.2 Change battlefield Dual-Grid mask resolution to consume semantic surface layout rather than plantable/route roles while retaining mask derivation, point filtering, half-cell alignment, clipping, and current migrated pixels
- [x] 5.3 Update `FruitDefenseGame`, project setup, and `Battle.unity` to resolve the active theme palette from the registered palette set without silently substituting another level's palette
- [x] 5.4 Resolve core and any visible gameplay marker positions through canonical marker cells and the shared `BattlefieldProjection`, preserving all render, interaction, and safe-area geometry

## 6. Validation and regression coverage

- [x] 6.1 Add focused compiler tests for layer-size mismatch, unknown IDs, duplicate IDs, invalid route continuity/endpoints, missing references, incompatible markers, invalid marker cells, multiple unsupported active routes, and presentation/gameplay independence
- [x] 6.2 Add deterministic tests proving presentation-only surface/palette changes keep gameplay fingerprints/outcomes stable while capability, collision, route, or gameplay-marker changes alter the fingerprint
- [x] 6.3 Extend bundled catalog, topology, snapshot, projection, terrain, and release-scene smoke validation across all three maps and include it in `FruitDefense.Editor.ProjectSetup.SmokeValidate`
- [x] 6.4 Run strict validation for this OpenSpec change and the aggregate main specs after all generated deltas and implementation updates are complete

## 7. Runtime build and visual acceptance

- [x] 7.1 Run Unity batch compilation, required project smoke, deterministic simulation, catalog, and snapshot suites with no regression from the recorded fixtures
- [x] 7.2 Build the ordinary WebGL release through `FruitDefense.Editor.WebBuild.Build` and confirm `Bootstrap → Lobby → Battle → Settlement` remains unchanged
- [x] 7.3 Capture and inspect all three bundled maps from a real WebGL canvas at the required portrait/safe-area matrix, verifying terrain parity, route/core/pot/entity readability, layer clipping, marker alignment, hit targets, and controls
- [x] 7.4 Record final acceptance evidence, confirm presentation-only changes do not alter deterministic results, and remove temporary migration fixtures/adapters that are not required for supported snapshot compatibility
