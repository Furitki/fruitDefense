## ADDED Requirements

### Requirement: High-capacity near-target combat floating-text pool
The combat presentation layer SHALL admit up to 9999 total floating-text records and up to 9999 ordinary floating-text records without changing simulation state, persistence, event ordering, map projection, or BattleStage geometry. It SHALL preallocate the fixed presentation storage required by that capacity and SHALL continue to use the bundled glyph atlas and presentation-only rendering path.

#### Scenario: Large mixed feedback burst is admitted
- **WHEN** a presentation-only battle event stream emits up to 9999 eligible ordinary or non-ordinary floating-text records
- **THEN** the presentation buffer admits the records without an 8- or 12-record capacity eviction and leaves authoritative battle state unchanged

### Requirement: Deterministic no-avoidance target placement
The combat floating-text renderer SHALL place each label from only its authored visual-lane offset and semantic offset. It SHALL NOT evaluate collision candidates, score overlap against other labels, or change a label's position because another label is active. Labels SHALL retain upward motion and BattleStage containment.

#### Scenario: Dense labels remain on their authored lanes
- **WHEN** several active labels are rendered during a Battle frame
- **THEN** each selected bound is derived from that record's visual-lane and semantic offsets only, stays within BattleStage, and does not change as a result of another label's bounds

### Requirement: Acceptance reflects the capacity contract
The focused editor smoke and WebGL combat-feedback acceptance harness SHALL report and validate the 9999 total/ordinary pool contract while retaining the existing 12-record dense visual fixture and its placement, projection, and performance checks.

#### Scenario: Standard WebGL dense fixture is captured
- **WHEN** the 12-record combat-feedback dense acceptance fixture runs at a required portrait viewport
- **THEN** telemetry reports a pool capacity of 9999, verifies all 12 active records, and records authored-lane placement, deterministic re-sync, and performance evidence without altering the authored Battle UI layout; it does not require zero overlap between dense labels

### Requirement: Fast-rise, gradual-fade floating-text motion
The combat floating-text renderer SHALL render a newly emitted label at full opacity, advance its upward offset from whole-lifetime progress with a fast-start curve, and fade it continuously across that lifetime. It SHALL NOT use a separate entry fade, a hold-until-late fade, or detachment progress as the upward-motion clock.

#### Scenario: A label starts moving before it fades away
- **WHEN** a label advances from the beginning through the middle of its lifetime
- **THEN** it has already completed more than 43 percent of its upward travel at 25 percent lifetime progress, retains more than 49 percent opacity at 50 percent lifetime progress, and reaches zero opacity only at the end of its lifetime
