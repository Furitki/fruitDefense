## Why

The project needs a repeatable GM stress surface that can create mowing-style enemy density and exercise all bundled plants without economy friction. The existing canonical map and simulation execution path resolve one primary route and one spawn, so faking eight lanes through presentation offsets would make targeting, projectiles, effects, floating text, and performance evidence untrustworthy.

## What Changes

- Extend the canonical battlefield/runtime position model so an enemy owns a stable route identity and multiple named cardinal routes can execute simultaneously while all positions still resolve through the shared map projection.
- Add a development-only eight-lane GM stress battle: eight vertical routes and top spawn pads, two complete bottom plant rows, no automatic waves, no victory/defeat, and no result or snapshot submission.
- Add deterministic GM commands for per-lane enemy queues, batch sizes `1/10/50`, four bundled enemy types, a total active-enemy cap of 500, and an all-lanes stress action.
- Add five unlimited plant selectors that place or replace a one-star plant in any of the sixteen bottom-row pots without sunlight, inventory, refresh, expansion, merge, or equipment cost.
- Render the GM battlefield through the registered production `terrain-brush.grass-on-soil` composition: soil remains the cell-aligned base and the bottom two plant rows use the square grass landform with its refined sixteen-mask edge.
- Preserve the approved runtime UI system by reusing Secondary/Quiet actions, Selectable Card state, non-color selection cues, the packaged Chinese font, shared safe-area geometry, and existing gameplay art; no GM-local skin or new production ArtSet slot is introduced.
- Keep bundled and published release levels on the standard single-route execution contract. The GM level is not added to `PlayableLevels`, the publication manifest, production `Resources`, profile selection, settlement, or the normal WebGL build.
- Provide a stable `Fruit Defense/Playtest/GM 压力测试关` editor entry and a separate Development WebGL build/acceptance surface.

## Capabilities

### New Capabilities

- `gm-multi-lane-stress-battle`: Defines the development-only GM session, eight-lane map, manual spawn/plant controls, no-failure lifecycle, load caps, and WebGL stress evidence.

### Modified Capabilities

- `battlefield-tile-route`: Generalizes compiled route topology and entity projection from one active route to stable per-entity named routes while retaining the standard single-route release profile.
- `battlefield-map-layout`: Allows the canonical aggregate and shared projection to expose several validated named routes and their typed spawn/goal markers without introducing a second position source.
- `deterministic-battle-simulation`: Requires route identity to participate in deterministic enemy movement, targeting positions, presentation events, and state checksums.

## Impact

- Core route/map/state/simulation code under `Assets/Scripts/Core/` and its deterministic smoke fixtures.
- Battle session contracts and composition under `Assets/Scripts/BattleSessionContracts.cs`, `Assets/Scripts/App/AppFlowCoordinator.cs`, and `Assets/Scripts/FruitDefenseGame.cs`.
- A modular development-only GM controller/presenter plus stable editor build/playtest tooling under `Assets/Scripts/Development/` and `Assets/Editor/Tools/`.
- The shared layered-terrain GUI renderer and the existing release orchard terrain palette; no GM-local texture, brush copy, or missing-terrain fallback is introduced.
- Automated validation under `Assets/Editor/Tests/` and a separate Development WebGL artifact/evidence directory.
- `docs/design/game-design-overview.md` section 4.3 will record that the runtime now supports named multi-route execution while the three released levels remain single-route and the first consumer is a non-release GM surface.
- No economy, reward, production level order, platform authorization, released UI theme, or mini-game adapter behavior changes.
