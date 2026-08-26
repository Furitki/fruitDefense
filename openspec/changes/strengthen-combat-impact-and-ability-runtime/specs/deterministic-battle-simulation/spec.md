## ADDED Requirements

### Requirement: Read-only presentation interpolation fraction
The simulation host SHALL expose a read-only normalized fraction representing retained frame time toward the next 0.05-second step, and reading that fraction SHALL NOT advance, discard, or mutate authoritative state.

#### Scenario: Partial accumulated frame
- **WHEN** frame advancement retains 0.025 seconds after completed fixed steps
- **THEN** the presentation interpolation fraction is 0.5 and repeated reads leave checksum, random state, and accumulator unchanged

#### Scenario: Pause or terminal state
- **WHEN** the battle is paused or terminal and pending frame time is reset
- **THEN** the presentation interpolation fraction is zero

### Requirement: Authoritative step remains unchanged by interpolation
Presentation interpolation SHALL use previous/current render samples and SHALL NOT increase the fixed-step rate, change speed-mode equivalence, or feed interpolated positions into target selection, collision, damage, status, reward, or victory rules.

#### Scenario: Interpolated and headless peers
- **WHEN** one simulation is rendered with interpolation reads and an identical peer runs headless
- **THEN** both consume the same logical steps and produce the same deterministic gameplay checksum and random state
