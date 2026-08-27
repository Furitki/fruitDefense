## Context

The online publisher currently accepts an operator-configurable expected branch and defaults it to `main`. That leaves server publication coupled to the active development line and does not prevent a developer from selecting another branch. The project also has a heavily modified `main` working directory, so the release checkout must be created as a separate clean Git worktree rather than by switching or copying files in place.

## Goals / Non-Goals

**Goals:**

- Establish `oper` from the current committed `main` revision as the dedicated server-release line.
- Keep the release checkout separate from the active development working directory.
- Make the maintained online publisher accept server publication only when its executing checkout is on `oper`.
- Preserve existing clean-source, build-manifest, acceptance, and ordinary-WebGL-only gates.

**Non-Goals:**

- This does not publish a new WebGL release or alter the server.
- This does not change gameplay, persistence, runtime UI, Unity project settings, or mini-game platform gates.
- This does not introduce a long-lived merge workflow, release channel system, or CI service.

## Decisions

### Create a complete Git worktree instead of copying selected project folders

`oper` is created at the committed `main` revision and checked out at `E:\project\unity\furitDefense-oper`. A Git worktree contains every versioned project file, including Unity `.meta`, `Packages`, `ProjectSettings`, scripts, specifications, and documentation, while excluded generated directories remain excluded through `.gitignore`.

This avoids the incomplete-reference risk of copying only `Assets` or another hand-maintained list, and it leaves the dirty development checkout untouched.

### Hard-code the release branch in the publisher

`scripts/publish-online.ps1` will remove `-ExpectedBranch` and use a single internal `oper` value for its execute gate. Plan-only mode remains non-mutating and reports the required branch plus the current branch; `-Execute` fails before the build or deployment when the current checkout is not `oper`.

Keeping an override would turn the rule into an operator preference. Hard-coding the one supported branch is simpler and makes the release provenance contract explicit.

### Promote changes deliberately

`main` continues normal development. A release candidate reaches `oper` only through a deliberate fast-forward/merge decision or selected commit after the desired source is committed and validated. Server releases are built, accepted, and published from the `oper` worktree only. The release commit is tagged after a successful publication.

## Risks / Trade-offs

- [A stale `oper` misses a desired fix] → Promotion is an explicit release-preparation step; record the selected revision before building.
- [A user invokes an old script from an old checkout] → The maintained publisher and its documented workflow enforce `oper`; repository-host branch protection or server-side policy remains a separate administrative control.
- [Two Unity worktrees are opened concurrently] → Run Unity build jobs one at a time, retaining the existing exclusive-project-access gate.
- [Ignored build artifacts are mistaken for release source] → Build manifests remain revision-bound and reject dirty source before publication.
