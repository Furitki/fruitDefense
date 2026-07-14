## Why

The published WebGL build downloads about 9.65 MB over a measured 1.5-3.1 Mbps public path, while every asset is currently served with `Cache-Control: no-cache`. First loads are slow and repeat visits unnecessarily download the same Unity payload again, so deployment-side optimizations are needed before server bandwidth can be upgraded.

## What Changes

- Give each WebGL build a content version and use that version in the Unity loader asset URLs.
- Serve versioned Unity build assets with long-lived immutable caching while keeping HTML and unversioned entry files revalidatable.
- Build the Unity payload with Brotli compression plus Unity's JavaScript decompression fallback so the current bare-HTTP origin can use the smaller payload safely.
- Keep managed-code stripping enabled at an appropriate production level and report output sizes during the build.
- Extend local and deployed acceptance to verify cache policy, versioned URLs, compression headers, successful Unity startup, and representative download sizes.
- Preserve the current portrait gameplay flow, save behavior, and gameplay rules unchanged.

## Capabilities

### New Capabilities

- `webgl-delivery-performance`: Defines versioned browser caching, compressed Unity asset delivery, and deployment acceptance requirements for the WebGL build.

### Modified Capabilities

None.

## Impact

- Affects `Assets/Editor/WebBuild.cs`, the WebGL template/output HTML, `deploy/server.mjs`, `deploy.ps1`, and WebGL acceptance tooling.
- Changes HTTP response headers and generated asset URLs, but does not change gameplay APIs, persistence, presentation layout, or player controls.
- The player-visible flow remains opening the public URL and reaching the portrait game canvas; validation must run against both local output and the deployed service.
