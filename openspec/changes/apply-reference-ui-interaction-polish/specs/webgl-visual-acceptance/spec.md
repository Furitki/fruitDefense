## ADDED Requirements

### Requirement: Temporal WebGL motion evidence
Canonical ordinary-WebGL acceptance SHALL capture deterministic start, representative midpoint, interrupted, reduced-motion, and resting checkpoints for the changed Lobby, Battle, and Settlement feedback patterns.

#### Scenario: Motion evidence is recorded
- **WHEN** the interaction-polish acceptance run executes on the real Unity canvas
- **THEN** its manifest records route, motion pattern, checkpoint time, viewport, safe area, theme and ArtSet identity, build identity, input result, and screenshot path

#### Scenario: Resting state differs from static authority
- **WHEN** a motion finishes or is cancelled and the resulting component bounds, opacity, input target, or content differ from the canonical static layout
- **THEN** acceptance fails and the build is not considered visually complete
