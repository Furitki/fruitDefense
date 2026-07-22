## Why

Transient combat visuals and floating feedback currently live inside `GameState` and advance on the deterministic simulation clock. That couples rendering lifetime to authoritative battle state, makes presentation data reachable from snapshots and tests, and prevents future platform clients from dropping or rebuilding visuals without risking simulation divergence.

## What Changes

- Introduce a simulation-owned presentation event stream with deterministic sequence numbers, logic ticks, stable cue IDs, and stable visual IDs.
- Provide ordered, destructive drain and explicit discard APIs so a presentation consumer can consume each event once, while an absent or restarted consumer does not affect battle results.
- Move combat-effect and feedback lifetimes into a presentation-only buffer owned by `FruitDefenseGame`; the game view consumes events but never writes commands or state back into the simulation as a consequence of rendering them.
- Remove transient cue, combat-effect, and floating-text collections from deterministic `GameState`; snapshots and outcome checksums remain presentation-free.
- Preserve the current combat UI and 13 WebGL acceptance states while adding smoke coverage for ordering, single consumption, snapshot exclusion, consumer independence, and stable identifiers.
- Gameplay rules, rewards, persistence schema, content definitions, and battle UI layout are non-goals for this change.

## Capabilities

### New Capabilities

- `battle-presentation-event-boundary`: Defines the ordered one-way contract from deterministic battle simulation to disposable presentation consumers.

### Modified Capabilities

None.

## Impact

- Affected runtime code: `Assets/Scripts/Core/GameModel.cs`, `Assets/Scripts/Core/GameSimulation.cs`, and `Assets/Scripts/FruitDefenseGame.cs`.
- Existing battle, skill, snapshot, deterministic, and project smoke fixtures will consume the new event API instead of reading transient lists from `GameState`.
- No new package or platform dependency is introduced, and no ProjectSettings, Build Settings, WebGL build entry, or acceptance script changes are required.
