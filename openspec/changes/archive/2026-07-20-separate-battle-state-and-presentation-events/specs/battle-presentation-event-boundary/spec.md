## ADDED Requirements

### Requirement: Simulation emits presentation events through a one-way boundary
The battle simulation SHALL emit transient presentation information into a state-external event stream, and presentation consumption SHALL NOT invoke or mutate simulation rules as a side effect.

#### Scenario: Cue emitted by a battle effect
- **WHEN** a fixed simulation step executes a skill effect with a cue
- **THEN** the simulation appends a presentation event after earlier events from that simulation instance
- **AND** the event contains its logic tick, source and target entity IDs, position, stable cue ID, and stable visual ID

#### Scenario: No presentation consumer exists
- **WHEN** the same seed and command sequence run once with events drained and once without any presentation consumer
- **THEN** both simulations produce the same outcome checksum and random state

### Requirement: Presentation event delivery is ordered, single-consumption, and bounded
The presentation event stream SHALL preserve emission order for retained events, SHALL remove events through destructive consumption, SHALL support explicit discard, and SHALL bound pending transient data without blocking simulation.

#### Scenario: Consumer drains pending events
- **WHEN** a consumer drains multiple pending presentation events
- **THEN** it receives them in increasing sequence order exactly once
- **AND** an immediate second drain returns no events

#### Scenario: Consumer discards pending events
- **WHEN** a consumer explicitly discards pending transient events during a route or view reset
- **THEN** no discarded event is delivered later
- **AND** subsequent simulation events remain consumable in their emission order

#### Scenario: Pending event capacity is exceeded
- **WHEN** event production exceeds the stream capacity without a consumer
- **THEN** the oldest transient events are dropped
- **AND** simulation stepping and outcome state continue unchanged

### Requirement: Authoritative state and persistence exclude presentation delivery state
`GameState`, `BattleSnapshotV1`, and the outcome checksum SHALL exclude pending presentation events, local delivery sequence, combat-effect lifetime, and floating feedback lifetime.

#### Scenario: Snapshot is exported with pending presentation events
- **WHEN** a battle exports a snapshot while cue and feedback events are pending
- **THEN** the serialized snapshot contains no presentation event payload or delivery cursor
- **AND** restoring the snapshot starts with no pending transient event

#### Scenario: Presentation queue changes without battle state changes
- **WHEN** pending events are drained or discarded between two checksum calculations
- **THEN** the outcome checksum remains unchanged

### Requirement: Battle view owns disposable transient presentation
`FruitDefenseGame` SHALL consume presentation events into a local presentation buffer and SHALL render and expire combat effects and feedback from that buffer without changing battle state.

#### Scenario: View consumes a combat cue
- **WHEN** the battle view consumes a cue event with stable cue and visual IDs
- **THEN** it creates the same player-visible effect kind and duration used before this change
- **AND** drawing or expiring the effect sends no command to the simulation

#### Scenario: View is rebuilt or acceptance state is replaced
- **WHEN** the battle view clears or recreates its local presentation buffer
- **THEN** transient combat effects and feedback may disappear
- **AND** plants, enemies, projectiles, wave state, and all other authoritative visuals remain rebuildable from `GameState`

### Requirement: Existing player-visible battle flow remains compatible
The separated event boundary SHALL preserve current battle input, layout, visuals, and acceptance routing across Editor and WebGL.

#### Scenario: Existing validation suite runs
- **WHEN** the project smoke, deterministic smoke, composable-skill smoke, and snapshot smoke execute
- **THEN** gameplay parity checks pass with presentation assertions performed through the event boundary

#### Scenario: WebGL acceptance states render
- **WHEN** the WebGL build is captured through the existing 13-state acceptance runner
- **THEN** all states complete without changing the acceptance manifest contract, safe-area layout, or control geometry
