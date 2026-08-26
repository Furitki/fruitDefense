## MODIFIED Requirements

### Requirement: Live WebGL canvas capture
The project SHALL provide a repeatable visual acceptance command that verifies the `acceptance` build profile, opens the dedicated acceptance WebGL build at the target portrait viewport, and captures the live Unity canvas at a requested point in time.

#### Scenario: Continuously rendering game
- **WHEN** the Unity render loop never becomes idle
- **THEN** the acceptance command captures through the browser debugging protocol without waiting for page idleness or process exit

#### Scenario: Wrong build profile is supplied
- **WHEN** the visual acceptance command opens a build whose profile is absent, unknown, or `release`
- **THEN** it fails before requesting an injected route or named state
- **AND** it produces no successful acceptance manifest

### Requirement: Required portrait evidence states
Portrait acceptance SHALL produce canonical live WebGL evidence from the dedicated acceptance profile for Bootstrap initialization/error, Lobby default/alternate/loading, Battle ready/active/between-wave/paused/tool-selected/legal-illegal drag/detail/dense/terminal/restart, and Settlement victory/defeat/return/retry states across the supported Shell size matrix and 402×874 full/inset cross-route matrix.

#### Scenario: Complete acceptance run
- **WHEN** the visual acceptance command completes successfully
- **THEN** it records every required state, viewport, safe-area inset, theme identity, ArtSet identity/GUID, acceptance payload hash, input result, and manifest outcome without substituting synthetic screenshots, stale screenshots, or an ordinary release payload

### Requirement: Real host-window acceptance
Canonical WebGL acceptance SHALL include the actual browser-host shell at representative wide desktop sizes. Ordinary-release host checks SHALL use the `release` profile without acceptance queries or bridge access, while injected route and portrait-state evidence SHALL use the dedicated `acceptance` profile. Acceptance SHALL fail before route review if either profile's complete canvas is clipped, scrolled, stretched, or mapped to the wrong pointer coordinates.

#### Scenario: Embedded desktop browser is shorter than the portrait canvas
- **WHEN** acceptance opens the ordinary release build at 1280 by 720 without an acceptance query
- **THEN** the screenshot contains the complete portrait frame, the DOM canvas rectangle lies entirely inside the viewport, the page requires no vertical scrollbar, and recorded clicks on player-visible controls map to the expected canvas-relative targets
- **AND** no acceptance bridge or Unity instance is exposed

#### Scenario: Desktop host and portrait canonical use one payload
- **WHEN** desktop-host and portrait-route injected evidence are signed for one acceptance run
- **THEN** both record the same acceptance payload identity, theme, ArtSet, and runtime behavior rather than validating a hand-edited build output

#### Scenario: Release and acceptance profiles are paired
- **WHEN** a release candidate's host checks and injected visual evidence are signed
- **THEN** the evidence records distinct `release` and `acceptance` profile identities built from the same source revision, scenes, content, theme, ArtSet, and template source

#### Scenario: A new WebGL build replaces files in place
- **WHEN** the stable localhost service serves a newly built `index.html` or host asset at the same path for either profile
- **THEN** its ETag is recomputed from the current file bytes and an ordinary browser reload cannot receive a stale 304 response for the previous build

### Requirement: Battle structural-weight and closeout evidence
Canonical dedicated-acceptance WebGL capture SHALL measure Battle's live structural surfaces, outline bands, occupied-content bounds, lower closeout, text containment, and context-tray state from the composited canvas rather than inferring the visual hierarchy from logical rectangles or a concept image.

#### Scenario: Ready and active Battle are captured
- **WHEN** 402×874 full and inset ready/active states are recorded from the dedicated acceptance profile
- **THEN** the manifest identifies one gameplay-stage heavy frame, light Header/context/nursery sections, no enclosing legacy lower-page frame, all required text ink within owners, and the refresh action's lower edge plus remaining safe-content margin

#### Scenario: Selected plant detail is captured
- **WHEN** the canonical detail state is recorded from the dedicated acceptance profile
- **THEN** the context tray contains the selected-plant title, finite attributes, and close action instead of the tool row, while nursery, refresh, and wave action remain visible and recorded hit targets activate the expected controls

#### Scenario: Structural hierarchy regresses
- **WHEN** a second unapproved heavy large frame, hidden empty detail band, overflowed copy, mixed ArtSet binding, stale stage asset, or release-profile payload is detected
- **THEN** acceptance fails and publishing remains blocked until the current acceptance payload is rebuilt and the canonical matrix passes
