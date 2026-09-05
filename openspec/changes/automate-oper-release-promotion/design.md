## Context

`oper` is the only branch authorized for online publication and is checked out in a dedicated Git worktree. Promotion from `main` is currently manual. The development checkout may legitimately be dirty, while the release worktree must remain clean and every promoted object must come from committed history.

The workflow must be portable across machines, safe to inspect, idempotent, and limited to Git release-line preparation. It must not build Unity, publish WebGL, tag a release, or modify development files.

## Goals / Non-Goals

**Goals:**

- Provide one stable PowerShell entry point for planning and executing `main` to `oper` promotion.
- Discover the `oper` worktree through Git metadata.
- Make the committed source revision and exclusion of uncommitted files visible before execution.
- Reject dirty, missing, unrelated, or non-fast-forward release state before changing `oper`.
- Push and verify the exact promoted revision.
- Cover the workflow with a disposable-repository integration test.

**Non-Goals:**

- Automatically commit, stash, discard, or otherwise modify development work.
- Build or publish WebGL, tag releases, merge `oper` back into `main`, or implement a release-channel/CI system.
- Make branch or remote names operator-configurable.
- Change gameplay, persistence, runtime UI, Unity project settings, or mini-game platform readiness.

## Decisions

### Default to a local plan and require `-Execute` for mutation

Running `scripts/promote-oper.ps1` without `-Execute` inspects the local Git graph and prints a structured plan plus a stable success marker. It performs no fetch, merge, or push. Execution fetches the fixed `origin/oper` ref before re-evaluating all gates.

An always-mutating convenience command was rejected because branch promotion is release authorization, not a routine development side effect.

### Fix the topology to `main` → `oper` on `origin`

The script is invoked from the repository checkout containing the maintained script and requires that checkout to be on `main`. It discovers the one worktree whose porcelain metadata names `refs/heads/oper`; no absolute worktree path is stored.

Configurable branch, remote, and worktree parameters were rejected because they would weaken the single release-line contract and create additional untested paths.

### Gate on committed ancestry, not source-worktree cleanliness

The source revision is the committed `main` HEAD. Source dirtiness is reported and warned about but is not included and does not block promotion. The `oper` worktree must be clean. After fetching, both local `oper` and `origin/oper` must be ancestors of the selected source revision, so advancing the release branch cannot rewrite or discard release history.

Requiring a clean development checkout was rejected because it would unnecessarily block promotion of an already approved commit and conflict with the purpose of the dedicated release worktree.

### Fast-forward locally, push normally, then verify the remote ref

Execution fast-forwards the checked-out `oper` worktree to the captured source revision, performs a normal non-force push, and queries `origin` to verify that `refs/heads/oper` equals that exact revision. Re-running at the same revision is a successful no-op.

Force pushing and reset-based recovery were rejected because the release workflow must never rewrite published branch history or discard worktree state.

### Test with isolated disposable Git repositories

A PowerShell integration test creates a temporary bare remote plus `main` and `oper` worktrees. It verifies plan-only immutability, dirty-source exclusion, successful execution/idempotence, and rejection of dirty or non-fast-forward release state without using the real repository refs.

## Risks / Trade-offs

- [The development worktree is dirty and an operator assumes those files are promoted] → The plan exposes source dirtiness, execution emits a warning, and the output states that only the captured commit is eligible.
- [The remote branch moves after fetch] → The normal push rejects the race; success is emitted only after an exact remote-ref query.
- [A push fails after local `oper` advances] → No history is lost; a later run accepts the still-fast-forward local state and retries the normal push.
- [The dedicated worktree is missing or opened on another branch] → Discovery or branch validation fails before merge or push.
