## MODIFIED Requirements

### Requirement: Snapshot preserves complete level identity
The current snapshot schema SHALL export `levelId`, `mapId`, `waveSetId`, `ruleSetId`, and `themeId`, and restore MUST validate all five values and the exact Ability catalog/content identity against one catalog resolution before mutating live simulation state. The flow SHALL NOT infer a current level or Ability state from a legacy single-map snapshot.

#### Scenario: Restore a matching level snapshot
- **WHEN** a snapshot's composite identity, catalog version, content version, and Ability schema match the current resolved level
- **THEN** restore succeeds and the resumed session continues with the same map, waves, rules, theme, seed, Ability state, and gameplay state

#### Scenario: Reject a component mismatch atomically
- **WHEN** any recorded level component ID or Ability catalog identity differs from the current catalog resolution
- **THEN** restore fails with the mismatched identity field identified and leaves the live simulation and presentation-event state unchanged

#### Scenario: Reject a legacy single-map snapshot
- **WHEN** a snapshot lacks the current composite level and unified Ability identity
- **THEN** restore rejects the unsupported schema without mapping it to `orchard-01`, current Lobby selection, or another level
