# Project Agent Instructions

## Project baseline

- This is a Unity `6000.3.19f1` portrait game project.
- The release scene flow is `Bootstrap → Lobby → Battle → Settlement`.
- Treat ordinary WebGL as the shared build and acceptance baseline, not as proof that Douyin or WeChat conversion has succeeded.
- Preserve unrelated user changes and keep edits scoped to the requested work.

## Documentation ownership

Use one source of truth for each kind of information:

| Location | Owns |
|---|---|
| `README.md` | How to open and play the project, main engineering entry points, and links to deeper documents |
| `docs/design/game-design-overview.md` | Stable game direction, experience pillars, core loops, content structure, milestone intent, exclusions, and unresolved design questions |
| `openspec/changes/<change>/` | The proposal, requirements, design decisions, tasks, and acceptance contract for one concrete change |
| `docs/p0-release-baseline.md` | The currently verified P0 runtime, artifacts, and release gates |
| `docs/p1-first-wave-gate.md` | The currently verified P1 authorization state and unblock sequence |
| `docs/platform/` | Current platform toolchain, conversion, simulator, device, package, and compatibility evidence |

## Documentation update rules

1. Decide which document owns a fact before editing. Link to the owner instead of copying the same fact into several files.
2. Keep the game-design overview stable. Do not put build hashes, SDK installation state, generated artifact sizes, dated acceptance logs, branch names, or transient task counts in it.
3. Distinguish `已定`, `方向`, `当前基线`, and `待定`. Existing behavior is not automatically the intended permanent design, and an aspiration is not current functionality.
4. Keep milestone sections about goals, scope, dependencies, and exit intent. Put live Green/Yellow/Red status and exact evidence in the gate or platform documents.
5. Separate temporary scope exclusions from lasting architecture boundaries. Payment, ads, economy, progression, or backend work may be deferred without being permanently forbidden.
6. Do not claim a mini-game target is supported from a successful ordinary WebGL run. Douyin and WeChat adapters must remain explicitly unavailable until their own gates authorize them, and they must never silently fall back to the Web adapter.
7. Use OpenSpec for a concrete behavior or capability change. A stable product-direction change or a resolved design question triggers the design synchronization gate below; it does not authorize an automatic overview edit.
8. Keep README concise and current. It should route readers to the design overview, OpenSpec, release baselines, and platform evidence instead of duplicating them.

## Major design synchronization gate

A change is a major design change when it alters one or more of the following:

- game positioning, target audience, platform order, or experience pillars;
- the lobby, pre-battle, battle, settlement, growth, or replay loop;
- foundational combat, resource, reward, progression, economy, or level-structure rules;
- content-extension boundaries, milestone intent, long-term exclusions, or architecture boundaries;
- a `待定` question in `docs/design/game-design-overview.md` by making a concrete decision.

Bug fixes, implementation refactors, acceptance evidence, build metadata, platform readiness status, and balance-number adjustments inside an already approved rule are not major design changes by themselves.

When a major design change is detected:

1. Do not edit `docs/design/game-design-overview.md` or another product-direction document automatically.
2. Tell the user what changed, which document and sections would be affected, and ask: `这项变更会影响策划文档，是否同步更新？`
3. Sync the affected design document only after the user explicitly confirms. An explicit request made up front to update that document already counts as confirmation.
4. If the user does not confirm, leave design documents unchanged and report `策划文档同步待确认` in the handoff. Do not treat silence or implementation approval as documentation approval.
5. Status and evidence documents remain governed by their own requested scope; this gate must not be used to copy transient status into the game-design overview.

Before reporting current release or platform status, re-read the relevant baseline/gate document and verify the working tree rather than relying on the design overview.
