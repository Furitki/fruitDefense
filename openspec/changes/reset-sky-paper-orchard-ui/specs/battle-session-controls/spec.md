## MODIFIED Requirements

### Requirement: Contextual battlefield wave action
The interface SHALL place the available phase-specific Wave action inside the independent flow row immediately below the gameplay stage, SHALL derive its label and visibility from the current game phase, and SHALL NOT expose the command through the battlefield projection or a compatibility target.

#### Scenario: Ready phase
- **WHEN** the run is ready before the first wave
- **THEN** a button labeled `开始波次` is visible in the phase/Wave row and activating it starts the first wave

#### Scenario: Active wave
- **WHEN** the game phase is playing
- **THEN** no Wave command is visible or interactive, active wave/enemy status is shown in the same persistent row, and the lower control tracks do not move

#### Scenario: Between-wave countdown
- **WHEN** the previous wave is complete and the automatic next-wave timer is counting down
- **THEN** a button labeled `立即开始下一波` is visible in the phase/Wave row and activating it starts the next wave immediately

#### Scenario: Terminal phase
- **WHEN** the game is in victory or defeat
- **THEN** no Wave command is visible or interactive in either the phase row or gameplay stage

### Requirement: Safe battlefield control geometry
The independent phase/Wave row and pause modal actions SHALL remain inside the portrait safe area, SHALL use shared draw/hit-test rectangles from the authoritative layout, and SHALL not obscure required battlefield, context, nursery, or refresh targets.

#### Scenario: Reference viewport
- **WHEN** the interface renders at 402 by 874 logical points
- **THEN** all session controls are fully visible, touch-sized, and the phase/Wave row is non-overlapping and separate from the route, core, and required planting targets

#### Scenario: Safe-area insets
- **WHEN** top or bottom safe-area insets are present
- **THEN** the phase/Wave and pause actions remain fully operable within the usable content region and retain their recorded pointer mapping

### Requirement: Session-control WebGL acceptance
The project SHALL capture and verify ready, active-wave, between-wave, paused, continued, restarted, and terminal states from a real WebGL canvas before publication, including the independent flow-row owner and absence of the removed in-stage target.

#### Scenario: Complete control-flow acceptance
- **WHEN** the portrait acceptance command completes successfully
- **THEN** evidence shows the correct phase-specific Wave labels and row states, both pause actions, a successful continue, a clean restart, the absence of the former bottom action row, and no in-stage or duplicate Wave action
