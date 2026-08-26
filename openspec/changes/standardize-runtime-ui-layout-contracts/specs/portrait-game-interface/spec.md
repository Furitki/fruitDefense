## ADDED Requirements

### Requirement: Aligned Battle structural tracks

The Battle Header and BattleStage SHALL use one authoritative top-level horizontal owner track and a 4-point-derived vertical rhythm. Header and persistent sections SHALL use the light structural family with 1–2 capture-pixel outlines, while BattleStage SHALL be the sole normal-state heavy structural anchor with a 3–5 capture-pixel outline. The lower control stack SHALL NOT introduce a second enclosing outer frame. Nested context-tool/detail, nursery, and refresh regions SHALL use declared shared owner tracks and SHALL NOT introduce page-local outer widths, outline weights, or spacing scales.

#### Scenario: Ready Battle is rendered
- **WHEN** Battle renders at any supported full or inset portrait viewport
- **THEN** the visible left and right edges of Header and BattleStage align to their shared owner track, Header and persistent section outlines remain light at 1–2 capture pixels, BattleStage is the only normal-state heavy frame at 3–5 capture pixels, no second outer frame encloses the lower controls, and all nested section edges follow their declared tracks

#### Scenario: Battle layout is extended
- **WHEN** a new Battle section or player-visible control is added
- **THEN** it selects an existing named owner/inner track, spacing token, semantic structural-weight role, typography role, and line policy instead of adding unclassified coordinates, a local frame style, or another heavy enclosing frame

### Requirement: Separated Battle header rows

The Battle header SHALL place its title and two Quiet persistent-mode controls in a primary row and its three resource metrics in a separate repeated metric row. Neither row SHALL overlap the other, and every text/icon group SHALL fit its owner at the packaged-font line height.

#### Scenario: Header values reach their release boundary samples
- **WHEN** sun, lives, and wave values are rendered with their declared maximum acceptance samples
- **THEN** all three icon-label-value groups remain contained and aligned without clipping, implicit shrink-to-fit, overlap, or collision with pause/speed controls

### Requirement: Bounded Battle text anatomy

Every Battle title, label, metric, status, tool count, nursery label, refresh cost, detail field, merge hint, and modal copy SHALL declare a semantic typography role, alignment, finite line policy, authoritative owner rectangle, and release boundary sample. A single-line owner SHALL be at least the role's theme line-height and SHALL NOT silently compress the line box.

#### Scenario: Dynamic content is validated
- **WHEN** formatted numeric values and production-authored plant/equipment names are evaluated
- **THEN** their minimum and maximum contract samples fit the same runtime anatomy used by the presenter at every supported full and inset viewport

#### Scenario: New content exceeds the existing contract
- **WHEN** a content or formatter change no longer fits its declared owner and finite line policy
- **THEN** validation fails until the copy, authoritative layout, or explicitly controlled multi-line anatomy is corrected, without runtime truncation, ellipsis, generic word wrapping, or font shrinking
