## MODIFIED Requirements

### Requirement: Required portrait evidence states
Portrait acceptance SHALL produce canonical live WebGL evidence for Bootstrap initialization/error, Lobby default/alternate/loading, Battle ready/active/between-wave/paused/tool-selected/legal-illegal drag/detail/dense/terminal/restart, and Settlement victory/defeat/return/retry states across the supported Shell size matrix and 402×874 full/inset cross-route matrix. Paused Battle evidence SHALL expose the complete title ribbon, centered indicator-copy row, and paired actions at rest and during contained press feedback.

#### Scenario: Complete acceptance run
- **WHEN** the visual acceptance command completes successfully
- **THEN** it records every required state, viewport, safe-area inset, theme identity, ArtSet identity/GUID, payload hash, input result, optical measurement result, and manifest outcome without substituting synthetic or stale screenshots

### Requirement: Visual acceptance checklist
The portrait build SHALL NOT be publishable unless captured evidence and deterministic Editor gates confirm packaged Chinese text, explicit line fit, corrected glyph placement, actual-alpha optical alignment, equal paired-action visible bounds, safe-area containment, minimum touch targets, semantic color contrast, non-color state cues, full-width control use, readable status information, correct illustration aspect, nine-slice integrity, resource-set identity, and absence of clipping, overlap, stretch, mixed-set chrome, default skin, or input drift.

#### Scenario: Text is absent
- **WHEN** a required screenshot contains the game art but omits expected player-facing labels
- **THEN** the acceptance result is failed and publishing is blocked

#### Scenario: Declared geometry passes but final raster is misaligned
- **WHEN** component rectangles are contained but visible surface alpha, icon alpha, or corrected glyph mass exceeds the documented centering, size, gap, or baseline tolerance
- **THEN** the acceptance result fails and cannot be waived by rectangle-only evidence

#### Scenario: Text or component violates the quality standard
- **WHEN** a required state contains missing, clipped, overflowed, misaligned, unequal, low-contrast, overlapping, stretched, or semantically incorrect UI content
- **THEN** the acceptance result fails, the defect is recorded with severity and evidence, and publishing remains blocked until the canonical rerun passes

#### Scenario: Broad pixel sentinel matches a legitimate new asset
- **WHEN** an automated visual heuristic flags a screenshot but the suspected defect is not structurally present
- **THEN** the failed attempt is retained as infrastructure regression evidence and the heuristic is replaced by a structural rule without weakening the underlying product requirement
