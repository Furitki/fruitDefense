## ADDED Requirements

### Requirement: Release scene order
The release build SHALL contain enabled scenes in the order Bootstrap, Lobby, Battle, and Settlement, and Bootstrap SHALL be the only persistent app composition root.

#### Scenario: Cold release start
- **WHEN** the release starts without a development route override
- **THEN** Bootstrap initializes the selected platform and navigates to Lobby

#### Scenario: Scene transition
- **WHEN** the app leaves Battle for Lobby or Settlement
- **THEN** the Battle scene and its session-owned objects are destroyed while Bootstrap remains active

### Requirement: Complete player flow
The app SHALL support Lobby to Battle to Settlement to Lobby and a Settlement retry path using immutable launch requests and single-submit battle results.

#### Scenario: Start battle
- **WHEN** the player activates Start in Lobby
- **THEN** the app creates a new session ID and seed for level `orchard-01` and loads Battle with the current bundled content version

#### Scenario: Return from settlement
- **WHEN** the player returns from Settlement
- **THEN** the completed session and result are cleared before Lobby becomes interactive

#### Scenario: Retry from settlement
- **WHEN** the player retries from Settlement
- **THEN** the app uses the same level and content version with a new session ID, a new seed, and a clean battle state

### Requirement: Serialized and safe routing
The navigator MUST allow at most one route transition at a time and MUST reject duplicate start, completion, return, or retry actions without creating duplicate sessions or results.

#### Scenario: Duplicate start input
- **WHEN** Start is activated repeatedly while Battle is loading
- **THEN** exactly one Battle session is created

#### Scenario: Missing settlement result
- **WHEN** Settlement is entered without a valid Battle result
- **THEN** the app returns to Lobby with a structured recoverable error

#### Scenario: Platform initialization failure
- **WHEN** the selected platform adapter fails initialization
- **THEN** Bootstrap displays a retryable error and does not enter Lobby or silently select another platform

### Requirement: Background battle behavior
The integrated app MUST pause an active battle when the platform enters Background and MUST NOT automatically resume it on Foreground.

#### Scenario: Hide and show during battle
- **WHEN** the platform emits Background followed by Foreground during an active battle
- **THEN** simulation time does not catch up and the battle remains paused until the player resumes

### Requirement: Integrated acceptance surfaces
The project SHALL retain all existing named battle acceptance states and SHALL add a real WebGL acceptance sequence for Lobby, Battle, Settlement, return, retry, and failure-safe routing.

#### Scenario: Direct battle acceptance route
- **WHEN** WebGL loads with `acceptance=1` and `route=battle`
- **THEN** Bootstrap loads Battle, exposes the acceptance bridge, and forwards named state requests after the battle host is ready

#### Scenario: Existing battle states
- **WHEN** the 13-state battle acceptance executes through the new route
- **THEN** every existing screenshot, interaction, delivery, and geometry assertion still passes

#### Scenario: Full-flow acceptance
- **WHEN** the end-to-end acceptance starts from the default URL
- **THEN** it captures and verifies Lobby, active Battle, Settlement, return to Lobby, and retry with no black or transparent frames
