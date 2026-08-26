## MODIFIED Requirements

### Requirement: Semantic action role and content pairing

Every action SHALL resolve its container, content, outline, and state cue from an explicit action role and state. Text, action glyphs, and compact multiplier content on the same action SHALL use the same resolved content role. Content color SHALL NOT be inferred from an icon filename or baked into an action glyph.

#### Scenario: Start wave renders on a Primary surface
- **WHEN** the Battle ready phase renders the start-wave action
- **THEN** its label and play glyph both use `primary.content` on `primary.container`, remain visibly one content group, and meet the required final-pixel contrast

#### Scenario: The same play glyph renders as Continue
- **WHEN** the paused compact control renders the shared play/continue glyph
- **THEN** the glyph keeps the same geometry but uses the compact control's resolved content color rather than the Primary action color or a baked soil-brown color

### Requirement: Orthogonal role, form, and behavior

Action role (`Primary / Secondary / Quiet / Danger`), content form (text, icon-label, icon-only, compact multiplier), and behavior (instantaneous or persistent mode) SHALL be explicit orthogonal inputs. Start wave SHALL be Primary, nursery refresh SHALL be Secondary, pause/continue and speed SHALL be Quiet persistent modes, and close SHALL be a Quiet instantaneous command.

#### Scenario: Ready-phase action hierarchy is reviewed
- **WHEN** start wave and nursery refresh are visible together
- **THEN** start wave is the clear flow-advancing Primary action and refresh uses the lower-emphasis Secondary pairing

### Requirement: Tintable action glyph contract

Action glyph assets SHALL be monochrome neutral masters or alpha masks with stable canvas, optical bounds, and alpha silhouette. They SHALL NOT bake soil-brown, amber, gradient, highlight, shadow, or interaction-state colors into visible pixels. Tintability governs color ownership and SHALL NOT require different actions to share generic or indistinct geometry; glyph families MAY use distinct silhouettes, negative space, proportions, and rhythm while preserving their common canvas and weight contract. Resource/content icons MAY retain intrinsic color but SHALL use a distinct resource-icon rendering path.

#### Scenario: Production ArtSets are validated
- **WHEN** action glyphs in both production ArtSets are inspected
- **THEN** every action glyph is tintable without hue contamination, preserves its authoritative geometry, and its manifest/hash/import metadata is current

### Requirement: Single resolved compact-control surface

Compact controls SHALL render exactly one resolved container surface per frame. An active ArtSet variant MAY replace the inactive variant, but inactive and active surfaces SHALL NOT be drawn as simultaneous button layers or expose two perimeter structures. Superseded overlay-opacity properties and no-op active-cycle tokens SHALL NOT remain as compatibility APIs. Interaction cues and mode lifecycle SHALL remain orthogonal and contained in the unchanged 52×52 draw/hit rectangle.

#### Scenario: A mode activates or deactivates
- **WHEN** pause or speed transitions through Activating or Deactivating
- **THEN** the renderer interpolates or switches one complete resolved surface/content pairing without stacking inactive and active button faces

#### Scenario: A compact control is active and pressed
- **WHEN** the player presses an already-active pause or speed control
- **THEN** pressed feedback is visible while the active container/content pairing and structural mode cue remain recognizable

### Requirement: Complete state pairings and contrast

Normal, hover/focus, pressed, mode-active, and disabled states SHALL each resolve a complete container/content pairing. Final rendered button text SHALL have at least `4.5:1` contrast. Action glyphs SHALL target `4.5:1` and SHALL never fall below `3:1`. Required component boundaries, focus indicators, and state cues SHALL have at least `3:1` contrast against adjacent colors. State meaning SHALL NOT rely on hue alone.

Both production ArtSets' Primary and Danger runtime surfaces SHALL use deep-leaf and deep-terracotta content regions that reach at least `4.5:1` against their warm-white content colors. The semantic container tokens SHALL describe the actual exported surfaces rather than colors applied a second time by the renderer.

#### Scenario: Final Primary pixels are measured
- **WHEN** the start-wave play glyph and label are measured against the actual textured Primary surface in the final WebGL canvas
- **THEN** both pass their thresholds, including the least-contrasting significant interior pixels, and neither merges into the green surface

#### Scenario: Mode states are viewed in grayscale
- **WHEN** inactive and active pause/speed captures are compared without hue
- **THEN** continue versus pause and `1×` versus `2×` remain distinguishable and the active state retains a structural cue

### Requirement: Editor and ordinary-WebGL acceptance

Automated validation SHALL cover both production ArtSets, every action role/state pairing including Disabled, tintable glyph integrity, mutually-exclusive compact surfaces, unchanged geometry, focus-cue raster structure, and contrast calculations. Ordinary WebGL acceptance SHALL capture 402×874 full and representative inset states for the naturally reachable Primary start wave, Secondary refresh, Quiet compact inactive/active, press/focus, and instantaneous close with recorded build/theme/ArtSet identity. It SHALL NOT add a production acceptance-only behavior branch merely to fabricate an otherwise unreachable Disabled action.

#### Scenario: Disabled has no stable production action state
- **WHEN** the current release flow exposes no naturally reachable Disabled action in a stable WebGL frame
- **THEN** Disabled is accepted through the deterministic complete-pairing/final-pixel Editor gate, while WebGL evidence covers all naturally reachable action states without a fake runtime path

#### Scenario: Previous overlay evidence exists
- **WHEN** evidence was produced by the superseded inactive-plus-active overlay renderer or before semantic content pairing was introduced
- **THEN** it SHALL NOT satisfy this requirement and a new build plus new final-pixel capture is required
