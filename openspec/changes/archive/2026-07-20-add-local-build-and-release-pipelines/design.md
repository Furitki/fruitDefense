## Context

The repository already exposes the authoritative P0 gate through `FruitDefense.Editor.P0ValidationSuite.Run`, the WebGL builder through `FruitDefense.Editor.WebBuild.Build`, Windows standalone generation through Unity's `-buildWindows64Player`, and an accepted remote WebGL workflow through `deploy.ps1`. These commands work, but they do not yet form a stable operator interface with consistent preflight checks, target selection, logs, or machine-readable evidence.

The immediate need is local automation, not a Jenkins controller or hosted workflow. The scripts must work in Windows PowerShell 5.1, preserve the exact Unity `6000.3.19f1` baseline, serialize Unity access to the project, and keep any actual server mutation behind explicit operator intent.

## Goals / Non-Goals

**Goals:**

- Provide two clear entry points: one local build pipeline and one online WebGL publication pipeline.
- Let the local pipeline build Web, PC, or both after one P0 gate.
- Produce stable logs and ignored JSON manifests that identify the source revision, dirty state, targets, artifact sizes, hashes, and Web content version.
- Make the online pipeline non-publishing by default and require explicit execution plus release preconditions.
- Reuse the existing deployment transport, health checks, cache-header checks, and acceptance rather than duplicate them.

**Non-Goals:**

- Add Jenkins, GitHub Actions, a scheduler, build queue, artifact server, or remote runner in this change.
- Publish to the live server while implementing or validating the preset.
- Authorize Douyin or WeChat adapters, conversion, upload, or device release.
- Change gameplay, player flow, persistence, scenes, UI, or build contents.

## Decisions

### Two entry points, with Web and PC as local target choices

`scripts/build-local.ps1` owns local P0 validation and accepts `Web`, `PC`, or `All`. `scripts/publish-online.ps1` owns release planning and explicit online execution. Three independent scripts were rejected because Web and PC share the same Unity/environment/P0 preflight and should not drift.

### Share process and evidence helpers

`scripts/pipeline-common.ps1` will own project-version checks, module checks, serialized Unity invocation, success-marker checks, Git metadata, directory sizing, hashing, and JSON output. Keeping these mechanics in one helper avoids different exit-code and log semantics between targets.

### Build targets sequentially behind a named mutex

Unity project access is serialized with a Windows named mutex. A local `All` build runs P0, Web, then PC in one process-level critical section. Parallel Unity invocations were rejected because they can contend for `Library`, generated project state, and the same output directories.

### Keep local builds permissive but observable

Developers may build a dirty working tree locally; the manifest records that state and the exact `HEAD`. Rejecting dirty local builds was rejected because preview packages are useful during development. Online publication remains strict and accepts only a clean expected branch.

### Make publication plan-only unless `-Execute` is present

Without `-Execute`, the online entry prints the resolved server, branch, artifact, key path, and planned gates, then emits a success marker without checking the key or making network calls. `-Execute` gates on the expected branch, clean working tree, key presence, and a fresh Web manifest before delegating to `deploy.ps1 -SkipBuild`.

### Bind skipped builds to exact evidence

`-SkipBuild` is permitted only when the local manifest records a successful Web target for the current clean revision and the current `Builds/WebGL/index.html` hash matches the manifest. Checking only for `index.html` was rejected because it allows stale output to be published.

### Leave transport and acceptance in `deploy.ps1`

The online entry does not reimplement archive creation, SSH/SCP, service restart, health checks, WebGL delivery headers, or local/public portrait acceptance. `deploy.ps1` remains the transport owner, while the new wrapper owns authorization and source/artifact provenance.

## Risks / Trade-offs

- [A local build from dirty sources could be mistaken for a release] -> Record dirty state prominently in the manifest and reject it in the online pipeline.
- [A Unity process or another pipeline could corrupt shared outputs] -> Acquire a named mutex before validation or building and always release it in `finally`.
- [The default online command could accidentally mutate the server] -> Default to plan-only and require the literal `-Execute` switch for all key and network operations.
- [A skipped build could publish stale WebGL files] -> Require matching revision, clean-state evidence, Web target evidence, and current index hash.
- [The wrapper and deploy script could drift] -> Keep the wrapper thin and delegate existing transport/acceptance behavior to `deploy.ps1`.
- [Generated logs and manifests add workspace noise] -> Store them only under already ignored `Logs/` and `Builds/Pipeline/` paths.

## Migration Plan

1. Add the common helper and local build entry.
2. Add the plan-first online publication entry.
3. Add concise README routing plus a detailed operator document.
4. Parse-check all scripts, run strict OpenSpec validation, execute an `All` local build, and execute only the online plan mode.
5. Leave the change unarchived for review; rollback removes the new scripts/docs and OpenSpec change without touching runtime or server state.

## Open Questions

None. A later CI change can call these same local entry points from GitHub Actions or another orchestrator.
