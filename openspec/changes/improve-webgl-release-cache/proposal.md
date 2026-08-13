## Why

The ordinary WebGL release still downloads the full 17.7 MB Unity payload after most publications because all generated files share one cache version, even when only one payload changed. The current acceptance proves response headers but does not prove that a warm launch reuses cached bytes, so cache regressions can pass release gates unnoticed.

## What Changes

- Give each generated Unity payload its own content-derived version token so unchanged loader, data, framework, or WebAssembly files retain stable cache keys across publications.
- Generate content-derived ETags so redeploying identical bytes does not invalidate Unity's revalidating data cache because of a new filesystem modification time.
- Extend WebGL delivery acceptance with a cold/warm browser run that proves the advertised payload URLs are reused, records transfer and startup measurements, and blocks publication when a warm run redownloads immutable payloads.
- Keep the existing bare-HTTP Brotli fallback for the current origin; HTTPS/CDN migration is outside this change.
- Preserve gameplay, persistence, the portrait player flow, and all mini-game platform boundaries.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `webgl-delivery-performance`: Replace the shared build version with per-payload content versions, require content-stable validators, and require measured cold/warm cache reuse in local and deployed acceptance.

## Impact

- Affects `Assets/Editor/Tools/WebBuild.cs`, generated `Builds/WebGL/index.html`, `deploy/server.mjs`, `deploy.ps1`, `scripts/accept-webgl-portrait.ps1`, and local/online pipeline manifests.
- Changes the acceptance manifest delivery schema from one shared version to per-asset versions and cache-run measurements.
- Adds no runtime dependency and does not alter the player-visible `Bootstrap → Lobby → Battle → Settlement` flow.
