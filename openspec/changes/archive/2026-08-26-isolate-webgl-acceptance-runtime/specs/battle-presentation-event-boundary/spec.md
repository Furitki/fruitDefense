## ADDED Requirements

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

## MODIFIED Requirements

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
