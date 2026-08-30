## ADDED Requirements

### Requirement: Shared plant drag connector and target frame
The runtime UI SHALL render the plant drag connector and target frame through one shared, allocation-free presentation helper using semantic theme colors, an approved production nine-slice frame resource, existing drag-state icons, and finite contained geometry. The target frame SHALL NOT be synthesized from primitive edge rectangles.

#### Scenario: Drag state is communicated without hue
- **WHEN** legal, illegal, merge, or swap feedback is visible
- **THEN** the target-sized frame is paired with the existing distinct legal, prohibition, merge, or swap icon so the result does not depend only on color

#### Scenario: Connector is rendered under portrait scaling
- **WHEN** the connector is drawn at a supported full or inset portrait safe area in ordinary WebGL
- **THEN** every dash remains finite, visible, clipped to the design-space presentation, and aligned with the same GUI transform as its source and destination

#### Scenario: Connector is rendered in a letterboxed PC window
- **WHEN** the portrait design is contained inside a wide or tall standalone-player viewport with fractional scale and a non-zero viewport offset
- **THEN** connector endpoints are projected once into device space, dash rotation does not reapply the letterbox offset, and the line remains aligned with its source and destination

#### Scenario: Target frame is rendered
- **WHEN** a legal, illegal, merge, or swap target is resolved
- **THEN** the authoritative target rectangle uses the approved transparent-center nine-slice UI resource with semantic tint and the distinct existing state icon rather than a procedurally drawn four-edge frame

#### Scenario: Reduced motion is requested
- **WHEN** reduced-motion presentation is active
- **THEN** the static connector, target frame, and semantic icon remain visible without travel, looping, or pulsing motion
