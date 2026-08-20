## ADDED Requirements

### Requirement: Animated geometry integrity
Runtime UI animation SHALL transform only transient visual geometry while drawing and hit-testing continue to derive from the same authoritative portrait layout and safe-area projection.

#### Scenario: Control is visually scaled or offset
- **WHEN** press, pop, or reveal motion temporarily scales or offsets a control's artwork and content
- **THEN** its hit target remains the original touch-safe rectangle, input mapping remains aligned, and the animated pixels stay inside the permitted safe-area or decorative overflow boundary

#### Scenario: Motion reaches rest
- **WHEN** an animation completes or reduced motion resolves it immediately
- **THEN** every component returns exactly to the static rectangles, optical alignment, gutters, and containment used by portrait validation
