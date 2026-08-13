## ADDED Requirements

### Requirement: Deterministic Web payload gate
The local Web build pipeline MUST build the production WebGL target twice and compare the full SHA-256 digest and byte length of every advertised Unity payload before writing publishable evidence.

#### Scenario: Verification builds match
- **WHEN** both Web builds produce identical payload maps
- **THEN** the second output remains in `Builds/WebGL` and the manifest records a passed two-build determinism comparison

#### Scenario: Verification builds differ
- **WHEN** any payload digest, length, role, or advertised version differs between the two builds
- **THEN** the pipeline fails with the differing roles and does not write a successful local manifest

## MODIFIED Requirements

### Requirement: Local build evidence manifest
The local pipeline SHALL write an ignored schema-3 JSON manifest containing the Unity version, Git revision, pre-build dirty state, requested target, completed target evidence, output paths, artifact sizes, target hashes, per-payload full digests and versions, deterministic Web comparison, and relevant log paths.

#### Scenario: Local build succeeds
- **WHEN** all requested targets and any target-specific deterministic checks complete successfully
- **THEN** the pipeline writes the manifest and emits a stable local-pipeline success marker containing its path

#### Scenario: Dirty development sources are built
- **WHEN** a developer runs the local pipeline with uncommitted changes
- **THEN** the build may proceed but the manifest records the dirty state for downstream release rejection
