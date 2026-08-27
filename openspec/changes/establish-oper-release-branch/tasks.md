## 1. Release-line establishment

- [x] 1.1 Create `oper` from the current committed `main` revision and add a clean dedicated worktree.
- [ ] 1.2 Publish `oper` to `origin` with upstream tracking while preserving the existing dirty development checkout.

## 2. Publication enforcement

- [x] 2.1 Remove the online publisher's configurable branch parameter and require `oper` for execution.
- [x] 2.2 Document the `oper`-only server-publication and explicit promotion workflow in the release pipeline guide.

## 3. Verification and handoff

- [x] 3.1 Verify plan-only publication remains non-mutating and reports the fixed release branch.
- [x] 3.2 Verify `-Execute` from a non-`oper` clean checkout fails before building or deploying.
- [ ] 3.3 Commit the release-branch policy and OpenSpec artifacts, fast-forward `oper`, and verify both worktrees' Git state.
