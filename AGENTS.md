# Project Agent Instructions

## Core values

- 不要保留向后兼容性。应删除过时的实现路径，而不是添加兼容层、回退机制或迁移逻辑。
- 选择能够完整满足当前需求的最简单实现。避免预先设计抽象层、配置项和间接调用。
- 以分层方式逐步扩展系统。先实现能够端到端运行的最小版本，再以已经可用的产品为基础逐项增加能力。绝不能为了尚未完成的复杂设计而牺牲一个可以正常工作的产品。
- 保持组件模块化，清晰分离不同关注点。
- 如果成熟且维护良好的库能够降低整体复杂度或提高可靠性，应优先使用。没有明确理由时，不要重复实现常见功能。
- 在自行实现或添加新依赖之前，优先利用项目中已有的依赖。不要在没有查阅文档和类型定义的情况下，断定某个库不具备所需能力。
- 从长期角度作出架构决策。不要接受只能暂时使用、以后还需要替换的权宜方案。

## Project baseline

- This is a Unity `6000.3.19f1` portrait game project.
- The release scene flow is `Bootstrap → Lobby → Battle → Settlement`.
- Treat ordinary WebGL as the shared build and acceptance baseline, not as proof that Douyin or WeChat conversion has succeeded.
- Preserve unrelated user changes and keep edits scoped to the requested work.

## Editor tooling hygiene

- Put stable, user-facing Unity editor workflows under `Assets/Editor/Tools/` and automated validation under `Assets/Editor/Tests/`; keep reusable test data under `Assets/Editor/Tests/Fixtures/`.
- Keep daily authoring commands in their task-oriented `Fruit Defense/...` menus. Automated checks may expose only clearly named `Fruit Defense/Validation/...` suite entries; do not scatter individual smoke commands through the daily tool menus.
- One-shot runners, temporary importers, task-specific debug commands, marker files, and disposable acceptance helpers must be removed after the owning task is validated.
- Test fixtures and acceptance catalogs must never be stored under production `Resources`, added to release scenes, or merged into the production playable catalog. Load or inject them explicitly from Editor tests.
- Preserve Unity `.meta` files and GUIDs when reorganizing editor scripts or fixtures, and rerun the aggregate editor smoke after moves.

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
