## ADDED Requirements

### Requirement: Promotion defaults to a non-mutating plan
The release-promotion entry point SHALL default to a plan that reports the fixed source branch, release branch, remote, selected committed revision, source dirtiness, discovered release worktree, ancestry readiness, and the fact that uncommitted files are excluded. Plan mode MUST NOT fetch, merge, push, build, publish, tag, commit, stash, or modify either worktree.

#### Scenario: Operator inspects a promotion
- **WHEN** the operator invokes the release-promotion script without `-Execute`
- **THEN** the script prints the resolved promotion plan and a stable plan-success marker without changing local or remote Git refs

#### Scenario: Source checkout contains uncommitted work
- **WHEN** plan mode observes modified or untracked files in the `main` checkout
- **THEN** the plan reports that the source is dirty and that those files are excluded from the selected committed revision

### Requirement: Execution requires the fixed release topology and safe history
The release-promotion entry point MUST require explicit `-Execute` authorization, execution from `main`, exactly one worktree checked out on `oper`, a clean `oper` worktree, an available `origin/oper`, and a selected source revision that contains both the local and fetched remote `oper` histories. Branch and remote names MUST NOT be operator-configurable.

#### Scenario: A release gate is not satisfied
- **WHEN** the source checkout is not on `main`, the `oper` worktree is missing or dirty, the remote release ref is unavailable, or either release history is not an ancestor of the selected source revision
- **THEN** execution fails before merging or pushing and does not rewrite, clean, stash, or discard any worktree

#### Scenario: Source checkout is dirty but its committed revision is eligible
- **WHEN** the source checkout has uncommitted files while its committed `main` revision satisfies every release-history gate
- **THEN** execution warns that uncommitted files are excluded and promotes only the captured committed revision

### Requirement: Promotion is fast-forward-only and remotely verified
Authorized execution SHALL fast-forward the clean `oper` worktree to the captured committed `main` revision, perform a normal non-force push to `origin/oper`, and verify that the remote branch resolves to exactly that revision before emitting a stable success marker.

#### Scenario: Eligible revision is promoted
- **WHEN** the operator executes promotion and all release gates pass
- **THEN** local `oper` and `origin/oper` both resolve to the captured `main` revision and the script reports promotion success

#### Scenario: Revision is already promoted
- **WHEN** local `oper` and `origin/oper` already equal the selected `main` revision
- **THEN** execution succeeds idempotently without creating a commit or rewriting history

#### Scenario: Remote changes during promotion
- **WHEN** the normal push is rejected or the post-push remote ref does not equal the captured source revision
- **THEN** the script fails and MUST NOT claim promotion success

### Requirement: Promotion remains separate from release publication
The release-promotion entry point SHALL stop after Git branch promotion and verification. It MUST NOT invoke Unity builds, WebGL acceptance, server publication, release tagging, or Douyin/WeChat conversion.

#### Scenario: Git promotion succeeds
- **WHEN** the exact source revision has been verified on local and remote `oper`
- **THEN** the script exits without creating build artifacts, changing the server, tagging the revision, or claiming any platform release
