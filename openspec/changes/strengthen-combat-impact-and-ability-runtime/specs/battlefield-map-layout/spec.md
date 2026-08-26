## MODIFIED Requirements

### Requirement: Shared battlefield projection
The portrait presentation SHALL derive visual surfaces, route geometry, gameplay-marker positions, base cell and flowerpot rectangles, entities, effects, feedback, drag targets, and hit-test regions from one battlefield projection for the current board rectangle. Bounded transient presentation reactions MAY offset or deform rendered world sprites after projection, but SHALL NOT alter canonical layout, drag, drop, or hit-test geometry.

#### Scenario: Flowerpot is drawn and clicked
- **WHEN** an active flowerpot is rendered at a canonical marker/cell without a transient reaction
- **THEN** its visual rectangle, click target, drag target, and drop highlight use the same projected bounds

#### Scenario: Battlefield feedback is active
- **WHEN** a bounded impact profile applies a transient battlefield or entity render offset
- **THEN** only the world visual pass receives that local offset and HUD, button, drag, drop, and hit-test geometry continue using the unmodified projected bounds

#### Scenario: Projectile leaves a plant
- **WHEN** a plant creates a projectile or combat effect
- **THEN** the canonical projected source aligns with the rendered plant and the canonical projected target aligns with the rendered enemy before presentation-only reaction offsets

#### Scenario: Gameplay marker is presented
- **WHEN** a spawn, goal, core, item, or trigger marker has a visible presentation
- **THEN** its base screen position is derived from its canonical cell through the same projection and remains aligned at every supported portrait safe area
