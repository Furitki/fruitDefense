## Context

The current WebGL release and visual-acceptance payload are the same build. `WebBuild.Build` injects a browser-visible Unity instance when an acceptance query is present, `AppFlowCoordinator` publishes route identity and accepts terminal-flow commands, and `FruitDefenseGame` accepts named state replacement. `RuntimeSafeAreaResolver` also interprets acceptance query parameters. These paths preserve a strong deterministic evidence workflow, but they are compiled into `Builds/WebGL` and the production `IBattleSessionHost` exposes its mutable `GameSimulation` solely so orchestration and acceptance code can write authoritative state.

The change crosses build tooling, browser bootstrap code, runtime composition, session contracts, Editor validation, and PowerShell acceptance automation. The existing named states, route identity, screenshots, manifest fields, safe-area matrix, payload checks, and interaction assertions are required evidence and cannot be weakened while the production surface is removed.

## Goals / Non-Goals

**Goals:**

- Produce a normal release WebGL payload with no acceptance bridge, named-state entry point, synthetic safe-area override, or browser-visible Unity instance.
- Produce a separately identified acceptance WebGL payload that retains the current deterministic state, route, manifest, safe-area, and runner contracts.
- Remove mutable simulation access from the production battle-host API and express normal orchestration through immutable observations and bounded session commands.
- Make build validation prove release absence and acceptance parity, and make automation fail closed when it receives the wrong profile.
- Delete the old shared release/acceptance path after consumers migrate; do not retain a compatibility bridge or fallback.

**Non-Goals:**

- Changing gameplay rules, balance, deterministic stepping, snapshots, persistence formats, bundled content, or result semantics.
- Redesigning runtime UI, controls, geometry, visual tokens, ArtSet binding, or the real platform safe-area behavior.
- Replacing the existing IMGUI presentation architecture or refactoring unrelated large files.
- Claiming Douyin or WeChat support from either WebGL profile.

## Decisions

### 1. Use compile-time WebGL profiles with explicit output and identity

`FruitDefense.Editor.WebBuild.Build` remains the ordinary release entry and writes `Builds/WebGL` without `FRUIT_DEFENSE_ACCEPTANCE`. A new stable Editor build entry writes `Builds/WebGL-Acceptance` and supplies `FRUIT_DEFENSE_ACCEPTANCE` through `BuildPlayerOptions.extraScriptingDefines`; it does not mutate the project's persistent scripting-define settings.

Both outputs use the same enabled release scenes, WebGL template, compression policy, content, theme, and ArtSet. The generated host contains a required `fruit-defense-build-profile` identity with the exact value `release` or `acceptance`. The profile identity is build metadata, not a URL-selected mode. `Builds/WebGL` remains the only deployable release output.

Using a runtime query flag without a compile-time boundary was rejected because it leaves fixture code and bridge symbols in production. Temporarily changing global PlayerSettings defines was rejected because interrupted builds can leak Editor state into later builds.

### 2. Compile the acceptance port and bridge only into the acceptance profile

All public `SendMessage` acceptance entry points, acceptance route parsing, route-ready publication, combat-feedback telemetry publication, and synthetic `safeTop`/`safeBottom` parsing are placed behind `FRUIT_DEFENSE_ACCEPTANCE`. The acceptance `.jslib` functions are referenced only by code under the same define so WebGL dead-code linking removes them from the release framework. The acceptance build keeps the current JavaScript globals, named `ConfigureAcceptanceState` and `ConfigureAcceptanceFlow` calls, identity payload, and telemetry payload so the evidence runner does not receive a second contract.

The ordinary safe-area resolver reads only `Screen.safeArea`. The acceptance profile decorates that system value with the existing query-driven inset fixture. This prevents a production URL from becoming a hidden layout-control API while leaving actual device and host safe-area behavior unchanged.

Keeping no-op acceptance methods in release was rejected because their presence would preserve the unsupported surface. Renaming the existing acceptance bridge was rejected because it would add migration cost without improving isolation.

### 3. Separate production session control from acceptance fixture mutation

`IBattleSessionHost` no longer exposes `GameSimulation`. It retains bounded lifecycle commands such as initialize, visibility handling, restart, snapshot export/restore, terminal-result submission, and disposal. Any state required by app orchestration is exposed as a new immutable `BattleSessionStatus` value containing only the finite session facts needed by callers; it contains no `GameState`, collections, or mutable aggregate reference.

The view implementation continues to own its simulation privately. Acceptance-only named-state and terminal-outcome mutation move behind an `IAcceptanceBattlePort` compiled only with `FRUIT_DEFENSE_ACCEPTANCE`. `AppFlowCoordinator.ConfigureAcceptanceFlow` resolves that port and requests a finite fixture transition instead of writing `Simulation.State` fields. The port validates known commands and returns an explicit failure for invalid or unavailable transitions.

Adding setter methods to `IBattleSessionHost` was rejected because it would make fixture mutation part of the production contract. Returning a read-only interface over `GameState` was rejected because the underlying mutable object and collections could still escape.

### 4. Preserve one acceptance contract in a dedicated payload

The acceptance profile preserves the current 13 named Battle states, integrated Lobby/Battle/Settlement route sequence, route identity fields, runtime UI identity, safe-area full/inset matrix, screenshot names, interaction protocol, and JSON manifest schema. It also preserves the existing same-payload rule within an individual acceptance run: desktop-host and portrait-route evidence for that run must come from one acceptance payload identity.

Release real-host checks remain valuable but operate without acceptance query parameters or bridge calls. They verify HTTP delivery, load success, complete canvas containment, scrolling, scaling, pointer mapping through player-visible controls, and release profile identity. Injected visual states are evidence from the dedicated acceptance payload and gate publication of the corresponding release revision.

Running the state matrix against release and tolerating a bridge only on localhost was rejected because origin is not a trustworthy build boundary.

### 5. Build gates fail closed on both sides of the boundary

The release post-build validator verifies the `release` profile and scans generated host/framework/loader output for forbidden acceptance globals, native bridge symbols, `ConfigureAcceptanceState`, `ConfigureAcceptanceFlow`, and acceptance-only query bootstrap code. A browser smoke then opens the release with acceptance-shaped query parameters and proves the normal production route remains active, no acceptance identity appears, and no Unity instance is exposed.

The acceptance post-build validator verifies the `acceptance` profile and required bridge symbols before any screenshot work. Existing Editor smoke and WebGL runner assertions then prove named-state, route, manifest, safe-area, geometry, identity, and interaction parity. A missing profile marker is an error, not a legacy profile.

Source-only checks were rejected because IL2CPP stripping and template rewriting determine the actual shipped surface. Browser-only checks were rejected because a dormant symbol can remain packaged without being exercised.

### 6. Acceptance automation never falls back to release

`accept-webgl-host.ps1` and `accept-webgl-portrait.ps1` receive or resolve the dedicated acceptance output/URL, read the build profile before calling `SendMessage`, and abort when the profile is missing or is `release`. They do not probe `Builds/WebGL` if the acceptance output is absent. Ordinary release host validation uses an explicitly release-scoped command path and never appends `acceptance=1`.

Automatic fallback was rejected because it can silently reintroduce the exact production exposure this change removes and can sign evidence from the wrong payload.

## Risks / Trade-offs

- [Acceptance and release payloads can drift] → Build both profiles from the same revision, scenes, content, theme, ArtSet, template source, and build settings; record both profile identities and payload hashes in the release handoff.
- [A `.jslib` symbol may survive despite conditional C# references] → Scan the generated release artifacts and fail the build on every forbidden symbol; restructure the plugin linkage if the linker retains it.
- [Removing `GameSimulation` breaks tests or development presenters] → Migrate each caller to immutable status or an existing bounded command; development-only hosts implement the same production contract without reintroducing an aggregate escape hatch.
- [A runner accidentally signs release evidence] → Validate the profile marker before browser commands and refuse absent, unknown, or release identities.
- [Two outputs increase build time and storage] → Build the acceptance profile only for acceptance workflows; keep `Builds/WebGL` as the sole publication artifact and treat both generated directories as disposable.
- [Compile defines hide uncompiled-path errors] → Editor validation compiles and builds both profiles, and acceptance-specific tests run under the acceptance build entry rather than relying only on the default Editor domain.

## Migration Plan

1. Add the two explicit profile descriptors, acceptance build entry, output directory, and host identity marker. Make profile validation mandatory before changing runtime behavior.
2. Add the acceptance-only port and compile-gate the existing bridge, route, telemetry, state-fixture, and synthetic safe-area paths. Build `Builds/WebGL-Acceptance` and prove the unchanged runner contract there.
3. Update the PowerShell runners and CI/editor commands to require the acceptance profile and remove every fallback to `Builds/WebGL`.
4. Remove `GameSimulation` from `IBattleSessionHost`, add the immutable status value, and migrate normal orchestration plus development/test hosts to bounded commands. Route acceptance terminal fixtures through `IAcceptanceBattlePort`.
5. Remove the old shared runtime path and any now-unused release query handling. Add generated-artifact scans and browser absence checks to the release gate.
6. Run aggregate Editor smoke, deterministic/snapshot smoke, release WebGL host acceptance, and the complete dedicated acceptance matrix from the same revision. Publish only after both profile gates pass.

There is no compatibility phase. If the migration cannot pass both gates, revert the change as one revision and rebuild the former single profile; do not ship dual routing, no-op bridge methods, or runner fallback.

## Open Questions

None. The build define, output paths, identity marker, host boundary, runner behavior, migration order, and removal policy are fixed by this design.
