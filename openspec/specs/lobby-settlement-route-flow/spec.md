# lobby-settlement-route-flow Specification

## Purpose
TBD - created by archiving change add-lobby-and-settlement-route-flow. Update Purpose after archive.
## Requirements
### Requirement: Minimal Lobby start surface
Lobby SHALL display the game title, a primary Start action, and visible non-interactive reserved areas for level selection, growth, and settings.

#### Scenario: Start default level
- **WHEN** the player activates Start while the navigator is Idle
- **THEN** Lobby requests level `orchard-01` using a new session ID, nonzero seed, and current bundled content version

#### Scenario: Reserved area input
- **WHEN** the player activates a reserved area
- **THEN** no route or gameplay state changes and the area remains visibly unavailable

### Requirement: Settlement result display
Settlement SHALL display outcome, reached wave, and remaining lives from the completed Battle result.

#### Scenario: Valid result
- **WHEN** Settlement receives a valid result for the completed session
- **THEN** the displayed values exactly match that result

#### Scenario: Missing result
- **WHEN** Settlement has no valid result
- **THEN** the app returns safely to Lobby with a recoverable structured error

### Requirement: Return and retry actions
Settlement SHALL support Return to Lobby and Retry without reusing completed session identity.

#### Scenario: Return
- **WHEN** Return is activated
- **THEN** completed session/result data is cleared before Lobby becomes interactive

#### Scenario: Retry
- **WHEN** Retry is activated
- **THEN** a new request uses the same level and content version with a new session ID and seed

### Requirement: Portrait interaction contract
Lobby and Settlement draw and hit-test geometry MUST derive from the same portrait layout and respect the current safe-area behavior.

#### Scenario: WebGL portrait capture
- **WHEN** either shell renders at the 402x874 acceptance viewport
- **THEN** all required copy and actions are visible, controls do not overlap, and no black or transparent frame is produced

