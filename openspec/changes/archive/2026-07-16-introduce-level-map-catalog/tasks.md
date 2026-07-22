## 1. P0 dependency gate

- [x] 1.1 Confirm every task in `refactor-battlefield-route-to-tile-grid` is complete, run strict OpenSpec validation for that change, and retain its passing Unity smoke result before editing P1 application code
- [x] 1.2 Record the finalized P0 cell semantics, ordered-route, topology-validation, projection, and tile-render API surface that this change will consume, and verify no legacy polyline or stretched-strip path is needed by P1

## 2. Catalog and bundled definitions

- [x] 2.1 Add immutable `LevelDefinition`, composite identity, structured resolution result, and `ResolvedLevelDefinition` contracts for `levelId`, `mapId`, `waveSetId`, `ruleSetId`, and `themeId`
- [x] 2.2 Add addressable wave-set, rule-set, and presentation-theme definitions and compilation indexes while keeping shared fruit, enemy, equipment, and skill definitions unchanged
- [x] 2.3 Implement catalog validation for stable and duplicate IDs, dangling references, P0 topology failures, wave ordering and enemy references, wave/rule count and milestone compatibility, and incomplete themes
- [x] 2.4 Author `orchard-01` as the P0-grid U-shaped teaching map with its explicit teaching wave set, baseline rules, and day-orchard theme
- [x] 2.5 Author `orchard-02` as the distinct P0-grid S-shaped coverage map with alternating turns, fast-and-armored wave emphasis, coverage rules, and a distinct theme
- [x] 2.6 Author `orchard-03` as the shorter P0-grid core-corridor map with boss-final pressure waves, pressure rules, and a distinct theme
- [x] 2.7 Add deterministic catalog tests proving ordered three-level exposure, complete resolution, distinct route signatures, required U/S/corridor topology, pressure composition, and rejection of every invalid-reference class

## 3. Battle construction and simulation composition

- [x] 3.1 Resolve `BattleLaunchRequest.LevelId` once in the flow coordinator, return a structured error for unknown levels, and inject the same resolved bundle into the battle host, simulation, and presentation before their first use
- [x] 3.2 Migrate simulation wave lookup and battle-rule access from global `wave.NN` and catalog-wide rules to the active resolved wave set and rule set
- [x] 3.3 Use the resolved P0 map for topology, movement, targeting, planting, and projection, and use `themeId` only for presentation so theme-only changes cannot affect gameplay checksums
- [x] 3.4 Add deterministic multi-level simulation tests covering distinct maps/waves/rules, fixed launch-time identity, same-seed replay equality, and no default fallback for an invalid `LevelId`

## 4. Versioned snapshot identity

- [x] 4.1 Add a new snapshot schema version that exports `levelId`, `mapId`, `waveSetId`, `ruleSetId`, and `themeId` with the existing catalog/content version and gameplay state
- [x] 4.2 Resolve and compare all snapshot identity fields before restore mutation, return field-specific mismatch errors, and keep live state and presentation-event delivery atomic on every failure
- [x] 4.3 Add the narrowly gated legacy default-snapshot compatibility path for the supported bundled version and map to `orchard-01`, rejecting every ambiguous legacy payload without consulting current Lobby selection
- [x] 4.4 Add round-trip, legacy migration, missing definition, stale version, per-component mismatch, and restore-atomicity smoke fixtures for all three levels

## 5. Lobby, profile, settlement, and retry flow

- [x] 5.1 Extend the shared portrait shell layout with safe-area-aware draw and hit-test rectangles for three level cards, visible selection, and Start at 360×800, 375×812, 402×874, and 430×932
- [x] 5.2 Replace the Lobby's unavailable placeholder with selectable `orchard-01`, `orchard-02`, and `orchard-03` cards, concise route/focus copy, and Start behavior that submits only the visibly selected `LevelId`
- [x] 5.3 Persist the last valid selected level in the local profile and implement explicit unavailable-profile recovery to the catalog-declared UI default without allowing launch fallback
- [x] 5.4 Display the completed level identity in settlement, restore it as the Lobby selection on return, and retry it through catalog resolution with a fresh session ID and nonzero seed
- [x] 5.5 Extend shell flow and layout validations for card selection, duplicate-transition guards, profile recovery, result mismatch rejection, return selection, retry identity, and draw/hit geometry equality

## 6. Integrated acceptance and handoff

- [x] 6.1 Run `FruitDefense.Editor.ProjectSetup.SmokeValidate` with catalog, topology, simulation, snapshot, shell, and supported-viewport assertions enabled and retain the passing log
- [x] 6.2 Run `FruitDefense.Editor.WebBuild.Build`, verify the ordinary WebGL artifact, and do not infer Douyin or WeChat support from that success
- [x] 6.3 Exercise all three Lobby selections and battle maps on a real portrait WebGL canvas at the supported viewports, capturing readable selection, correct map launch, unclipped safe areas, aligned input, and per-cell route rendering
- [x] 6.4 Because the user reported the embedded browser may crash, avoid that path and complete the same real-canvas checks through external Chrome/CDP before marking visual acceptance complete
- [x] 6.5 Run `openspec validate introduce-level-map-catalog --strict`, confirm every acceptance artifact is current, and report any design-overview synchronization separately from transient gate evidence
