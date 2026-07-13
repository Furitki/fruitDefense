## MODIFIED Requirements

### Requirement: Continuous drag feedback
The system SHALL show a lifted source, a compact plant preview offset from the pointer, and a distinct hovered destination throughout a plant drag on mouse and pointer/touch input. The preview MUST NOT cover the pointer hotspot or the center of the hovered destination and MUST remain non-interactive for hit testing.

#### Scenario: Dragging across the battlefield
- **WHEN** the player drags a nursery or field plant across several pots
- **THEN** the offset preview follows the pointer while the currently hovered pot alone receives the strongest target emphasis and remains visually readable

#### Scenario: Dragging near a viewport edge
- **WHEN** the pointer approaches an edge where the default preview offset would move content off-screen
- **THEN** the preview offset flips or clamps to remain visible without covering the destination
