## ADDED Requirements

### Requirement: Battle structural-weight and closeout evidence
Canonical ordinary-WebGL acceptance SHALL measure Battle's live structural surfaces, outline bands, occupied-content bounds, lower closeout, text containment, and context-tray state from the composited canvas rather than inferring the visual hierarchy from logical rectangles or a concept image.

#### Scenario: Ready and active Battle are captured
- **WHEN** 402×874 full and inset ready/active states are recorded
- **THEN** the manifest identifies one gameplay-stage heavy frame, light Header/context/nursery sections, no enclosing legacy lower-page frame, all required text ink within owners, and the refresh action's lower edge plus remaining safe-content margin

#### Scenario: Selected plant detail is captured
- **WHEN** the canonical detail state is recorded
- **THEN** the context tray contains the selected-plant title, finite attributes, and close action instead of the tool row, while nursery, refresh, and wave action remain visible and recorded hit targets activate the expected controls

#### Scenario: Structural hierarchy regresses
- **WHEN** a second unapproved heavy large frame, hidden empty detail band, overflowed copy, mixed ArtSet binding, or stale stage asset is detected
- **THEN** acceptance fails and publishing remains blocked until the current payload is rebuilt and the canonical matrix passes
