## ADDED Requirements

### Requirement: Selectable local build targets
The project SHALL provide one local build pipeline that accepts `Web`, `PC`, or `All` and produces only the requested local artifacts after a successful shared validation gate.

#### Scenario: Web target is requested
- **WHEN** an operator invokes the local pipeline with the `Web` target
- **THEN** the pipeline runs the P0 gate and produces `Builds/WebGL` without rebuilding the Windows player

#### Scenario: PC target is requested
- **WHEN** an operator invokes the local pipeline with the `PC` target
- **THEN** the pipeline runs the P0 gate and produces `Builds/Windows/FruitDefense.exe` without rebuilding WebGL

#### Scenario: All targets are requested
- **WHEN** an operator invokes the local pipeline with the `All` target
- **THEN** the pipeline runs the P0 gate once and then produces both Web and PC artifacts sequentially

### Requirement: Deterministic local preflight and failure behavior
The local pipeline MUST require the project Unity version, requested platform modules, exclusive project access, successful Unity exit codes, and expected success markers before reporting a target successful.

#### Scenario: Required Unity environment is unavailable
- **WHEN** the required editor executable, project version, or requested platform module is missing
- **THEN** the pipeline stops before validation or build and reports the missing prerequisite

#### Scenario: Validation or build fails
- **WHEN** Unity exits unsuccessfully or the expected success marker is absent from its log
- **THEN** the pipeline fails and identifies the corresponding log without reporting the target successful

### Requirement: Local build evidence manifest
The local pipeline SHALL write an ignored JSON manifest containing the Unity version, Git revision, pre-build dirty state, requested target, completed target evidence, output paths, artifact sizes, target hashes, and relevant log paths.

#### Scenario: Local build succeeds
- **WHEN** all requested targets complete successfully
- **THEN** the pipeline writes the manifest and emits a stable local-pipeline success marker containing its path

#### Scenario: Dirty development sources are built
- **WHEN** a developer runs the local pipeline with uncommitted changes
- **THEN** the build may proceed but the manifest records the dirty state for downstream release rejection
