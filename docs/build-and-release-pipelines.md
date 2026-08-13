---
id: build-and-release-pipelines
parent: design-kb-home
order: 40
status: active
---

# Local build and online release pipelines

The repository exposes two operator pipelines. They are local PowerShell entry points today and are intentionally shaped so a later GitHub Actions runner can call the same commands without duplicating build logic.

## 1. Local build pipeline

Entry point: `scripts/build-local.ps1`

The local pipeline checks Unity `6000.3.19f1`, requested build modules, and exclusive Unity project access. It runs the unified P0 release gate once, then builds the selected targets sequentially.

```powershell
# WebGL only
powershell -ExecutionPolicy Bypass -File .\scripts\build-local.ps1 -Target Web

# Windows PC only
powershell -ExecutionPolicy Bypass -File .\scripts\build-local.ps1 -Target PC

# WebGL and Windows PC
powershell -ExecutionPolicy Bypass -File .\scripts\build-local.ps1 -Target All
```

Generated outputs:

| Output | Purpose |
|---|---|
| `Builds/WebGL/` | Ordinary WebGL artifact |
| `Builds/Windows/` | Local Windows preview artifact |
| `Logs/pipeline-local-p0.log` | Unified P0 gate log |
| `Logs/pipeline-local-web-first.log` | First deterministic WebGL verification build log |
| `Logs/pipeline-local-web.log` | Second deterministic WebGL build log for the retained candidate |
| `Logs/pipeline-local-pc.log` | Windows build log |
| `Builds/Pipeline/local-build-manifest.json` | Revision, dirty state, target, size, per-payload WebGL full hashes/versions/lengths, two-build comparison, duration, and log evidence |

Local preview builds may use uncommitted source. The manifest records `dirtyBeforeBuild`; this evidence is deliberately rejected by the online pipeline.

Success marker:

```text
FRUIT_DEFENSE_LOCAL_BUILD_PIPELINE_OK
```

## 2. Online WebGL publication pipeline

Entry point: `scripts/publish-online.ps1`

Running the entry without `-Execute` is a non-publishing plan. It prints the resolved target and gates but does not require the SSH key, connect to the server, upload files, or change the remote service.

```powershell
# Safe plan only; no network or server mutation
powershell -ExecutionPolicy Bypass -File .\scripts\publish-online.ps1
```

Plan success marker:

```text
FRUIT_DEFENSE_ONLINE_PUBLISH_PLAN_OK
```

Actual publication requires explicit authorization and all of these conditions:

- current branch matches `-ExpectedBranch` (`main` by default);
- working tree is clean;
- SSH private key exists at `-KeyPath`;
- a P0-validated Web build manifest matches the current commit;
- the current `Builds/WebGL/index.html` hash matches that manifest.

```powershell
# Build a fresh Web artifact, validate it, then publish
powershell -ExecutionPolicy Bypass -File .\scripts\publish-online.ps1 `
  -Execute `
  -KeyPath "$HOME\.ssh\id_ed25519"

# Reuse an existing Web artifact only when its manifest matches exactly
powershell -ExecutionPolicy Bypass -File .\scripts\publish-online.ps1 `
  -Execute `
  -SkipBuild `
  -KeyPath "$HOME\.ssh\id_ed25519"
```

After the source/artifact gates pass, the pipeline delegates to `deploy.ps1 -SkipBuild`. That workflow remains responsible for local portrait acceptance, seeding the current public release into a persistent browser profile, archive/upload, remote service replacement, entry health, WebGL delivery headers, service status, and deployed acceptance. Local acceptance still performs a cold launch followed by a same-profile warm reload. Deployment opens release N before replacement and release N+1 after replacement in the same browser profile; publication fails if an unchanged advertised payload body is downloaded again. Candidate acceptance then repeats a same-release warm reload for the new release.

WebGL payloads use independent SHA-256-derived versions. The production build suppresses Unity's per-build unique identifier, and the local pipeline builds WebGL twice; all four full payload hashes and byte lengths must match before the second output becomes publishable. Unchanged loader, data, framework, or WebAssembly bytes therefore retain their cache key when a different payload changes. The static service grants immutable caching only when the requested version matches the served content and emits a strong content-hash ETag.

Local and online pipeline manifests use schema version 3. Local evidence stores the four complete payload records plus the deterministic comparison. Online evidence classifies payload roles as reused or changed and records expected and observed release-transition download bytes. Older manifest schemas must be rebuilt rather than reused.

Successful publication writes `Builds/Pipeline/online-publish-manifest.json` and emits:

```text
FRUIT_DEFENSE_ONLINE_PUBLISH_OK
```

## Safety and platform boundary

- Generated builds, logs, and manifests remain under ignored `Builds/` and `Logs/` directories.
- Only the online entry's explicit `-Execute` path can reach `deploy.ps1`.
- This workflow publishes ordinary WebGL only. It does not convert, upload, authorize, or claim support for Douyin or WeChat mini games.
- A later CI service should invoke these scripts rather than reimplement Unity commands. Use one concurrent Unity job per project workspace.
