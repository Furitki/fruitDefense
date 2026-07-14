## ADDED Requirements

### Requirement: No persistent bottom session-action row
The portrait interface SHALL NOT display the former bottom row containing persistent wave-start and restart buttons, and SHALL make the reclaimed vertical space available to the gameplay layout.

#### Scenario: Normal ready screen
- **WHEN** a new run is displayed at the portrait reference viewport
- **THEN** there is no two-button session-action row below the build and information surfaces

#### Scenario: Active wave screen
- **WHEN** a wave is playing
- **THEN** the former bottom wave and restart buttons remain absent

### Requirement: Contextual battlefield wave action
The interface SHALL place the available wave-start action inside the battlefield's right-side control region and SHALL derive its label and visibility from the current game phase.

#### Scenario: Ready phase
- **WHEN** the run is ready before the first wave
- **THEN** a button labeled `开始波次` is visible on the battlefield and activating it starts the first wave

#### Scenario: Active wave
- **WHEN** the game phase is playing
- **THEN** no wave-start button is visible and active wave/enemy status is shown instead

#### Scenario: Between-wave countdown
- **WHEN** the previous wave is complete and the automatic next-wave timer is counting down
- **THEN** a button labeled `立即开始下一波` is visible and activating it starts the next wave immediately

#### Scenario: Terminal phase
- **WHEN** the game is in victory or defeat
- **THEN** no battlefield wave-start button is visible

### Requirement: Pause modal actions
The non-terminal pause modal SHALL present separate `继续游戏` and `重新开始` buttons, each with an interactive target at least 44 logical points on its shortest dimension.

#### Scenario: Continue from pause
- **WHEN** the player activates `继续游戏`
- **THEN** the pause modal closes and the same run resumes without resetting its resources, wave, plants, or enemies

#### Scenario: Restart from pause
- **WHEN** the player activates `重新开始`
- **THEN** the current run is reset immediately, the pause modal closes, and a clean ready state is displayed

#### Scenario: Keyboard resume
- **WHEN** the game is paused and the player presses the existing pause keyboard shortcut
- **THEN** the game resumes with the same outcome as the continue button

### Requirement: Centralized restart cleanup
Every restart surface SHALL use one reset path that resets simulation state and clears transient presentation state.

#### Scenario: Restart clears UI state
- **WHEN** restart is activated while a plant, tool, drag, nursery reward, or transient message is active
- **THEN** the new run contains no stale selection, range overlay, tool mode, drag ghost, reward pulse, or paused state

#### Scenario: Terminal restart
- **WHEN** the player activates restart from victory or defeat
- **THEN** the same clean ready state is produced as restart from the pause modal

### Requirement: Safe battlefield control geometry
The contextual wave action and pause modal actions SHALL remain inside the portrait safe area, SHALL use shared draw/hit-test rectangles, and SHALL not obscure required battlefield targets.

#### Scenario: Reference viewport
- **WHEN** the interface renders at 402 by 874 logical points
- **THEN** all session controls are fully visible, touch-sized, and do not overlap the route, core, or required planting targets

#### Scenario: Safe-area insets
- **WHEN** top or bottom safe-area insets are present
- **THEN** the wave and pause actions remain fully operable within the usable content region

### Requirement: Session-control WebGL acceptance
The project SHALL capture and verify ready, active-wave, between-wave, paused, continued, and restarted states from a real WebGL canvas before publication.

#### Scenario: Complete control-flow acceptance
- **WHEN** the portrait acceptance command completes successfully
- **THEN** evidence shows the correct phase-specific wave labels, both pause actions, a successful continue, a clean restart, and the absence of the former bottom action row
