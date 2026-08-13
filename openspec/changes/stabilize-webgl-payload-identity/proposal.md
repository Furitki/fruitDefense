## Why

Same-version warm reloads reuse the browser cache, but rebuilding unchanged WebGL content still changes Unity's `.data.unityweb` bytes because the package embeds a new build GUID. That gives the large data payload a new URL on every publication and makes players download about 13.6 MB again even when the packaged content is unchanged.

## What Changes

- Make ordinary WebGL output deterministic for unchanged source by suppressing Unity's per-build unique identifier.
- Exclude editor-only terrain bake source textures from runtime `Resources` while preserving their asset GUIDs and editor references.
- Extend local build evidence with payload digests, deterministic-rebuild comparison, and expected cross-release download bytes.
- Extend online publication evidence and acceptance so release N cached in one browser profile is followed by release N+1 in that same profile, proving unchanged payload bodies are reused.
- Remove the fragile remote cache-header check that currently emits an avoidable grep warning.
- Keep gameplay, persistence, the ordinary WebGL runtime flow, and mini-game platform boundaries unchanged.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `webgl-delivery-performance`: Require stable payload identity for identical source inputs, exclude editor-only bake inputs from runtime delivery, and verify cross-release browser cache reuse.
- `local-build-pipeline`: Record deterministic double-build evidence and payload-level comparison/download estimates for Web builds.
- `online-publish-pipeline`: Compare the candidate with the previous online manifest and record which payloads were reused or changed during publication.

## Impact

- Affects `Assets/Editor/Tools/WebBuild.cs`, terrain bake source asset locations, `scripts/build-local.ps1`, `scripts/publish-online.ps1`, `scripts/accept-webgl-portrait.ps1`, `deploy.ps1`, pipeline manifests, and release documentation.
- Web builds perform an additional deterministic verification build before a publishable manifest is produced.
- No new runtime dependency, Addressables catalog, remote content system, gameplay rule, or mini-game adapter is introduced.
