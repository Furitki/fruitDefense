## MODIFIED Requirements

### Requirement: Contextual optional edge refinement
The painter SHALL expose no separate pure-base preset cards, SHALL let the selected ordinary terrain brush opt into a contextual `只绘制纯图` mode, SHALL resolve that pure visual from the selected brush's configured opaque endpoint rather than an unrelated global thumbnail, SHALL use the target's preconfigured contour without exposing a square/organic switch in the ordinary laboratory, SHALL let one contour-specific edge resource serve both pair directions through complemented reverse masks, and MUST disable unavailable combinations with an actionable reason and no cross-contour substitution.

#### Scenario: Pure-only mode is active
- **WHEN** an author selects an ordinary material or pair brush and enables `只绘制纯图`
- **THEN** that brush writes its own configured opaque foreground endpoint, clears landform and edge state in the touched cell, and does not substitute an unrelated material thumbnail or reverse-pair asset

#### Scenario: Full-composite trial brush enters pure-only mode
- **WHEN** a reviewed full-composite sixteen-mask brush provides opaque background at mask `0` and opaque foreground at mask `15`
- **THEN** the trial binding uses those exact endpoints for pure background and foreground previews and writes, so toggling pure-only mode does not switch to another texture family

#### Scenario: Primary brush chooser is shown
- **WHEN** the embedded terrain laboratory displays its ordinary brushes
- **THEN** it shows only A-on-B and B-on-A as square preview cards in one row, exposes pure output through the contextual option, and shows no duplicate landform-only or pure-material brush card

#### Scenario: Ordinary laboratory uses the configured contour
- **WHEN** the embedded terrain laboratory is shown for a target configured as square or organic
- **THEN** it keeps that target's configured contour and exposes no `方形` / `自然` option switch

#### Scenario: One square painted edge serves both directions
- **WHEN** either ordered direction of a material pair has the square painted edge and the painted edge is selected
- **THEN** both pair brushes use that pre-authored edge asset, with the reverse direction selecting the complemented mask and no image generation or source-pixel processing

#### Scenario: Registered production brushes are shown
- **WHEN** the canonical map editor or terrain laboratory resolves valid imported brush definitions
- **THEN** both authoring surfaces enumerate the same registry in stable order and select the exact semantic base, landform, contour, and edge combination without pair-specific hard-coded buttons

#### Scenario: Registered brush library is previewed
- **WHEN** the terrain laboratory contains two or more valid registered brush definitions
- **THEN** one scrollable preview gallery shows every definition at the same time as a real assembled composition card in stable registry order, labels its material pair and available direction count, and does not require applying one definition before the others can be discovered

#### Scenario: Registered brush card preserves artwork proportions
- **WHEN** a registered composition is drawn above its card footer
- **THEN** the assembled preview occupies a centered square rect and every mask sprite is uniformly scaled without horizontal or vertical stretching

#### Scenario: Production pair dependency is missing
- **WHEN** a shortcut lacks its base texture, foreground square landform, or exact directed refined edge
- **THEN** that shortcut is disabled and does not substitute another material pair or contour

#### Scenario: Laboratory selects a registered brush
- **WHEN** the author selects a registered brush on an empty terrain-laboratory target
- **THEN** the target uses the brush foreground as material A, background as material B, its registered endpoint bases, reusable foreground landform, and exact pair edge, then exposes only directions whose landform dependency exists

#### Scenario: Laboratory target already contains authored cells
- **WHEN** changing brush would reinterpret existing generic A/B marker cells
- **THEN** the laboratory keeps the complete registered preview gallery visible, refuses the switch with an actionable clear-canvas instruction, and preserves the existing cells

#### Scenario: Only organic refinement exists
- **WHEN** the requested square pair lacks its exact edge but the organic pair is registered
- **THEN** the painted edge is disabled with the missing-contour reason while the square base edge remains selectable

### Requirement: Direct Scene painting with bounded lifecycle and Undo
The terrain-resource acceptance Overlay SHALL show the active material, configured contour, and edge treatment in the Overlay and Scene view, SHALL support left-drag painting, SHALL group one mouse gesture into one Undo operation, and MUST release Scene input when painting stops, the target becomes invalid, play mode begins, or the Overlay is hidden, closed, or destroyed.

#### Scenario: One drag crosses several cells
- **WHEN** the author holds the left mouse button and drags a square-contour preset across multiple cells
- **THEN** each newly entered cell is painted once, connected-style constraints remain valid, and one Undo command restores the pre-gesture state

#### Scenario: Resource-acceptance Overlay closes during an active session
- **WHEN** the Overlay is hidden or closed, or loses its valid target
- **THEN** Scene input is released and subsequent clicks use normal Unity Scene behavior without changing terrain

### Requirement: Authoring validation and runtime parity
The change SHALL provide automated editor coverage and visual evidence for configured contour use, component constraints, and hand-painted square terrain, and the ordinary release WebGL build MUST preserve gameplay behavior and `Bootstrap → Lobby → Battle → Settlement` flow.

#### Scenario: Editor acceptance runs
- **WHEN** focused terrain painter validation executes
- **THEN** target selection, semantic metadata, configured square and organic targets, absence of the ordinary contour switch, contextual edges, Undo grouping, and session teardown pass

#### Scenario: Authored sample is built for ordinary WebGL
- **WHEN** a sample containing square and disconnected organic components is authored and the release build is validated
- **THEN** both contour styles render from the same canonical map while player interaction and flow match the accepted baseline
