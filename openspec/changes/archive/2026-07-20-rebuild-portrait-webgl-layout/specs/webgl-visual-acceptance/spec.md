## ADDED Requirements

### Requirement: Live WebGL canvas capture
The project SHALL provide a repeatable visual acceptance command that opens a local or deployed WebGL build at the target portrait viewport and captures the live Unity canvas at a requested point in time.

#### Scenario: Continuously rendering game
- **WHEN** the Unity render loop never becomes idle
- **THEN** the acceptance command captures through the browser debugging protocol without waiting for page idleness or process exit

### Requirement: Load failure detection
The visual acceptance command SHALL fail with a non-zero result when the page is unreachable, the Unity canvas is missing, the player reports a load error, or a screenshot cannot be produced within the configured timeout.

#### Scenario: Broken WebGL deployment
- **WHEN** a required build artifact is missing or the player cannot initialize
- **THEN** the command reports the failed check and does not mark the visual acceptance as successful

### Requirement: Required portrait evidence states
Portrait acceptance SHALL produce evidence for the initial screen and representative interactive states covering the build tray, a selected-plant detail surface, an active wave, and a blocking modal.

#### Scenario: Complete acceptance run
- **WHEN** the visual acceptance command completes successfully
- **THEN** it records screenshots for every required state at the reference viewport and identifies the source URL and viewport used

### Requirement: Visual acceptance checklist
The portrait build SHALL NOT be publishable unless the captured evidence confirms visible Chinese text, safe-area containment, full-width control use, readable status information, and absence of clipped or overlapping primary controls.

#### Scenario: Text is absent
- **WHEN** a required screenshot contains the game art but omits expected player-facing labels
- **THEN** the acceptance result is failed and publishing is blocked

