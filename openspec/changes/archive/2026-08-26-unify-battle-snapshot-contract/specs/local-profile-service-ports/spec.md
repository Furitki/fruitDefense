## MODIFIED Requirements

### Requirement: No automatic battle resume in P0
P0 profile services MUST NOT automatically persist or restore a running Battle, regardless of the current battle snapshot schema or session API.

#### Scenario: Profile save during battle
- **WHEN** settings or shell profile data is saved while Battle exists
- **THEN** no `BattleSnapshot` or current-session snapshot payload is written by the profile store
