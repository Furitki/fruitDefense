# battle-presentation-event-boundary Specification

## Purpose
TBD - created by archiving change separate-battle-state-and-presentation-events. Update Purpose after archive.
## Requirements
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
`GameState`, `BattleSnapshot`, and the catalog-independent outcome checksum SHALL exclude pending presentation events, local delivery sequence, dropped-event count, combat-effect lifetime, and floating feedback lifetime. Successful current snapshot restore SHALL reset the presentation event stream to empty without emitting a restore-success event; failed restore SHALL preserve pending content and order, next sequence, and dropped count exactly.

#### Scenario: Snapshot is exported with pending presentation events
- **WHEN** a supported Standard battle exports a snapshot while cue and feedback events are pending
- **THEN** the serialized snapshot contains no presentation event payload, delivery cursor, next sequence, or dropped count

#### Scenario: Snapshot restore succeeds
- **WHEN** a current snapshot passes validation and commits to a same-source target with pending events
- **THEN** the stream becomes empty with its initial sequence/drop state and no restore-success event is appended

#### Scenario: Snapshot restore fails
- **WHEN** restore fails schema, presence, source, value, identity, or reference validation while events are pending
- **THEN** pending event contents/order, next sequence, and dropped count remain exactly as they were before the attempt

#### Scenario: Presentation queue changes without battle state changes
- **WHEN** pending events are drained, discarded, or dropped between two checksum calculations
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
The separated event and host boundaries SHALL preserve current battle input, layout, visuals, gameplay outcomes, snapshot behavior, and production routing across Editor and ordinary WebGL, and SHALL preserve the existing injected state matrix in the dedicated acceptance WebGL profile.

#### Scenario: Existing validation suite runs
- **WHEN** the project smoke, deterministic smoke, composable-skill smoke, and snapshot smoke execute
- **THEN** gameplay parity checks pass with presentation assertions performed through the event boundary
- **AND** host assertions use immutable observations and bounded commands rather than a mutable simulation reference

#### Scenario: WebGL acceptance states render
- **WHEN** the dedicated acceptance WebGL build is captured through the existing 13-state acceptance runner
- **THEN** all states complete without changing the acceptance manifest contract, safe-area layout, or control geometry

#### Scenario: Ordinary release battle runs
- **WHEN** a player enters Battle through the release `Bootstrap → Lobby → Battle` flow
- **THEN** input, simulation, presentation-event consumption, result submission, and routing behave as before
- **AND** no acceptance port or mutable host-state access is exposed

### Requirement: Production battle host exposes bounded session control
`IBattleSessionHost` MUST NOT expose `GameSimulation`, `GameState`, mutable gameplay collections, or another mutable authoritative aggregate. It SHALL expose only immutable session observations and bounded lifecycle, visibility, restart, snapshot, result-submission, and disposal commands required by production orchestration.

#### Scenario: App orchestration observes an active battle
- **WHEN** the production coordinator needs session phase, wave, lives, or completion information
- **THEN** it reads an immutable battle-session status value containing only the required finite facts
- **AND** it cannot obtain a reference that permits authoritative state mutation

#### Scenario: Production coordinator changes session lifecycle
- **WHEN** the app pauses for platform backgrounding, restarts, restores, submits a terminal result, or disposes a session
- **THEN** it invokes the corresponding bounded host command
- **AND** it does not write simulation fields or collections directly

### Requirement: Acceptance fixture mutation uses an acceptance-only port
Named-state replacement and terminal-outcome fixtures SHALL be exposed through a finite acceptance port compiled only with `FRUIT_DEFENSE_ACCEPTANCE`, and that port MUST NOT extend or be reachable through the production battle-host contract.

#### Scenario: Dedicated acceptance build requests a terminal fixture
- **WHEN** `ConfigureAcceptanceFlow` requests victory or defeat in the dedicated acceptance profile
- **THEN** the coordinator delegates the known command to the acceptance port
- **AND** the port performs the fixture transition and result submission without exposing the simulation aggregate

#### Scenario: Ordinary release is compiled
- **WHEN** the release profile is built without `FRUIT_DEFENSE_ACCEPTANCE`
- **THEN** acceptance port types, state replacement entry points, and terminal fixture commands are absent from the player

#### Scenario: Unknown acceptance command is received
- **WHEN** the dedicated acceptance port receives an unknown named state or terminal command
- **THEN** it rejects the request without changing authoritative battle state
- **AND** acceptance automation reports the failed fixture transition

