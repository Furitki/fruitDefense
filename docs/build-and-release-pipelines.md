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
| `Logs/pipeline-local-web.log` | WebGL build log |
| `Logs/pipeline-local-pc.log` | Windows build log |
| `Builds/Pipeline/local-build-manifest.json` | Revision, dirty state, target, size, hash, duration, and log evidence |

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

After the source/artifact gates pass, the pipeline delegates to `deploy.ps1 -SkipBuild`. That existing workflow remains responsible for local portrait acceptance, archive/upload, remote service replacement, entry health, WebGL delivery headers, service status, and deployed acceptance.

Successful publication writes `Builds/Pipeline/online-publish-manifest.json` and emits:

```text
FRUIT_DEFENSE_ONLINE_PUBLISH_OK
```

## Safety and platform boundary

- Generated builds, logs, and manifests remain under ignored `Builds/` and `Logs/` directories.
- Only the online entry's explicit `-Execute` path can reach `deploy.ps1`.
- This workflow publishes ordinary WebGL only. It does not convert, upload, authorize, or claim support for Douyin or WeChat mini games.
- A later CI service should invoke these scripts rather than reimplement Unity commands. Use one concurrent Unity job per project workspace.
