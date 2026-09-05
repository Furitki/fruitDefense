## 1. Promotion entry point

- [x] 1.1 Implement portable `oper` worktree discovery and a structured, non-mutating default plan in `scripts/promote-oper.ps1`.
- [x] 1.2 Implement explicit execution gates, fast-forward-only promotion, normal push, exact remote verification, and stable result markers.

## 2. Validation and operator guidance

- [x] 2.1 Add disposable-repository integration coverage for plan immutability, dirty-source exclusion, successful/idempotent execution, and rejection gates.
- [x] 2.2 Document the promotion plan, execution command, exclusions, and separation from WebGL publication in the release pipeline guide.
- [x] 2.3 Run the integration validation, inspect the real-repository plan, and pass strict OpenSpec validation.
