## MODIFIED Requirements

### Requirement: Content-versioned Unity payload URLs
The generated WebGL entry page SHALL reference the loader, data, framework, and WebAssembly payloads with independent non-empty content-derived version tokens, and two builds of identical clean source with the same Unity version and production settings SHALL produce byte-identical versions and payloads.

#### Scenario: Build output is generated
- **WHEN** the WebGL build completes successfully
- **THEN** every Unity payload URL advertised by `index.html` includes the first 12 lowercase hexadecimal characters of that payload's SHA-256 digest

#### Scenario: One payload changes
- **WHEN** one generated Unity payload changes and another payload remains byte-identical in a subsequent build
- **THEN** the changed payload receives a new cache key and the unchanged payload retains its previous cache key

#### Scenario: Identical source is rebuilt
- **WHEN** the same clean revision is built twice with the same Unity version and production settings
- **THEN** all four payload SHA-256 digests and byte lengths are identical

### Requirement: Production build size controls
The WebGL build SHALL enable high managed-code stripping, SHALL exclude editor-only terrain bake inputs from runtime `Resources`, and SHALL report total output size plus the size and content version of every generated Unity payload.

#### Scenario: Production WebGL build succeeds
- **WHEN** the editor build entry completes
- **THEN** its log identifies the compression mode, total output size, and per-payload sizes and versions

#### Scenario: Runtime resources are inspected
- **WHEN** release inputs are validated
- **THEN** generated runtime atlases remain loadable and editor-only HD terrain bake source textures are not under a runtime `Resources` directory

### Requirement: End-to-end delivery acceptance
The deployment workflow SHALL block publication completion unless local and public acceptance validate per-payload version correctness, cache headers, content-stable ETags, Brotli fallback delivery, a cold launch, a same-release warm reload without payload body redownloads, cross-release reuse of every unchanged payload in one browser profile, Unity startup, portrait canvas dimensions, readable HUD rendering, and the required interaction states.

#### Scenario: Delivery metadata is incorrect locally
- **WHEN** a local asset has an incorrect version, validator, cache policy, encoding, or MIME type
- **THEN** deployment stops before uploading the build

#### Scenario: Same-release warm reload redownloads a payload body
- **WHEN** the same browser profile reloads the same advertised WebGL version and a Unity payload transfers more than the permitted validation-header allowance
- **THEN** acceptance fails and identifies the payload and measured transfer size

#### Scenario: Cross-release unchanged payload redownloads
- **WHEN** release N is cached and release N+1 advertises the same content version for a payload in the same browser profile but transfers more than the permitted validation-header allowance
- **THEN** deployed acceptance fails and identifies the payload and measured transfer size

#### Scenario: Public service differs from local output
- **WHEN** the deployed service fails any delivery, cache, or canvas requirement
- **THEN** deployment reports failure instead of declaring the release complete

#### Scenario: Acceptance succeeds
- **WHEN** all delivery, cache, and runtime checks pass
- **THEN** the acceptance evidence records every payload version, URL, response policy, ETag, content length, same-release cold/warm transfer, cross-release reused and changed roles, expected download bytes, measured candidate transfer, startup duration, and required screenshots
