## ADDED Requirements

### Requirement: Non-publishing default plan
The online WebGL publication pipeline SHALL default to a plan-only mode that reports resolved inputs and release gates without reading private-key contents, opening a network connection, uploading artifacts, or mutating the server.

#### Scenario: Operator invokes the online entry without execute authorization
- **WHEN** the online publication script is run without `-Execute`
- **THEN** it prints the publication plan, emits a stable plan success marker, and performs no online publication

### Requirement: Explicit online release authorization and provenance gates
The online pipeline MUST require `-Execute`, the expected Git branch, a clean working tree, an existing SSH key path, and a Web build manifest bound to the current revision before delegating to remote deployment.

#### Scenario: Release precondition is not satisfied
- **WHEN** the branch, working tree, SSH key, manifest revision, Web target evidence, or Web entry hash does not satisfy the release gate
- **THEN** the pipeline stops before calling the remote deployment script

#### Scenario: Fresh Web build is requested
- **WHEN** an authorized operator executes the online pipeline without `-SkipBuild`
- **THEN** the pipeline first runs the local Web build pipeline and validates its new manifest before remote deployment

#### Scenario: Existing Web build is reused
- **WHEN** an authorized operator executes with `-SkipBuild`
- **THEN** the pipeline reuses the artifact only if its clean revision and current Web entry hash match the local manifest

### Requirement: Existing WebGL deployment contract remains authoritative
After release gates pass, the online pipeline SHALL delegate to the existing deployment workflow for local acceptance, archive/upload, remote service replacement, entry health, WebGL cache/header validation, service status, and deployed acceptance.

#### Scenario: Remote deployment or acceptance fails
- **WHEN** the existing deployment workflow reports an upload, health, header, service, or public acceptance failure
- **THEN** the online pipeline reports failure and MUST NOT declare publication complete

#### Scenario: Online WebGL publication succeeds
- **WHEN** all local gates and delegated remote checks succeed
- **THEN** the pipeline writes an ignored publication manifest and emits a stable online-publication success marker

### Requirement: Mini-game release boundary is preserved
The online publication pipeline SHALL publish only the ordinary WebGL artifact and MUST NOT claim, invoke, or silently substitute Douyin or WeChat conversion and release.

#### Scenario: Ordinary WebGL release plan is inspected
- **WHEN** an operator reviews or executes the online pipeline
- **THEN** the pipeline identifies the target as ordinary WebGL and leaves mini-game adapters and readiness gates unchanged
