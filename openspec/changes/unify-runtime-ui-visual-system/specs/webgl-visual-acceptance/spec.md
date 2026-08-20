## ADDED Requirements

### Requirement: Cross-route visual-system evidence
The ordinary WebGL visual acceptance run SHALL capture the real release canvas for Bootstrap startup/error, Lobby default and selected states, representative Battle HUD, detail, interaction and modal states, and Settlement victory/defeat states using the same release theme and production UI assets.

#### Scenario: Complete visual-system acceptance runs
- **WHEN** the candidate WebGL build is reviewed at the reference portrait viewport
- **THEN** evidence covers every required route and component state, identifies the theme ID plus active art-set ID and revision, and contains no default-skin, legacy-style, mixed-set, experimental-art, black-frame, or transparent-frame surface

### Requirement: Full and inset portrait comparison
Visual-system acceptance SHALL compare the required release-flow states at the 402-by-874 full safe area and at least one representative top-and-bottom inset safe area, and SHALL fail when shared artwork, text, state cues, or controls clip, stretch incorrectly, overlap, or drift from interaction geometry.

#### Scenario: Shared component is distorted by safe-area scaling
- **WHEN** a panel corner, button outline, icon safe inset, Chinese label, or visible control target differs incorrectly between full and inset captures
- **THEN** acceptance fails and identifies the affected route, state, viewport, and component

### Requirement: Visual consistency review checklist
Publication SHALL require a cross-route review of semantic color use, typography hierarchy, spacing rhythm, surface family, action priority, selected/disabled/error cues, non-color state indicators, UI-art integrity, and separation between stable application chrome and level-specific battlefield content.

#### Scenario: One route retains a local visual language
- **WHEN** a captured route uses an unapproved color, font treatment, component shape, default skin, or duplicated legacy control style for an equivalent semantic role
- **THEN** the candidate fails visual-system acceptance even if that route remains functionally operable
