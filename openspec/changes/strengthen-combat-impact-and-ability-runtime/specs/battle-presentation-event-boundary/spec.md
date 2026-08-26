## MODIFIED Requirements

### Requirement: Simulation emits presentation events through a one-way boundary
The battle simulation SHALL emit transient semantic presentation information into a state-external event stream, and presentation consumption SHALL NOT invoke or mutate simulation rules as a side effect. Events SHALL identify gameplay facts and SHALL NOT contain rendering kinds, asset IDs, colors, durations, audio clips, or shake policy.

#### Scenario: Semantic impact emitted by battle resolution
- **WHEN** a fixed simulation step resolves an ability delivery or projectile impact
- **THEN** the simulation appends semantic phase and result events after earlier events from that simulation instance
- **AND** each event contains its logic tick, sequence, relevant stable gameplay identities, source and target entity IDs, position, direction, and resolved result values

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
`GameState`, the current Battle snapshot, and the outcome checksum SHALL exclude pending presentation events, local delivery sequence, feedback-profile identity, render interpolation samples, attacker/target reaction state, combat-effect lifetime, and floating-feedback lifetime.

#### Scenario: Snapshot is exported with pending presentation events
- **WHEN** a battle exports a snapshot while semantic events are pending
- **THEN** the serialized snapshot contains no event payload, delivery cursor, feedback profile, or reaction state
- **AND** restoring the snapshot starts with no pending transient event

#### Scenario: Presentation queue changes without battle state changes
- **WHEN** pending events are drained or discarded between two checksum calculations
- **THEN** the outcome checksum remains unchanged

### Requirement: Battle view owns disposable transient presentation
`FruitDefenseGame` SHALL consume semantic presentation events into a local feedback director, resolve profiles from presentation-owned gameplay keys, interpolate render samples, and render or expire combat feedback without changing battle state.

#### Scenario: View consumes a damage event
- **WHEN** the battle view consumes a damage-resolved event
- **THEN** it resolves the active profile, creates bounded local reaction/VFX/text/audio/surface-motion records, and sends no command to simulation

#### Scenario: View is rebuilt or acceptance state is replaced
- **WHEN** the battle view clears or recreates its local presentation state
- **THEN** transient feedback and interpolation history may disappear
- **AND** plants, enemies, projectiles, wave state, and all authoritative visuals remain rebuildable from `GameState`

### Requirement: Existing player-visible battle flow remains compatible
The semantic event boundary SHALL preserve current battle input, safe-area layout, control geometry, navigation, and acceptance routing while intentionally updating combat feedback and approved combat timing rules.

#### Scenario: Existing validation suite runs
- **WHEN** the project smoke, deterministic combat, Ability, snapshot, and presentation smoke execute
- **THEN** gameplay and boundary assertions pass through the semantic event path

#### Scenario: WebGL acceptance states render
- **WHEN** the WebGL build is captured through the battle acceptance runner
- **THEN** existing routes and controls remain usable and the feedback layer does not change hit-test geometry
