## Why

The battle simulation currently advances once per rendered frame, scales a variable delta directly for 2x speed, and keeps a `System.Random` instance that is not reseeded by `Reset(seed)`. That makes frame-rate-independent replay, reliable save snapshots, and later content-version validation impossible to guarantee.

## What Changes

- Add a 20 Hz fixed-step frame accumulator with a 0.25 second frame-delta cap and at most five simulation steps consumed per frame.
- Make 2x speed consume twice as many fixed steps instead of enlarging the duration of an individual step.
- Add an explicitly serializable xorshift32 random source and route all simulation randomness through it.
- Make reset restore simulation state, frame accumulator, and random state from the requested seed, with a stable non-zero mapping for seed zero.
- Keep `Tick(float)` as a compatibility wrapper while exposing explicit frame and step operations for hosts, tests, and future snapshots.
- Add deterministic editor validation for replay, frame partitioning, speed equivalence, and seeded reset behavior.

## Capabilities

### New Capabilities

- `deterministic-battle-simulation`: Defines fixed-step advancement, speed semantics, reset behavior, serializable random state, and deterministic validation.

### Modified Capabilities

None.

## Impact

- Affects `Assets/Scripts/Core/GameSimulation.cs` and adds a simulation-owned deterministic random source.
- Retains the existing `FruitDefenseGame` call to `Tick(Time.unscaledDeltaTime)` and does not change presentation, scene, project, map, or content APIs.
- Adds editor-only smoke coverage that can run under Unity 6000.3.19f1 without a loaded scene.
