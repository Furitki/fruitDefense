## Context

The shell already transports a stable `LevelId` in `BattleLaunchRequest` and `BattleResult`, stores `lastSelectedLevelId`, and retries the completed level. The battle host nevertheless constructs one bundled content catalog and one default `BattlefieldMapDefinition`; the snapshot identifies only the content catalog and map. As a result, `orchard-01` is currently a flow label rather than a complete, resolvable gameplay definition.

This is a cross-cutting P1 change across content definitions, map construction, simulation initialization, snapshot compatibility, Lobby presentation, settlement, and validation. It MUST follow `refactor-battlefield-route-to-tile-grid`: that P0 change owns per-cell semantics, ordered cardinal route cells, route topology validation, tile projection, and route-tile rendering. This change consumes those contracts and must not create another coordinate, route, or rendering model.

The runtime remains deterministic and scene-independent. All three levels use stable semantic IDs and bundled local data; ordinary WebGL remains the shared acceptance baseline. A successful WebGL build does not imply Douyin or WeChat readiness.

## Goals / Non-Goals

**Goals:**

- Resolve every supported `LevelId` through one validated catalog into `mapId`, `waveSetId`, `ruleSetId`, and `themeId`.
- Author three meaningfully distinct playable levels on the P0 tile grid: U-shaped teaching, S-shaped coverage, and core-corridor boss pressure.
- Inject the resolved level bundle into battle construction so simulation and presentation consume the same identity and definitions.
- Preserve the selected level across Lobby selection, launch, snapshot export/restore, settlement, return, and retry.
- Keep catalog and gameplay validation deterministic and suitable for editor smoke tests.
- Provide portrait-safe Lobby selection whose draw and hit-test rectangles come from the same layout helper.

**Non-Goals:**

- Long-term progression, unlock trees, currencies, rewards, economy, monetization, daily activities, or chapter-map structure.
- A general-purpose map editor, remote content download, AssetBundle delivery, or player-authored maps.
- New enemy, fruit, equipment, or skill systems beyond composing existing definitions into level-specific wave and rule sets.
- Douyin or WeChat adapter authorization.
- Reimplementing P0 tile semantics, path topology, route sampling, route-tile selection, or battlefield projection.

## Decisions

### 1. Treat P0 completion as a hard implementation gate

No application-code task in this change starts until `refactor-battlefield-route-to-tile-grid` is apply-complete and passes strict OpenSpec validation plus its smoke checks. Its canonical cell types, ordered path-cell contract, topology validator, and projection API are compile-time dependencies of the P1 catalog.

Depending on the P0 contract avoids two temporary map representations and prevents parallel agents from editing the same battlefield files with incompatible assumptions. Implementing P1 directly on the legacy route-segment API was rejected because it would recreate the adaptation defect and cause avoidable merge conflicts.

### 2. Use stable definitions and a resolved level bundle

Add an immutable `LevelDefinition` with exactly one stable `levelId`, `mapId`, `waveSetId`, `ruleSetId`, and `themeId`. A compiled catalog indexes levels and each referenced definition by ordinal string identity. Lookup either returns a `ResolvedLevelDefinition` containing all five identities and concrete definitions or returns a structured failure; it never substitutes the default map for an unknown or incomplete level.

`GameSimulation` and battlefield presentation receive the resolved map, waves, rules, and theme from the battle host. They do not query a global selected-level singleton. This keeps deterministic tests independent of loaded scenes and ensures one session cannot silently switch definitions.

Using scene names, enum ordinals, display labels, or Unity GUIDs as level identity was rejected because those values are presentation or authoring details and do not satisfy the project's stable-ID contract. Using separate ad-hoc switches in Lobby, battle, and settlement was rejected because they can drift.

### 3. Extend bundled content with addressable wave and rule sets

The bundled content layer exposes wave sets and rule sets by ID. A wave set owns an explicit ordered list of existing wave-definition IDs; a rule set owns the battle-rule values used by a session, including wave count and milestone configuration. Compilation validates references, ordering, count compatibility, enemy references, and duplicate identities before a level is selectable.

The simulation uses the active wave set rather than constructing `wave.NN` keys and uses the active rule set rather than the single catalog-wide rule object. Existing fruit, enemy, equipment, and skill definitions remain shared. This is narrower than cloning a complete content catalog per level and makes the composition boundary visible.

### 4. Author three bundled levels as data, not branches

The first release catalog contains:

| Level | Purpose | Required composition |
|---|---|---|
| `orchard-01` | Teaching | A forgiving U-shaped per-cell route, teaching wave set, baseline rule set, and day-orchard theme |
| `orchard-02` | Coverage | An S-shaped route with alternating turns, a set that emphasizes fast and armored enemies, coverage-oriented rules, and a visually distinct theme |
| `orchard-03` | Core pressure | A shorter core-corridor route whose terminal `Exit` cell is cardinally adjacent to the core, a set whose final pressure includes a boss, pressure rules, and a visually distinct theme |

Maps are authored through the P0 `BattlefieldMapDefinition`; route tiles are derived by P0 topology and never stored as stretched strips or separate normalized polylines. The three level definitions have distinct IDs and map IDs. Wave, rule, and theme IDs are explicit even when values happen to match, so later tuning does not change identity accidentally.

Hard-coding level-specific branches inside `GameSimulation` was rejected because new levels would require code changes and snapshot identity would no longer describe behavior completely.

### 5. Carry one composite identity through the session and snapshot

`BattleLaunchRequest` continues to originate with `LevelId`; the flow coordinator resolves it once before simulation construction. The current session retains the resulting composite identity. `BattleResult` keeps `LevelId` as its public flow identity and is validated against the originating request.

Snapshot export includes `levelId`, `mapId`, `waveSetId`, `ruleSetId`, and `themeId` in addition to catalog/content version. Restore resolves the recorded level and requires every recorded component ID to equal the current catalog resolution before mutating simulation state. Missing definitions, stale content, or any mismatch fails atomically with a structured error.

Because this extends the serialized contract, export uses a new snapshot schema version. A legacy single-map snapshot may migrate only through an explicit compatibility function that confirms its bundled catalog/content version and legacy map ID and maps it to `orchard-01`; all other legacy payloads fail rather than default silently. Keeping the old schema number after adding identity fields was rejected because it would make incompatible payloads indistinguishable.

### 6. Make Lobby selection authoritative and portrait-safe

The Lobby shows three available level cards with a visible selected state and concise route/focus copy. Selecting a card updates `lastSelectedLevelId`; Start launches only that ID. On entry, a valid stored selection is restored. If stored profile data references an unavailable level, profile recovery records the invalid identity and selects the catalog's declared safe default for the UI; a launch request with an unknown ID still fails and never falls through to that default.

`PortraitShellLayout` derives level-card, selection, and Start hit rectangles for each supported portrait/safe-area size, and `LobbyPresenter` draws and hit-tests from those same rectangles. The list remains usable at 360×800, 375×812, 402×874, and 430×932 logical viewports with inset safe areas.

Settlement displays the completed level identity. Return restores that level as the Lobby selection; retry creates a fresh session ID and seed while reusing the completed `LevelId`, which is then resolved again against the current bundled catalog.

### 7. Validate by layers and keep implementation ownership serialized

Implementation proceeds in non-overlapping lanes after P0:

1. **Catalog/core lane:** owns new level, wave-set, rule-set, theme, and snapshot contracts plus deterministic validation. It publishes the resolved-level API before shell work begins.
2. **Integration/shell lane:** owns flow-coordinator injection, Lobby/settlement presentation, profile selection, and layout hit testing while consuming the published API; it does not edit P0 topology internals.
3. **Acceptance lane:** updates editor smoke, WebGL build checks, and real canvas capture only after both prior lanes integrate.

The required editor surface is `FruitDefense.Editor.ProjectSetup.SmokeValidate`. Visible acceptance uses a real WebGL canvas rather than only unit geometry. Browser-driven capture is optional: if the embedded browser is unstable, acceptance uses the built player plus external browser or deterministic screenshots, and the crash is reported separately rather than weakening the assertions.

## Risks / Trade-offs

- [P0 contracts change while P1 is implemented] → Finish and validate P0 first; record the exact consumed APIs before assigning P1 code work.
- [Catalog composition permits dangling or incompatible references] → Compile all definitions into indexed dictionaries and reject missing IDs, duplicates, invalid route topology, wave/rule count mismatch, and unsupported theme references before Lobby exposure.
- [Snapshot schema change breaks existing fixtures] → Add explicit V2 round-trip, mismatch, atomicity, and narrowly gated V1-to-`orchard-01` compatibility fixtures.
- [Three portrait cards crowd the Lobby] → Use shared adaptive layout helpers, safe-area assertions across four target sizes, and short copy; do not shrink primary touch targets below the accepted minimum.
- [Balance tuning expands into an economy redesign] → Limit tuning to per-level wave/rule composition using existing entities and rewards; leave progression and economy decisions out of this change.
- [Embedded browser crashes during WebGL acceptance] → Keep editor smoke/build gates independent, use an external browser or stable capture path for real-canvas evidence, and report browser instability without claiming acceptance from editor-only output.

## Migration Plan

1. Complete and strictly validate `refactor-battlefield-route-to-tile-grid`; freeze the consumed P0 API surface for this change.
2. Add catalog contracts, compilation, and the three bundled definitions behind deterministic validation while `orchard-01` remains the only Lobby launch option.
3. Migrate simulation construction, waves, rules, themes, and snapshot export/restore to the resolved bundle; keep compatibility tests for the legacy default snapshot path.
4. Enable the three-card Lobby selection and settlement/retry identity presentation after session integration is green.
5. Run editor smoke, WebGL build, and real portrait canvas acceptance at the required viewports.

Rollback disables multi-level Lobby exposure and restores the single bundled `orchard-01` composition together with the previous snapshot reader. Catalog and snapshot changes must roll back as one unit; no player economy migration is involved.

## Open Questions

None for implementation. The three-level catalog is an initial bundled content slice and does not decide whether the eventual long-term structure is linear, chapter-based, or endless.
