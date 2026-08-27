## MODIFIED Requirements

### Requirement: Explicit online release authorization and provenance gates
The online pipeline MUST require `-Execute`, execution from the `oper` Git branch, a clean working tree, an existing SSH key path, and a Web build manifest bound to the current revision before delegating to remote deployment. The required release branch MUST NOT be operator-configurable.

#### Scenario: Release precondition is not satisfied
- **WHEN** the current branch is not `oper`, the working tree is dirty, the SSH key is unavailable, the manifest revision does not match, the Web target evidence is invalid, or the Web entry hash does not match
- **THEN** the pipeline stops before calling the remote deployment script

#### Scenario: Fresh Web build is requested
- **WHEN** an authorized operator executes the online pipeline from a clean `oper` checkout without `-SkipBuild`
- **THEN** the pipeline first runs the local Web build pipeline and validates its new manifest before remote deployment

#### Scenario: Existing Web build is reused
- **WHEN** an authorized operator executes from a clean `oper` checkout with `-SkipBuild`
- **THEN** the pipeline reuses the artifact only if its clean revision and current Web entry hash match the local manifest
