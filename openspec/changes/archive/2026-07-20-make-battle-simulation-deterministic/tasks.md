## 1. Deterministic Random Source

- [x] 1.1 Add a serializable xorshift32 source with stable seed-zero mapping, bounded integer generation, unit-interval generation, and restorable non-zero state
- [x] 1.2 Replace every `System.Random` use in `GameSimulation` and make constructor/reset seed the source before randomized initial-state generation

## 2. Fixed-Step Simulation

- [x] 2.1 Extract one 0.05 second gameplay step and expose `Step`, preserving existing gameplay rule ordering
- [x] 2.2 Add `AdvanceFrame` and `ResetFrameAccumulator` with finite non-negative input handling, 0.25 second caps, five-step limit, fixed-step 2x speed, and pause/terminal clearing
- [x] 2.3 Keep `Tick(float)` as a compatibility wrapper and expose accumulator/random state needed by later snapshot work

## 3. Deterministic Validation

- [x] 3.1 Add scene-independent editor smoke coverage for same-seed command checksums and xorshift state restoration
- [x] 3.2 Add frame partition, speed equivalence, bounded catch-up, and seeded reset/replay validation
- [x] 3.3 Run OpenSpec validation, Unity 6000.3.19f1 compilation, the deterministic smoke, and the existing project smoke; document any integration compatibility work outside this change's edit scope
