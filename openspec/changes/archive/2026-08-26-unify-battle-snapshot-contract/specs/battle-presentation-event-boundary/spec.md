## MODIFIED Requirements

### Requirement: Authoritative state and persistence exclude presentation delivery state
`GameState`, `BattleSnapshot`, and the catalog-independent outcome checksum SHALL exclude pending presentation events, local delivery sequence, dropped-event count, combat-effect lifetime, and floating feedback lifetime. Successful current snapshot restore SHALL reset the presentation event stream to empty without emitting a restore-success event; failed restore SHALL preserve pending content and order, next sequence, and dropped count exactly.

#### Scenario: Snapshot is exported with pending presentation events
- **WHEN** a supported Standard battle exports a snapshot while cue and feedback events are pending
- **THEN** the serialized snapshot contains no presentation event payload, delivery cursor, next sequence, or dropped count

#### Scenario: Snapshot restore succeeds
- **WHEN** a current snapshot passes validation and commits to a same-source target with pending events
- **THEN** the stream becomes empty with its initial sequence/drop state and no restore-success event is appended

#### Scenario: Snapshot restore fails
- **WHEN** restore fails schema, presence, source, value, identity, or reference validation while events are pending
- **THEN** pending event contents/order, next sequence, and dropped count remain exactly as they were before the attempt

#### Scenario: Presentation queue changes without battle state changes
- **WHEN** pending events are drained, discarded, or dropped between two checksum calculations
- **THEN** the outcome checksum remains unchanged
