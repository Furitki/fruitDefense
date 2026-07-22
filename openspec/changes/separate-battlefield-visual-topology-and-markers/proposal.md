## Why

The battlefield map currently overloads one exclusive cell role with terrain appearance, placement rules, route membership, entry/exit identity, and the core, while initial-pot points live in a separate special-case structure and enemy spawning is implicit at route progress zero. This blocks intuitive authoring of visual-only terrain, multi-capability cells, multiple routes or spawn points, item points, and channel-specific collision without duplicating or weakening the canonical map contract.

## What Changes

- **BREAKING (content schema):** Replace the exactly-one-`BattlefieldCellRole` authoring contract with a layered battlefield definition containing an explicit visual surface layout, composable gameplay cell capabilities/collision channels, ordered named routes, and typed semantic markers.
- Keep route order as canonical topology rather than reducing a route to a Boolean cell flag; derive entry/exit convenience views from route endpoints.
- Move the core and initial-flowerpot candidates into the typed marker layer, and introduce validated marker identities and references suitable for enemy spawn, route goal, player spawn, item spawn, and future trigger points.
- Resolve Dual-Grid masks from semantic visual surfaces and terrain palettes instead of treating gameplay plantability and route membership as permanent art choices; preserve the current grass/stone/soil result through deterministic migration defaults.
- Compile compatibility queries such as `IsPlantable`, `IsRoute`, `EntryCell`, `ExitCell`, `CoreCell`, and initial-pot groups so current simulation, projection, interaction, and rendering callers can migrate incrementally without player-visible behavior changes.
- Version and validate the layered content, include gameplay-affecting topology and markers in deterministic map identity, and exclude presentation-only surface/palette changes from gameplay state identity.
- Migrate all three bundled maps with identical routes, initial pots, battle outcomes, portrait geometry, and `Bootstrap → Lobby → Battle → Settlement` flow.

## Capabilities

### New Capabilities

- `battlefield-layered-map-model`: Defines the visual-layout, gameplay-topology, named-route, collision-capability, and typed-marker boundaries of a canonical battlefield map.

### Modified Capabilities

- `battlefield-tile-route`: Replaces the exclusive cell-role contract with composable cell capabilities, named ordered routes, and route-endpoint derivation while preserving square-grid traversal.
- `battlefield-map-layout`: Expands the canonical battlefield definition and validation contract to consume compiled topology and marker views without changing projection or interaction geometry.
- `level-map-catalog`: Requires layered map compilation, stable route and marker identities, reference validation, and migration of all bundled levels.
- `battle-snapshot-v1`: Separates gameplay map identity from presentation-only layout/palette data while pinning all gameplay-affecting routes, capabilities, and markers.
- `battlefield-dual-grid-terrain-presentation`: Changes grass/road occupancy from a permanent gameplay-role mapping to semantic visual surfaces with deterministic current-map defaults.

## Impact

- Core map data, topology validation, route sampling, initial setup, map identity, and snapshot compatibility under `Assets/Scripts/Core`.
- Level-map construction, compilation, stable content references, and bundled-map migration under `Assets/Scripts/Content`.
- Terrain surface resolution and level-theme/palette binding in `Assets/Scripts/Tilemaps`, `FruitDefenseGame`, project setup, and the release Battle scene.
- Editor smoke validation for layer independence, invalid marker combinations, route/marker references, deterministic identity, migration parity, all bundled maps, and the real WebGL portrait battle flow.
- Existing generated Dual-Grid assets remain reusable; no new Unity physics dependency, runtime Tilemap representation, platform adapter, backend, economy, reward, or scene-flow capability is introduced.
- The completed but unarchived `integrate-dual-grid-terrain-into-battlefield` change must be archived before this change is applied so its presentation capability can receive this change's delta cleanly.
