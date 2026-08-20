# webgl-visual-acceptance Specification

## Purpose
TBD - created by archiving change rebuild-portrait-webgl-layout. Update Purpose after archive.
## Requirements
### Requirement: Live WebGL canvas capture
The project SHALL provide a repeatable visual acceptance command that opens a local or deployed WebGL build at the target portrait viewport and captures the live Unity canvas at a requested point in time.

#### Scenario: Continuously rendering game
- **WHEN** the Unity render loop never becomes idle
- **THEN** the acceptance command captures through the browser debugging protocol without waiting for page idleness or process exit

### Requirement: Load failure detection
The visual acceptance command SHALL fail with a non-zero result when the page is unreachable, the Unity canvas is missing, the player reports a load error, or a screenshot cannot be produced within the configured timeout.

#### Scenario: Broken WebGL deployment
- **WHEN** a required build artifact is missing or the player cannot initialize
- **THEN** the command reports the failed check and does not mark the visual acceptance as successful

### Requirement: Required portrait evidence states
Portrait acceptance SHALL produce canonical live WebGL evidence for Bootstrap initialization/error, Lobby default/alternate/loading, Battle ready/active/between-wave/paused/tool-selected/legal-illegal drag/detail/dense/terminal/restart, and Settlement victory/defeat/return/retry states across the supported Shell size matrix and 402×874 full/inset cross-route matrix.

#### Scenario: Complete acceptance run
- **WHEN** the visual acceptance command completes successfully
- **THEN** it records every required state, viewport, safe-area inset, theme identity, ArtSet identity/GUID, payload hash, input result, and manifest outcome without substituting synthetic or stale screenshots

### Requirement: Visual acceptance checklist
The portrait build SHALL NOT be publishable unless captured evidence and deterministic Editor gates confirm packaged Chinese text, explicit line fit, baseline and optical alignment, safe-area containment, minimum touch targets, semantic color contrast, non-color state cues, full-width control use, readable status information, correct illustration aspect, nine-slice integrity, resource-set identity, and absence of clipping, overlap, stretch, mixed-set chrome, default skin, or input drift.

#### Scenario: Text is absent
- **WHEN** a required screenshot contains the game art but omits expected player-facing labels
- **THEN** the acceptance result is failed and publishing is blocked

#### Scenario: Text or component violates the quality standard
- **WHEN** a required state contains missing, clipped, overflowed, misaligned, low-contrast, overlapping, stretched, or semantically incorrect UI content
- **THEN** the acceptance result fails, the defect is recorded with severity and evidence, and publishing remains blocked until the canonical rerun passes

#### Scenario: Broad pixel sentinel matches a legitimate new asset
- **WHEN** an automated visual heuristic flags a screenshot but the suspected defect is not structurally present
- **THEN** the failed attempt is retained as infrastructure regression evidence and the heuristic is replaced by a structural rule without weakening the underlying product requirement

### Requirement: Scored UI quality evidence
Final visual acceptance SHALL publish a structured quality audit that reports every required category as pass/fail, records measured contrast and geometry results, links canonical screenshots, and distinguishes product defects from acceptance-infrastructure regressions.

#### Scenario: Final polish handoff
- **WHEN** the UI polish change is presented for manual review
- **THEN** the handoff identifies resolved defects, remaining non-blocking observations, exact build identity, canonical evidence, and any scope not proven by ordinary WebGL

### Requirement: Real host-window acceptance
Canonical ordinary-WebGL acceptance SHALL include the actual browser-host shell at representative wide desktop sizes in addition to exact portrait viewports and SHALL fail before route review if the complete canvas is clipped, scrolled, stretched, or mapped to the wrong pointer coordinates.

#### Scenario: Embedded desktop browser is shorter than the portrait canvas
- **WHEN** acceptance opens the release build at 1280 by 720
- **THEN** the screenshot contains the complete portrait frame, the DOM canvas rectangle lies entirely inside the viewport, the page requires no vertical scrollbar, and clicks at recorded canvas-relative targets activate the expected controls

#### Scenario: Desktop host and portrait canonical use one payload
- **WHEN** desktop-host and portrait-route evidence are signed
- **THEN** both record the same build identity, theme, ArtSet, and runtime behavior rather than validating a hand-edited build output

#### Scenario: A new WebGL build replaces files in place
- **WHEN** the stable localhost release service serves a newly built `index.html` or host asset at the same path
- **THEN** its ETag is recomputed from the current file bytes and an ordinary browser reload cannot receive a stale 304 response for the previous build

