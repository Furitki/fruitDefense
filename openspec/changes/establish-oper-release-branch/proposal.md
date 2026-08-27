## Why

`main` is an active development workspace and may legitimately contain uncommitted work. Server publication needs a stable, clean source line that can be packaged and released without switching branches or consuming development changes.

## What Changes

- Create `oper` as the dedicated release branch from the current clean `main` commit and provide it in an isolated Git worktree.
- **BREAKING**: make `oper` the only accepted branch for server publication; remove the `main` default and prevent operators from overriding the required branch.
- Document the release-worktree and promotion workflow in the build-and-release pipeline guide.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `online-publish-pipeline`: server publication must require the dedicated `oper` release branch rather than an operator-selectable branch.

## Impact

- Affected operational assets: the Git branch/worktree layout, `scripts/publish-online.ps1`, and `docs/build-and-release-pipelines.md`.
- Gameplay, persistence, runtime UI, platform-adapter behavior, and the existing ordinary-WebGL-only release boundary are unchanged.
