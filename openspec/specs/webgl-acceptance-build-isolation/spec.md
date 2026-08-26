# webgl-acceptance-build-isolation Specification

## Purpose
TBD - created by archiving change isolate-webgl-acceptance-runtime. Update Purpose after archive.
## Requirements
### Requirement: WebGL release and acceptance are distinct build profiles
The project SHALL produce an ordinary release profile at `Builds/WebGL` without `FRUIT_DEFENSE_ACCEPTANCE` and a dedicated acceptance profile at `Builds/WebGL-Acceptance` with `FRUIT_DEFENSE_ACCEPTANCE`, and each generated host SHALL declare its exact `fruit-defense-build-profile` identity as `release` or `acceptance`.

#### Scenario: Both profiles are built from one revision
- **WHEN** release and acceptance WebGL outputs are generated for a release candidate
- **THEN** both use the same enabled release scenes, bundled content, runtime UI theme, ArtSet, WebGL template source, compression policy, and source revision
- **AND** their profile identities and payload hashes are recorded separately

#### Scenario: Persistent project settings are inspected after an acceptance build
- **WHEN** the acceptance build completes or fails
- **THEN** `FRUIT_DEFENSE_ACCEPTANCE` has not been added to persistent PlayerSettings scripting defines
- **AND** a later ordinary release build is generated without the acceptance define

### Requirement: Ordinary release output excludes the acceptance surface
The ordinary release WebGL output MUST NOT contain or expose acceptance route activation, named-state or terminal-fixture entry points, synthetic safe-area query overrides, acceptance telemetry publication, acceptance JavaScript globals, acceptance native bridge symbols, or a browser-visible Unity instance.

#### Scenario: Generated release artifacts are validated
- **WHEN** `Builds/WebGL` finishes building
- **THEN** generated host, loader, framework, and payload validation reports the `release` profile
- **AND** fails if any forbidden acceptance symbol, global, entry-point name, or query bootstrap is packaged

#### Scenario: Acceptance-shaped URL opens the release
- **WHEN** the ordinary release is opened with `acceptance=1`, a direct route, named-state data, or `safeTop` and `safeBottom` query values
- **THEN** the normal production route and real `Screen.safeArea` behavior remain authoritative
- **AND** no acceptance identity, Unity instance, fixture state, or synthetic inset becomes observable

### Requirement: Dedicated acceptance output preserves deterministic evidence contracts
The dedicated acceptance WebGL profile SHALL retain the existing acceptance route, named Battle states, terminal fixtures, safe-area inset matrix, route and content identity, telemetry, interaction protocol, screenshot naming, and JSON manifest schema.

#### Scenario: Existing acceptance matrix runs on the dedicated output
- **WHEN** the current portrait and integrated-flow acceptance runners target `Builds/WebGL-Acceptance`
- **THEN** every required state, route, manifest field, safe-area case, identity assertion, screenshot, and input assertion completes without a compatibility adapter

#### Scenario: Acceptance bridge is missing from the dedicated output
- **WHEN** the acceptance post-build validator cannot find the required profile marker, bridge symbols, or state entry points
- **THEN** validation fails before browser capture begins
- **AND** no evidence is signed for that output

### Requirement: Acceptance automation fails closed on profile mismatch
Acceptance automation MUST verify the dedicated acceptance profile before sending any Unity message and MUST NOT fall back to the ordinary release output or treat a missing profile marker as a legacy acceptance build.

#### Scenario: Runner receives the release output
- **WHEN** a portrait, flow, or host acceptance command resolves a payload whose profile is `release`
- **THEN** the command exits with a non-zero result before calling `ConfigureAcceptanceState` or `ConfigureAcceptanceFlow`
- **AND** it does not retry against another output automatically

#### Scenario: Dedicated acceptance output is absent
- **WHEN** an acceptance command cannot resolve `Builds/WebGL-Acceptance` or another explicitly supplied acceptance-profile URL
- **THEN** it reports the missing acceptance artifact
- **AND** it does not probe `Builds/WebGL`
