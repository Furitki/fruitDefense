## ADDED Requirements

### Requirement: Live structural-frame and text-ink evidence

Canonical ordinary-WebGL acceptance SHALL measure top-level structural frame edges, final-pixel outline bands, and rendered text ink containment from the live canvas in addition to logical rectangle containment. Evidence SHALL use the same current payload, theme, ArtSet, and safe-area identity as the route interaction capture.

#### Scenario: Battle full and inset frames are captured
- **WHEN** ready, active/between-wave, detail, paused, and terminal Battle states are captured at 402×874 full and the canonical representative inset
- **THEN** the manifest records aligned header/Battle-surface visible edges, matching peer outline weight, declared inner tracks, and absence of text ink outside every required owner

#### Scenario: Logical rectangles pass but pixels disagree
- **WHEN** layout rectangles are contained but a nine-slice shadow/outline, clipped glyph, compressed line box, or dynamic value creates a visible mismatch
- **THEN** acceptance fails as a product defect and does not set `accepted=true` from screenshot existence, coarse pixel sentinels, or unrelated action-state checks

### Requirement: Canonical Battle viewport matrix

Battle geometry and text validation SHALL project every authoritative owner through 360×800, 375×812, 402×874, and 430×932 full and canonical inset safe areas, including device-pixel rounding. Repeating the unprojected 402×874 logical rectangles SHALL NOT count as viewport-matrix coverage.

#### Scenario: Narrow or inset projection is evaluated
- **WHEN** a Battle owner or line box lands on fractional device pixels after safe-area scaling
- **THEN** the projected, snapped bounds and rendered ink remain contained with the declared gap and no outline or glyph clipping
