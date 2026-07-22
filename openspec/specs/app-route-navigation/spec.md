# app-route-navigation Specification

## Purpose
TBD - created by archiving change introduce-app-bootstrap-and-platform-boundary. Update Purpose after archive.
## Requirements
### Requirement: Guarded application routes
The application navigator SHALL start at Lobby and SHALL allow only Lobby to Battle, Battle to Settlement, and Settlement to Lobby or Battle transitions.

#### Scenario: New navigator is created
- **WHEN** an `AppNavigator` is constructed
- **THEN** its current route is Lobby, its state is Idle, and it has no pending transition or error

#### Scenario: Valid transition is requested
- **WHEN** the caller begins an allowed transition while no transition is loading
- **THEN** the navigator enters Loading and records the destination without changing the current route

#### Scenario: Invalid transition is requested
- **WHEN** the caller requests a route edge outside the allowed graph
- **THEN** the request is rejected with `route-not-allowed` and the current route remains unchanged

### Requirement: Two-phase transition completion
The navigator SHALL change its current route only after an active transition is completed successfully and SHALL retain the current route when that transition fails.

#### Scenario: Loading completes
- **WHEN** the caller completes an active transition
- **THEN** the pending route becomes current, transition state returns to Idle, and route change is emitted once

#### Scenario: Loading fails
- **WHEN** the caller fails an active transition with an error code
- **THEN** the current route is retained, transition state becomes Failed, and the error code is exposed

#### Scenario: Duplicate request occurs while loading
- **WHEN** another transition is requested while one is Loading
- **THEN** it is rejected with `transition-in-progress` without replacing the pending route

### Requirement: Recoverable route failure
A failed navigator SHALL allow the caller to retry a valid transition without reconstructing the application root.

#### Scenario: Valid transition is retried after failure
- **WHEN** a valid transition is requested while the navigator is Failed
- **THEN** the previous error is cleared and a new Loading transition begins

### Requirement: Navigation remains presentation-independent
The navigator SHALL contain no scene names, Unity scene-loading operations, gameplay state, or player-visible UI behavior.

#### Scenario: Route transition begins
- **WHEN** a valid transition enters Loading
- **THEN** no Unity scene is loaded until a separate integration layer performs the load and completes the transition

