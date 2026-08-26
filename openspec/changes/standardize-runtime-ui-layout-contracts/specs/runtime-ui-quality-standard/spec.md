## ADDED Requirements

### Requirement: Named layout tracks and spacing rhythm

Runtime route layouts SHALL derive equivalent outer frames, inner content tracks, insets, gaps, and line boxes from named layout constants based on the release theme's 4-point spacing rhythm. Equivalent siblings SHALL share edges or declare an intentional difference that is represented by a semantic component role.

#### Scenario: Peer structural panels are inspected
- **WHEN** two panels act as peer structural frames on one route
- **THEN** automated validation confirms their authoritative edge relationship, surface role, outline contract, content inset, and spacing rhythm instead of accepting independent contained rectangles

### Requirement: Non-compressing finite line boxes

Shared single-line text layout SHALL use the semantic role's theme line-height. It SHALL report an invalid layout when the owner is shorter than that line-height and SHALL NOT reduce the line box to the owner's height. Generic wrapping text SHALL NOT be used for finite runtime chrome.

#### Scenario: Owner is too short
- **WHEN** a single-line title, label, metric, count, or detail field is assigned an owner shorter than its resolved line-height
- **THEN** the quality gate fails with the owner, role, required height, and affected copy rather than rendering a compressed or clipped glyph box

### Requirement: Dynamic text boundary registry

Every runtime formatter and content-authored text field SHALL register finite release boundary samples and SHALL be measured with the packaged font, real semantic action spec, actual owner rectangle, state, and viewport projection. Stable-copy inspection and dynamic-boundary inspection SHALL use the same fit and containment rules.

#### Scenario: Formatter or content domain changes
- **WHEN** a resource value, count, cost, star string, status, plant name, equipment name, or detail composition gains a wider possible value
- **THEN** its boundary registry and owning layout must be updated in the same change, and aggregate smoke fails if the new sample clips or is silently clamped

#### Scenario: Shared resolver constrains content
- **WHEN** an icon-label, metric, inline status, or controlled two-line resolver cannot allocate the full measured content
- **THEN** it exposes a failed fit result to validation and does not treat a clamped rectangle or unsplittable single-line fallback as success

### Requirement: Panel visual-structure validation

Production ArtSets SHALL validate panel-family nine-slice scale, protected border metadata, significant-alpha optical envelope, and declared outline role. Runtime routes SHALL use the same panel slot for structural peers that require identical final-pixel borders.

#### Scenario: Structural peer uses a different border treatment
- **WHEN** a route maps equivalent structural peers to different panel slots or incompatible visible envelopes
- **THEN** Editor validation fails before WebGL capture and identifies the route regions and ArtSet bindings that disagree
