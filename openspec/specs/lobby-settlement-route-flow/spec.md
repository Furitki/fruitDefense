# lobby-settlement-route-flow Specification

## Purpose
TBD - created by archiving change add-lobby-and-settlement-route-flow. Update Purpose after archive.
## Requirements
### Requirement: Lobby Hub route surface
Lobby SHALL display one `TopBar → PageHost → BottomNavigation` Hub with Home, Activity, and Growth destinations while keeping route navigation owned by the existing Lobby, Battle, and Settlement flow.

#### Scenario: Start selected level
- **WHEN** the player activates Home Start while the navigator is Idle
- **THEN** Lobby requests the visibly selected real level using a new session ID, nonzero seed, and current bundled content version; a cold profile defaults that selection to `orchard-01`

#### Scenario: Switch Hub destination
- **WHEN** the player activates Home, Activity, or Growth in the bottom navigation
- **THEN** only the Lobby PageHost content changes and no scene route or gameplay session is created

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

