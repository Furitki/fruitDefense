## 1. Establish WebGL Build Profiles

- [x] 1.1 Add immutable release and acceptance build-profile descriptors with exact outputs `Builds/WebGL` and `Builds/WebGL-Acceptance`, and keep `Builds/WebGL` as the only publishable output.
- [x] 1.2 Refactor `FruitDefense.Editor.WebBuild` so both entries share scenes, template, compression, stripping, and payload processing while only the acceptance entry supplies `FRUIT_DEFENSE_ACCEPTANCE` through `BuildPlayerOptions.extraScriptingDefines`.
- [x] 1.3 Emit and validate the exact `fruit-defense-build-profile` host identity for each output before any runtime isolation work depends on it.
- [x] 1.4 Add Editor validation proving the two profiles use identical release inputs and that acceptance builds never mutate persistent PlayerSettings scripting defines, including failure cleanup.

## 2. Introduce the Acceptance-Only Runtime Port

- [x] 2.1 Add a finite `IAcceptanceBattlePort` and explicit success/failure results under `FRUIT_DEFENSE_ACCEPTANCE` for known named states and terminal fixtures without extending `IBattleSessionHost`.
- [x] 2.2 Move `FruitDefenseGame` named-state replacement, acceptance telemetry access/publication, and related fixture state behind the acceptance define while preserving the existing state names and telemetry payload.
- [x] 2.3 Gate `AppFlowCoordinator` acceptance route detection, direct routing, `ConfigureAcceptanceFlow`, route-ready publication, and native bridge imports behind the acceptance define.
- [x] 2.4 Split safe-area resolution so ordinary runtime uses only `Screen.safeArea` and an acceptance-only decorator retains the existing `acceptance=1`, `safeTop`, and `safeBottom` fixture contract.
- [x] 2.5 Gate WebGL native bridge references so the dedicated profile retains the existing JavaScript globals and message names while release linking has no acceptance symbol dependency.

## 3. Make Acceptance Automation Fail Closed

- [x] 3.1 Add one reusable PowerShell profile probe that reads the host identity and rejects missing, unknown, or mismatched WebGL profiles before browser commands run.
- [x] 3.2 Update `accept-webgl-portrait.ps1` to target an explicitly supplied acceptance URL or `Builds/WebGL-Acceptance`, require the `acceptance` profile before `SendMessage`, and remove every fallback to `Builds/WebGL`.
- [x] 3.3 Separate `accept-webgl-host.ps1` release-host checks from injected acceptance checks so release mode never appends acceptance or synthetic-inset queries and acceptance mode requires the dedicated profile.
- [x] 3.4 Preserve the existing acceptance JSON manifest fields, screenshot names, route identity, payload checks, and safe-area matrix while recording the verified profile identity for the run.
- [x] 3.5 Add script self-checks for correct acceptance, incorrect release, missing marker, unknown marker, and absent acceptance output cases, including proof that no fallback or Unity message occurs on failure.

## 4. Close the Production Battle Host Boundary

- [x] 4.1 Add an immutable `BattleSessionStatus` value containing only the finite phase, wave, lives, and completion facts required by orchestration, with no `GameState`, collections, or aggregate reference.
- [x] 4.2 Remove `GameSimulation Simulation` from `IBattleSessionHost` and implement immutable status plus the existing bounded lifecycle, visibility, restart, snapshot, result, and disposal commands in `FruitDefenseGame`.
- [x] 4.3 Route acceptance victory/defeat flow through `IAcceptanceBattlePort` and eliminate all `AppFlowCoordinator` writes to `Simulation.State`.
- [x] 4.4 Migrate GM stress, test doubles, and every remaining `IBattleSessionHost` caller to immutable observations or bounded commands without adding a mutable compatibility accessor.
- [x] 4.5 Add contract tests proving production callers cannot obtain mutable simulation state and unknown acceptance commands leave authoritative state unchanged.

## 5. Migrate Validation to the New Boundaries

- [x] 5.1 Replace Editor tests that reflect over public `ConfigureAcceptanceState` or assume release-visible acceptance methods with acceptance-port behavior tests and generated-release absence assertions.
- [x] 5.2 Update battle-session smoke coverage to assert immutable status, bounded result submission, restart, visibility, snapshot export/restore, and disposal behavior after the host API change.
- [x] 5.3 Add separate safe-area tests proving release URLs cannot override the system rectangle and the dedicated acceptance profile still produces the existing full/inset matrix.
- [x] 5.4 Add acceptance route and telemetry tests proving the dedicated profile retains current route identity, named states, terminal fixtures, and combat-feedback payloads.
- [x] 5.5 Update aggregate validation registration so stable automated checks remain under `Assets/Editor/Tests` and run through `FruitDefense.Editor.ProjectSetup.SmokeValidate` without adding disposable menu commands.

Implementation note (2026-08-26): tasks 4.1–4.5 and 5.2 were migrated in one source cutover. `BattleSessionHostSmoke` now builds detached current-schema snapshots for restore and terminal setup, checks the immutable status and bounded commands, reflects the public host contract for aggregate leaks, and verifies unknown acceptance commands preserve canonical snapshot bytes and status. Host initialization receives one `CompiledLevelCatalog` authority, resolves `request.LevelId` internally, and constructs the simulation through the catalog-owned current-snapshot source identity; detached `ResolvedLevelDefinition` initialization is not retained. GM and terrain callers use status only, and `AppFlowCoordinator` delegates terminal fixtures exclusively to `IAcceptanceBattlePort`. Source/diff audits are green; coordinated Unity compilation is green, while the corrected catalog-authority host smoke awaits its coordinated rerun. Task 5.1 remains open until the generated-release absence half is closed by the tasks 6 artifact gate.

## 6. Remove the Shared Release/Acceptance Path

- [x] 6.1 Remove the unconditional WebGL index rewrite that exposes `fruitDefensePendingUnityInstance` or `fruitDefenseUnityInstance` from release output and emit it only for the acceptance profile.
- [x] 6.2 Delete unguarded acceptance URL parsing, route forwarding, state-entry methods, telemetry exports, synthetic safe-area overrides, and any obsolete helper made unreachable by the new port.
- [x] 6.3 Delete runner compatibility probes, legacy-profile handling, no-op bridge paths, and release-output fallbacks rather than retaining aliases or migration shims.
- [x] 6.4 Add a release post-build absence gate that scans generated host, loader, framework, and payload artifacts for every forbidden bridge global, native symbol, message entry point, and acceptance query bootstrap.
- [x] 6.5 Add an acceptance post-build presence gate that verifies the profile marker, bridge symbols, state entry points, route identity, and telemetry surface before live capture begins.
- [x] 6.6 Run a repository-wide caller and symbol audit to prove the mutable Host accessor and old shared acceptance path have no remaining production references.

## 7. Build and Complete End-to-End Verification

- [x] 7.1 Run `openspec validate isolate-webgl-acceptance-runtime --strict` and the aggregate Unity Editor smoke, deterministic smoke, composable-skill smoke, and snapshot smoke after all migrations.
- [x] 7.2 Build ordinary release WebGL through `FruitDefense.Editor.WebBuild.Build` and dedicated acceptance WebGL through the new stable entry from the same revision, then record both profile identities and payload hashes.
- [ ] 7.3 Run release post-build scanning and real-browser host checks at canonical portrait and 1280×720 sizes, including an acceptance-shaped URL that must remain on the production Lobby flow with no bridge, fixture, Unity instance, or synthetic inset.
- [ ] 7.4 Run acceptance post-build presence checks and the complete Lobby/Battle/Settlement flow, 13-state Battle matrix, 402×874 full/inset matrix, desktop host, interaction, geometry, identity, cache, and manifest acceptance suites.
- [ ] 7.5 Compare release and acceptance evidence to prove identical scenes, content, theme, ArtSet, template source, and source revision while retaining distinct profile and payload identities.
- [ ] 7.6 Review the final working tree for disposable helpers, generated evidence in production Resources, compatibility code, or unrelated changes, and hand off exact commands, outputs, manifests, and any remaining manual visual review.

Implementation note (2026-08-26, final structure-refactor pass): `WebBuildArtifactSurfaceSmoke` now performs strict generated-artifact release absence and acceptance presence scans over host, loader, data, framework, and Brotli-decoded wasm payloads; `WebBuildProfileSmoke` also enforces the exact partial-file surface and failure cleanup. `AcceptanceRuntimeIsolationSmoke` and the aggregate project/P0 suites are green. Fresh release and acceptance WebGL builds recorded distinct profile markers and payload hashes, the dedicated acceptance flow/desktop-host manifests are accepted, and an acceptance-shaped release URL remained on the production Lobby flow with the bridge, fixture state, Unity-instance globals, and synthetic safe-area override absent. Tasks 7.3–7.6 remain open because the exact canonical portrait release-host run, complete dedicated-profile matrix rerun/comparison record, and final OpenSpec completion review have not all been captured in one completion pass.
