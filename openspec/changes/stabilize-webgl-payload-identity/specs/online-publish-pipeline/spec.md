## ADDED Requirements

### Requirement: Cross-release payload delta evidence
The online publication pipeline SHALL compare the currently served ordinary WebGL payload versions with the candidate, SHALL seed the current release into the same browser profile used for candidate acceptance, and SHALL record reused roles, changed roles, expected download bytes, and observed first-load transfer bytes.

#### Scenario: Previous public release is reachable
- **WHEN** publication begins and the current public WebGL release passes cache seeding
- **THEN** the deployed candidate is accepted with that seeded profile and every unchanged payload must satisfy the transfer allowance

#### Scenario: Candidate changes one payload
- **WHEN** the candidate version differs from the seeded release for exactly one payload role
- **THEN** publication evidence lists that role as changed, lists the other roles as reused, and computes expected download bytes from the changed candidate payload only

#### Scenario: No previous public release is reachable
- **WHEN** publication is the first release and no valid baseline can be seeded
- **THEN** the manifest records an explicit first-release state while all non-transition delivery checks remain mandatory

## MODIFIED Requirements

### Requirement: Existing WebGL deployment contract remains authoritative
After release gates pass, the online pipeline SHALL delegate to the existing deployment workflow for local acceptance, prior-release cache seeding, archive/upload, remote service replacement, entry health, exact WebGL cache/header validation, service status, same-profile cross-release acceptance, and deployed visual acceptance.

#### Scenario: Remote deployment or acceptance fails
- **WHEN** the existing deployment workflow reports an upload, health, header, service, cross-release cache, or public acceptance failure
- **THEN** the online pipeline reports failure and MUST NOT declare publication complete

#### Scenario: Online WebGL publication succeeds
- **WHEN** all local gates and delegated remote checks succeed
- **THEN** the pipeline writes an ignored schema-3 publication manifest containing the candidate provenance and release-transition evidence and emits a stable online-publication success marker
