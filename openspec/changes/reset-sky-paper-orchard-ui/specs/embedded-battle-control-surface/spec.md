## ADDED Requirements

### Requirement: Independent phase and Wave flow row
Battle SHALL allocate one persistent row immediately below the gameplay stage for phase feedback and the phase-specific Wave command, and the row's draw, hit, text, and action rectangles SHALL come from `BattleUiLayout` rather than `BattlefieldProjection`.

#### Scenario: Ready or countdown command is available
- **WHEN** the phase permits `开始波次` or `立即开始下一波`
- **THEN** the row shows a sunlight-emphasis phase status and one leaf-green primary Wave action whose visible and hit rectangles are identical

#### Scenario: No Wave command is available
- **WHEN** the phase is active or terminal
- **THEN** the same row retains phase/progress feedback without drawing or accepting an unavailable Wave command and without moving the ContextTray, NurseryTray, or RefreshAction

#### Scenario: Legacy stage control path is inspected
- **WHEN** runtime sources, layout tests, and acceptance metadata are audited
- **THEN** no in-stage Wave rectangle, duplicate command target, compatibility draw path, or fallback interaction remains

## MODIFIED Requirements

### Requirement: Reference-faithful paper page and inset battlefield
The portrait battle screen SHALL place one warm-paper page shell below the
floating Header, SHALL inset one soil gameplay stage into the upper part of that
page, SHALL target 38–43 percent of resolved safe-content height for the stage at
402×874, and SHALL place the phase/Wave, Context, Nursery, and Refresh tracks in
the same page shell below it. The page shell is a light surface and SHALL NOT
duplicate the gameplay-stage heavy frame.

#### Scenario: Reference portrait layout
- **WHEN** the battle screen renders at 402 by 874 logical points
- **THEN** the floating Header and page shell use matched side gutters, the stage
  is inset within the page, remains the only persistent heavy gameplay frame,
  stays within the accepted height band, and contains all projected map content
  without an in-stage Wave action

#### Scenario: Safe-area portrait layout
- **WHEN** the same composition is scaled into a supported portrait safe area
- **THEN** Header, gameplay stage, phase/Wave row, ContextTray, NurseryTray, RefreshAction, and their complete hit targets remain visible and operable inside the safe area

### Requirement: Contextual plant details
An inspected plant SHALL replace the tool anatomy inside ContextTray with a compact information anatomy, while no separate detail rectangle or hidden detail band SHALL be reserved when no plant is inspected.

#### Scenario: Plant is inspected
- **WHEN** the player clicks an on-board or nursery plant without starting a drag
- **THEN** ContextTray shows its identity, finite essential combat values, and a touch-sized close action while the independent phase/Wave row, NurseryTray, and RefreshAction remain visible

#### Scenario: Inspection is closed
- **WHEN** the player closes the compact card
- **THEN** ContextTray returns to the tool anatomy without changing any plant position, phase, or battle state

### Requirement: Battle control touch and acceptance coverage
Primary battle controls SHALL remain at least 44 logical points on their shortest interactive dimension, and the project SHALL validate the sky-paper composition, role fonts, authoritative geometry, and phase-row lifecycle through Unity smoke and real WebGL portrait evidence.

#### Scenario: Geometry smoke
- **WHEN** editor validation checks the reference composition
- **THEN** Header, stage, phase/Wave row, ContextTray, NurseryTray, and RefreshAction are ordered, non-overlapping, touch-sized, aligned to the four-point rhythm, and contain no legacy enclosing BattleSurface or in-stage Wave target

#### Scenario: WebGL portrait evidence
- **WHEN** the rebuilt WebGL player is captured at the 402-by-874 full and representative inset viewports
- **THEN** ready, active, paused, and selected-detail evidence shows one heavy gameplay stage, the correct phase-row state, mutually exclusive ContextTray modes, persistent NurseryTray and RefreshAction, finite packaged-font containment, valid pointer targets, and an 8-to-40-point lower closeout
