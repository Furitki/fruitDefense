# webgl-delivery-performance Specification

## Purpose
TBD - created by archiving change optimize-webgl-loading. Update Purpose after archive.
## Requirements
### Requirement: Content-versioned Unity payload URLs
The generated WebGL entry page SHALL reference the loader, data, framework, and WebAssembly payloads with one shared non-empty content-derived version token.

#### Scenario: Build output is generated
- **WHEN** the WebGL build completes successfully
- **THEN** every Unity payload URL advertised by `index.html` includes the same version query token

#### Scenario: Build content changes
- **WHEN** one or more Unity payload files change in a subsequent build
- **THEN** the generated version token changes so the browser requests a new cache key

### Requirement: Safe long-lived browser caching
The static service SHALL serve generated versioned Unity build assets with `public, max-age=31536000, immutable` and SHALL keep the HTML entry document non-immutable and revalidatable.

#### Scenario: Versioned payload is requested
- **WHEN** a client requests an advertised Unity build asset with its generated version token
- **THEN** the response permits public immutable caching for one year

#### Scenario: Entry page is requested after deployment
- **WHEN** a client requests `index.html`
- **THEN** the response does not use immutable caching and can discover the newest payload version

### Requirement: Brotli-compressed Unity delivery on bare HTTP
The production WebGL build SHALL emit Brotli-compressed data, framework, and WebAssembly payloads with Unity's JavaScript decompression fallback, and the static service SHALL return the generated `.unityweb` containers as opaque binary responses without a native content encoding.

#### Scenario: WebAssembly payload is inspected
- **WHEN** acceptance sends a request for the advertised WebAssembly asset
- **THEN** the URL ends in `.unityweb`, the response has no `Content-Encoding`, and its `Content-Type` is `application/octet-stream`

#### Scenario: JavaScript framework payload is inspected
- **WHEN** acceptance sends a request for the advertised framework asset
- **THEN** the URL ends in `.unityweb`, the response has no `Content-Encoding`, and the Unity loader successfully decompresses and starts the framework

### Requirement: Production build size controls
The WebGL build SHALL enable high managed-code stripping and SHALL report total output size plus the sizes of the generated Unity payload files.

#### Scenario: Production WebGL build succeeds
- **WHEN** the editor build entry completes
- **THEN** its log identifies the compression mode, build version, total output size, and individual Unity payload sizes

### Requirement: End-to-end delivery acceptance
The deployment workflow SHALL block publication completion unless local and public acceptance validate version consistency, cache headers, Brotli fallback delivery, Unity startup, portrait canvas dimensions, readable HUD rendering, and the required interaction states.

#### Scenario: Delivery metadata is incorrect locally
- **WHEN** a local asset is unversioned, non-immutable, incorrectly encoded, or has an incorrect MIME type
- **THEN** deployment stops before uploading the build

#### Scenario: Public service differs from local output
- **WHEN** the deployed service fails any delivery or canvas requirement
- **THEN** deployment reports failure instead of declaring the release complete

#### Scenario: Acceptance succeeds
- **WHEN** all delivery and runtime checks pass
- **THEN** the acceptance manifest records the version, payload URLs, response policies, content lengths, and four required screenshots

