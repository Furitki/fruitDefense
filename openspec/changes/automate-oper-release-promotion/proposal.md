## Why

Promoting an approved development revision to `oper` currently requires several manual Git commands and depends on the operator remembering every cleanliness, ancestry, worktree, and push check. A maintained, safe-by-default entry point should make the established release-line policy repeatable without including uncommitted development work.

## What Changes

- Add a PowerShell release-promotion script that plans by default and requires explicit `-Execute` authorization before changing Git state.
- Resolve the dedicated `oper` worktree from Git metadata instead of relying on a machine-specific path.
- Require the source checkout to be on `main`, require a clean `oper` worktree, and allow promotion only when both the local and remote `oper` histories are already contained by the committed `main` revision.
- Push the resulting `oper` revision to `origin` and verify the local and remote release refs agree.
- Report source dirtiness in the plan and explicitly exclude all uncommitted source changes.
- Document the repeatable promotion workflow separately from WebGL build and online publication.

## Capabilities

### New Capabilities

- `oper-release-promotion`: Safe planning and explicit execution of a committed `main` revision promotion to the dedicated `oper` release branch and worktree.

### Modified Capabilities

None.

## Impact

- Adds `scripts/promote-oper.ps1` and focused automated PowerShell validation.
- Updates `docs/build-and-release-pipelines.md` with plan and execution commands.
- Uses the existing Git executable, `main`, `oper`, `origin`, and the dedicated release worktree; no new dependency is introduced.
- Does not change gameplay, persistence, runtime UI, Unity settings, WebGL artifacts, the server, or Douyin/WeChat gates.
