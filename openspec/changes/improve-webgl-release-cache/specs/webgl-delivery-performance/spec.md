## MODIFIED Requirements

### Requirement: Content-versioned Unity payload URLs
The generated WebGL entry page SHALL reference the loader, data, framework, and WebAssembly payloads with independent non-empty content-derived version tokens.

#### Scenario: Build output is generated
- **WHEN** the WebGL build completes successfully
- **THEN** every Unity payload URL advertised by `index.html` includes the first 12 lowercase hexadecimal characters of that payload's SHA-256 digest

#### Scenario: One payload changes
- **WHEN** one generated Unity payload changes and another payload remains byte-identical in a subsequent build
- **THEN** the changed payload receives a new cache key and the unchanged payload retains its previous cache key

### Requirement: Safe long-lived browser caching
The static service SHALL serve generated Unity build assets with `public, max-age=31536000, immutable` only when the advertised version matches the served bytes, SHALL emit a content-stable strong ETag, and SHALL keep the HTML entry document non-immutable and revalidatable.

#### Scenario: Correctly versioned payload is requested
- **WHEN** a client requests an advertised Unity build asset with its generated content version
- **THEN** the response permits public immutable caching for one year and its ETag identifies the payload SHA-256

#### Scenario: Missing or incorrect payload version is requested
- **WHEN** a client requests a Unity build asset without its generated version or with a version that does not match the served bytes
- **THEN** the response is not marked immutable

#### Scenario: Identical bytes are redeployed
- **WHEN** deployment changes a payload's filesystem modification time without changing its bytes
- **THEN** the payload URL and ETag remain unchanged

#### Scenario: Entry page is requested after deployment
- **WHEN** a client requests `index.html`
- **THEN** the response does not use immutable caching and can discover the newest payload versions

### Requirement: Production build size controls
The WebGL build SHALL enable high managed-code stripping and SHALL report total output size plus the size and content version of every generated Unity payload.

#### Scenario: Production WebGL build succeeds
- **WHEN** the editor build entry completes
- **THEN** its log identifies the compression mode, total output size, and per-payload sizes and versions

### Requirement: End-to-end delivery acceptance
The deployment workflow SHALL block publication completion unless local and public acceptance validate per-payload version correctness, cache headers, content-stable ETags, Brotli fallback delivery, a cold launch, a warm same-profile reload without payload body redownloads, Unity startup, portrait canvas dimensions, readable HUD rendering, and the required interaction states.

#### Scenario: Delivery metadata is incorrect locally
- **WHEN** a local asset has an incorrect version, validator, cache policy, encoding, or MIME type
- **THEN** deployment stops before uploading the build

#### Scenario: Warm reload redownloads a payload body
- **WHEN** the same browser profile reloads the same advertised WebGL version after a successful cold launch and any Unity payload transfers more than the permitted validation-header allowance
- **THEN** acceptance fails and records the offending asset role and transfer size

#### Scenario: Public service differs from local output
- **WHEN** the deployed service fails any delivery, cold/warm cache, or canvas requirement
- **THEN** deployment reports failure instead of declaring the release complete

#### Scenario: Acceptance succeeds
- **WHEN** all delivery, cache, and runtime checks pass
- **THEN** the acceptance manifest records every payload version, URL, response policy, ETag, content length, cold/warm transfer size, cold/warm startup duration, and required screenshots
