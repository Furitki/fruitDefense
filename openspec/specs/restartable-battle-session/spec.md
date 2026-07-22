# restartable-battle-session Specification

## Purpose
TBD - created by archiving change extract-restartable-battle-session. Update Purpose after archive.
## Requirements
### Requirement: Immutable session initialization
A Battle host SHALL initialize exactly once from a valid launch request containing session ID, level ID, seed, and content version.

#### Scenario: Valid initialization
- **WHEN** an uninitialized host receives a valid request and matching content
- **THEN** it creates a clean simulation using the request seed and reports itself ready

#### Scenario: Repeated initialization
- **WHEN** an initialized host receives another initialization request
- **THEN** it rejects the request without resetting or creating a second simulation

### Requirement: Single terminal result
Each Battle session MUST submit at most one immutable result to the app navigator.

#### Scenario: Repeated terminal frames
- **WHEN** victory or defeat remains visible across multiple frames
- **THEN** exactly one result is submitted for the session

### Requirement: Disposable battle lifetime
Leaving Battle SHALL destroy all session-owned simulation, presenter, callback, and transient state while leaving the app Bootstrap active.

#### Scenario: Return then start
- **WHEN** a completed battle returns to Lobby and a new battle starts
- **THEN** only the new host exists and no entity, pause, selection, or event state leaks from the old session

### Requirement: Platform background pause
An active Battle MUST pause and clear its frame accumulator on Background and MUST remain paused after Foreground until the player resumes.

#### Scenario: Long background interval
- **WHEN** the app remains in Background longer than one simulation step and returns
- **THEN** no background time is replayed and the battle remains paused

### Requirement: Existing battle control compatibility
The session host SHALL retain current local pause, continue, restart, and named acceptance behavior.

#### Scenario: Pause restart
- **WHEN** the player restarts from the pause modal
- **THEN** the same launch request is reset to a clean Ready state without submitting a settlement result

