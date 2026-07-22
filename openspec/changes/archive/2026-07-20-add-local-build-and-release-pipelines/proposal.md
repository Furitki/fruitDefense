## Why

The project has reliable Unity validation, WebGL build, Windows build, and remote deployment commands, but operators must currently assemble them manually and interpret separate logs. Two repo-local pipeline entry points are needed now: one repeatable local build pipeline with Web/PC targets, and one guarded online WebGL publication pipeline that is safe to inspect without publishing.

## What Changes

- Add a local build pipeline with `Web`, `PC`, and `All` target selection.
- Make the local pipeline verify Unity `6000.3.19f1`, required modules, and the unified P0 gate before producing target artifacts.
- Record stable success markers, logs, revision/dirty-state metadata, artifact sizes, and hashes in a machine-readable local manifest.
- Add an online WebGL publication pipeline that defaults to plan-only output and requires an explicit execute switch before any network or server mutation.
- Gate real publication on the expected Git branch, a clean working tree, an existing SSH key, a fresh local Web build, local acceptance, remote health/header checks, and deployed acceptance by delegating to the existing `deploy.ps1` workflow.
- Document the operator commands and the distinction between ordinary WebGL publication and unavailable Douyin/WeChat adapters.
- Do not execute an online publication as part of this change.

## Capabilities

### New Capabilities

- `local-build-pipeline`: Repeatable local P0 validation and Web/PC artifact generation with target selection and manifests.
- `online-publish-pipeline`: Default-safe planning and explicitly authorized WebGL publication using the existing server deployment contract.

### Modified Capabilities

None.

## Impact

- New PowerShell entry points and shared helpers under `scripts/`.
- Existing `deploy.ps1`, `Assets/Editor/WebBuild.cs`, P0 gate, acceptance script, and generated `Builds/`/`Logs/` artifacts are reused rather than replaced.
- README gains a concise pipeline entry and a deeper operator document owns commands and safety behavior.
- Player-visible `Bootstrap → Lobby → Battle → Settlement` behavior, gameplay, persistence, WebGL release semantics, and mini-game readiness remain unchanged.
