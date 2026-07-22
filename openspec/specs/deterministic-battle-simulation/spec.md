# deterministic-battle-simulation Specification

## Purpose
TBD - created by archiving change make-battle-simulation-deterministic. Update Purpose after archive.
## Requirements
### Requirement: Fixed-step frame advancement
The battle simulation SHALL advance gameplay only in 0.05 second logical steps and SHALL expose frame advancement, single-step advancement, and frame-accumulator reset operations.

#### Scenario: Equivalent render-frame partitions
- **WHEN** two simulations with the same seed and command sequence receive 100 frames of 0.01 seconds and 20 frames of 0.05 seconds respectively
- **THEN** both simulations consume 20 logical steps and produce the same deterministic gameplay-state checksum

#### Scenario: Compatibility tick entry point
- **WHEN** the existing runtime host calls `Tick` with an unscaled frame delta
- **THEN** the call delegates to fixed-step frame advancement without requiring a host API change

### Requirement: Bounded catch-up
The simulation SHALL clamp a frame delta to at most 0.25 seconds, SHALL retain no more than five pending logical steps, and SHALL execute at most five logical steps in one frame advancement.

#### Scenario: Long frame stall
- **WHEN** frame advancement receives a delta greater than 0.25 seconds
- **THEN** no more than five logical steps execute and excess time does not become catch-up work in later frames

#### Scenario: Pause does not accumulate time
- **WHEN** frame callbacks occur while the simulation is paused or terminal and the simulation later resumes or is inspected
- **THEN** no stale frame time is consumed

### Requirement: Fixed-step speed control
The 2x speed mode SHALL accelerate play by consuming twice as many 0.05 second logical steps for accepted real time and MUST NOT enlarge an individual logical-step duration.

#### Scenario: Speed equivalence
- **WHEN** equivalent simulations advance for two seconds at 1x and one second at 2x using render deltas that do not hit the catch-up cap
- **THEN** they consume the same logical-step count and produce the same deterministic gameplay-state checksum

### Requirement: Serializable deterministic randomness
All battle-simulation randomness SHALL use a serializable xorshift32 source whose current non-zero 32-bit state can be captured and restored.

#### Scenario: Same seed and commands
- **WHEN** simulations begin from the same seed and receive the same commands and frame inputs
- **THEN** random decisions, random state, and deterministic gameplay-state checksums remain identical

#### Scenario: Zero seed mapping
- **WHEN** a simulation or random source is reset with seed zero
- **THEN** it uses the documented fixed non-zero xorshift32 state instead of time-based entropy or the absorbing zero state

### Requirement: Complete deterministic reset
Resetting a simulation SHALL reconstruct model state, clear pending frame time, and reseed the random source before any randomized initial state is generated.

#### Scenario: Seeded reset replay
- **WHEN** a simulation consumes random values and accumulated frame time and is then reset with its original seed
- **THEN** its initial flowerpots, subsequent nursery results, frame accumulator, and random sequence match a newly constructed simulation with that seed

