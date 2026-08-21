## ADDED Requirements

### Requirement: Target-raster resource evidence
Canonical WebGL visual acceptance SHALL record final-raster evidence for every micro resource icon at its actual displayed size and SHALL fail when its visible envelope, optical center, critical feature, or pairwise silhouette distinction violates the approved target-size contract.

#### Scenario: Reference battle header is captured
- **WHEN** the stable ready-state frame is captured at 402×874 full and inset
- **THEN** the acceptance manifest records the three micro icon bounds and silhouette checks from the live canvas rather than inferring success from their layout rectangles

### Requirement: Route depth and occupancy evidence
Canonical WebGL visual acceptance SHALL measure the approved Lobby and Settlement hero/content/action regions and SHALL retain live screenshots proving intentional vertical occupancy and background/content/foreground value separation.

#### Scenario: Cross-route matrix completes
- **WHEN** Lobby default/alternate, Battle ready/paused, and Settlement victory/defeat states are captured
- **THEN** each route records its occupied-content bounds, required visual anchor, safe-area containment, backdrop presence, and absence of clipped, stretched, low-contrast, or top-heavy composition

#### Scenario: Settlement outcome and metrics are captured
- **WHEN** stable victory and defeat frames are captured at 402×874 full and inset
- **THEN** the acceptance result measures outcome glyph containment against the banner's significant-alpha bounds and verifies that the three read-only metric rows have no independent closed borders

#### Scenario: New backdrop is missing or stale
- **WHEN** the live build does not contain the expected route illustration hash or the screenshot lacks the declared backdrop layer
- **THEN** acceptance fails and does not sign screenshots from an older payload

### Requirement: Emphasis typography final-raster evidence
Canonical WebGL visual acceptance SHALL measure emphasis typography from the composited live canvas, including the distinct outline, and SHALL compare its actual ink envelope with the decorative backing's significant-alpha envelope.

#### Scenario: Settlement outcome evidence is recorded
- **WHEN** victory and defeat are captured at the 402×874 reference viewport
- **THEN** the manifest records fill-plus-outline glyph bounds, outline thickness/visibility, banner significant-alpha bounds, vertical occupancy, four-side padding, and top/bottom imbalance, and fails any out-of-range value instead of accepting nominal font size or owner-rect containment as proof
