## MODIFIED Requirements

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
