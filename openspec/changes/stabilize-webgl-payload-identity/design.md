## Context

The deployed service already gives each Unity payload a content-derived URL, a strong content ETag, and immutable caching. Same-release warm acceptance transfers zero payload bytes. The remaining failure is build identity: Unity embeds a fresh `build-guid` in `boot.config`, which is stored inside the Brotli-compressed `.data.unityweb` package. Two otherwise equivalent publications therefore produce different data bytes and URLs.

The data package also contains two 2048×2048 terrain source textures solely because they live under `Resources`. Runtime code loads the two generated gameplay atlases but never loads those terrain bake inputs. The source textures are referenced by an editor bake profile and can remain available to editor tooling outside `Resources` with their GUIDs preserved.

Existing acceptance validates cold and warm loads of one release. It does not seed the browser with release N and then observe release N+1, so it cannot prove cross-publication reuse.

## Goals / Non-Goals

**Goals:**

- Produce byte-identical loader, data, framework, and WebAssembly payloads when the same clean revision is built twice with the same Unity version and settings.
- Keep editor-only bake source textures available to authoring tools without shipping them as implicit runtime resources.
- Fail a publishable Web build when its immediate verification build changes any payload digest.
- Measure and record which payloads a publication reuses, changes, and is expected to download.
- Prove unchanged release-N payload bodies are reused when release N+1 opens in the same browser profile.

**Non-Goals:**

- Splitting `.data.unityweb`, introducing Addressables or remote catalogs, or guaranteeing fine-grained downloads for individual art changes.
- Changing gameplay, saves, battle flow, visual layout, platform order, or mini-game support.
- Preserving schema-2 build or publication manifest compatibility.

## Decisions

### Suppress Unity's per-build unique identifier

`WebBuild` will use `BuildOptions.NoUniqueIdentifier`. Unity documents this option as forcing the build GUID to zero, removing the known random `boot.config` input while leaving source-derived output intact. Every automated Unity process will also start with `-buildTarget WebGL`; otherwise a fresh Library can switch from the default target during its first build and produce a different scripting-assembly list than later WebGL-native processes. The local Web pipeline will run the production entry twice and compare the SHA-256 digest and byte length of all four advertised payloads. The second build remains the publish candidate.

Only setting an externally supplied cache version was rejected because immutable URLs must continue to identify actual bytes. Patching compressed output after the build was rejected because it would make Unity packaging harder to validate and maintain.

### Remove editor bake inputs from runtime Resources

The grass and soil HD source PNGs and their `.meta` files will move to `Assets/ArtSources/TempArt`. Their GUIDs remain unchanged, so the existing bake profile continues to reference them. The runtime atlases stay under `Resources` because `FruitDefenseGame` loads them by name.

Deleting the sources was rejected because editor rebaking still needs them. Adding custom runtime filtering was rejected because Unity's existing Resources inclusion rule is avoided simply by placing authoring-only sources outside that folder.

### Treat deterministic comparison as publication evidence

The local manifest schema becomes version 3. Web evidence records a payload map containing role, file name, full SHA-256, 12-character URL version, and byte length, plus the two-build comparison result. The pipeline removes its temporary first-build snapshot after comparison. Any payload mismatch fails the build and no publishable manifest is written.

An optional/manual determinism check was rejected because it could be skipped on the publication path that matters.

### Seed release N and verify release N+1 in one browser profile

Before replacing the public build, deployment will launch the current public release in a fresh persistent Chrome profile and write a cache-seed manifest. After replacement, deployed acceptance launches the candidate using that same on-disk profile and the seed manifest. It classifies roles by version: unchanged roles must remain below the existing per-payload transfer allowance on the candidate's first load; changed roles may download their new bodies. The ordinary same-release warm reload still requires all current payloads to remain below the warm limits.

The profile can be used by separate browser processes because the disk cache survives process exit. This keeps deployment sequencing simple while still validating the browser behavior players experience across visits.

### Compute release delta from observed public and candidate evidence

The publication manifest schema becomes version 3 and records baseline/candidate versions, reused roles, changed roles, expected payload download bytes, observed candidate transfer bytes, and the cross-release acceptance manifest path. With no reachable prior release, publication records an explicit first-release state and skips only the cross-release assertion; all ordinary delivery acceptance remains mandatory.

The old schema is removed rather than supported in parallel.

### Replace the fragile remote header grep with exact shell checks

The remote deployment check will parse the version once and use exact conditional checks whose failure messages name the asset and header. This eliminates the current warning-prone grep expression while keeping the same cache, ETag, MIME, and encoding contract.

## Risks / Trade-offs

- [Every Web build now invokes Unity twice] → Accept the longer build time for publishable determinism evidence; keep P0 validation single-run.
- [Unity may contain another nondeterministic input] → Fail with per-role first/second hashes so the source is diagnosable instead of publishing unstable cache keys.
- [The first fixed release necessarily changes `.data.unityweb`] → Record it as an expected one-time change; subsequent equivalent builds must stabilize.
- [Chrome disk-cache behavior can vary by version] → Record browser version and per-role Resource Timing and retain the existing small header-transfer allowance.
- [A failed deployment can leave the persistent acceptance profile] → Place it under the ignored evidence directory and remove it in deployment cleanup.

## Migration Plan

1. Move editor-only source textures outside `Resources` and verify profile references plus aggregate editor smoke.
2. Enable deterministic Web build identity and make the local Web pipeline produce schema-3 two-build evidence.
3. Add cache seed and cross-release comparison modes to portrait acceptance and wire them into deployment.
4. Build and compare the same clean revision twice; run local acceptance.
5. Seed the currently deployed release, publish the candidate, run same-profile deployed acceptance, and write schema-3 publication evidence.
6. Update the P0 release baseline with the deployed versions, size, and transition result. Rollback continues to use the deployment workflow's prior release artifact; the HTML then advertises that artifact's own content versions.

## Open Questions

None for this change. Fine-grained art delivery remains a separate future content-loading change.
