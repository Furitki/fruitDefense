# level-selection-flow Specification

## Purpose
TBD - created by archiving change introduce-level-map-catalog. Update Purpose after archive.
## Requirements
### Requirement: Three-level Lobby selection
The Lobby SHALL present `orchard-01`, `orchard-02`, and `orchard-03` as available selectable levels, SHALL make the current selection visually explicit, and SHALL launch only the selected `LevelId`.

#### Scenario: Select and start the coverage level
- **WHEN** the player selects the `orchard-02` card and activates Start
- **THEN** the shell creates a battle launch request whose `LevelId` is `orchard-02`

#### Scenario: Change selection without starting
- **WHEN** the player selects a different level card
- **THEN** the Lobby updates the visible selected state and does not begin a battle transition until Start is activated

#### Scenario: Prevent an unknown launch
- **WHEN** a caller submits a `LevelId` that is not in the compiled playable catalog
- **THEN** the flow rejects the launch with a structured error and remains or recovers to a usable Lobby state

### Requirement: Persist the last valid selection
The local profile SHALL retain the last valid selected `LevelId` and restore that selection when returning to or reopening the Lobby without treating the selection as an unlock or progression decision.

#### Scenario: Return from settlement
- **WHEN** the player returns to the Lobby after completing `orchard-03`
- **THEN** the Lobby restores `orchard-03` as the selected card

#### Scenario: Recover an unavailable stored identity
- **WHEN** stored profile data references a level absent from the compiled catalog
- **THEN** profile recovery records the invalid identity, selects the catalog-declared safe UI default, and never launches the missing level or silently resolves it to another map

### Requirement: Composite session identity is fixed at launch
The flow coordinator SHALL resolve the launch `LevelId` once into a composite level identity before simulation construction, and the active battle session MUST keep that `levelId`, `mapId`, `waveSetId`, `ruleSetId`, and `themeId` unchanged until disposal.

#### Scenario: Initialize a selected battle
- **WHEN** a valid launch request for `orchard-02` initializes a battle
- **THEN** simulation and presentation receive the same resolved `orchard-02` bundle before the first simulation step or draw

#### Scenario: Catalog changes during a live session
- **WHEN** a catalog or profile selection changes after a battle session has initialized
- **THEN** the active session continues using its launch-time resolved identity and definitions

### Requirement: Snapshot preserves complete level identity
The single current snapshot schema SHALL export `levelId`, `mapId`, `waveSetId`, `ruleSetId`, and `themeId` together with exact level-catalog ID, content-catalog ID/version, gameplay-map fingerprint, and a canonical resolved-source definition fingerprint covering compiled battle content, ordered waves, rules, theme, and gameplay map. A snapshot-capable Standard target SHALL retain that immutable resolved source from construction, and restore MUST prove that the serialized source, supplied catalog resolution, and existing target source all match in IDs, versions, and definition fingerprint before candidate construction or live mutation. The flow SHALL NOT infer, translate, or map a missing or legacy identity from `orchard-01`, current Lobby selection, a default map, or another level.

#### Scenario: Restore a matching current level snapshot
- **WHEN** a current snapshot, supplied catalog resolution, and existing target share the same catalog/content, level, map, waves, rules, theme, gameplay-map identity, and resolved-source definition fingerprint
- **THEN** restore may resume that existing target with the same seed, Ability state, and gameplay state

#### Scenario: Reject a supplied-catalog or target mismatch atomically
- **WHEN** any serialized, resolved, or target source component differs
- **THEN** restore reports the mismatched source path before candidate construction and leaves simulation and presentation-event state unchanged

#### Scenario: Reject changed definitions under reused identities
- **WHEN** a supplied catalog keeps all IDs and version strings but changes a rule value, ordered-wave payload, theme value, or compiled battle-content definition
- **THEN** restore rejects the definition-fingerprint mismatch before candidate construction and leaves the existing target unchanged

#### Scenario: Reject a non-current or incomplete identity
- **WHEN** a V1/V2/V3 or incomplete snapshot lacks the current schema or required resolved source fields
- **THEN** restore rejects it before mutation without mapping it to `orchard-01`, current Lobby selection, a default map, or another level

### Requirement: Settlement and retry preserve the completed level
Settlement SHALL identify the completed `LevelId`; retry SHALL launch that same level through normal catalog resolution with a fresh session identity, and returning SHALL restore it as the Lobby selection.

#### Scenario: Retry the boss-pressure level
- **WHEN** settlement for `orchard-03` activates Retry
- **THEN** the next launch request uses `orchard-03`, a new non-empty session ID, and a new nonzero seed, and resolves its current catalog bundle before battle initialization

#### Scenario: Reject a mismatched result
- **WHEN** a battle result's `LevelId` differs from its originating launch request
- **THEN** settlement submission fails with the level-mismatch error and does not offer retry for the mismatched identity

### Requirement: Portrait-safe selection geometry
The Lobby layout SHALL derive the drawn level cards, selected state, and hit-test regions from the same safe-area-aware geometry and SHALL keep all three choices and the Start action usable at supported portrait viewports.

#### Scenario: Validate supported portrait sizes
- **WHEN** layout validation runs at 360×800, 375×812, 402×874, and 430×932 with both full and inset safe areas
- **THEN** all required level content remains inside the safe area, card hit targets select only their drawn card, and Start does not overlap a card

#### Scenario: Use a real WebGL canvas
- **WHEN** the built WebGL Lobby is exercised at the target portrait sizes
- **THEN** the selected level is readable, all three cards can be selected, and Start launches the visibly selected map without clipping or input offset

### Requirement: Browser instability does not weaken acceptance
The required editor smoke and WebGL build checks SHALL remain independent of embedded-browser availability, and real-canvas acceptance SHALL use an external browser or another stable capture path if the embedded browser crashes.

#### Scenario: Embedded browser crashes during acceptance
- **WHEN** the embedded browser becomes unavailable before real-canvas evidence is captured
- **THEN** validation records the browser failure, completes editor and build gates, and obtains the same real-canvas assertions through a stable alternative before claiming visual acceptance

