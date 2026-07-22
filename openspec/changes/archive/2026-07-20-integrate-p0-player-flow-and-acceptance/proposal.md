## Why

The current project boots directly into a persistent battle presenter and validates isolated battle states. P0 needs a single integrated release path that proves Bootstrap, Lobby, restartable Battle, Settlement, local services, and existing battle acceptance work together without regressing the WebGL build.

## What Changes

- Configure the release scene order as Bootstrap, Lobby, Battle, and Settlement.
- Make cold start enter Lobby while preserving an explicit acceptance/development route to Battle.
- Wire session launch, single-result submission, Settlement return, and retry into one player-visible flow.
- Extend editor smoke and WebGL acceptance to cover the complete flow while retaining the existing 13 battle states.
- Add failure-path acceptance for duplicate navigation, missing launch/result data, initialization failure, and background pause.
- Keep platform SDKs, automatic battle resume, economy, and production backend integration outside this change.

## Capabilities

### New Capabilities

- `p0-integrated-player-flow`: Defines the release scene order, routing behavior, complete player flow, and integration acceptance gate.

### Modified Capabilities

None.

## Impact

- Modifies the project setup/build scene configuration, integration composition, WebGL acceptance bridge, and acceptance script.
- Consumes the app, content, deterministic simulation, battle session, lobby, settlement, snapshot, and local-service contracts produced by preceding P0 changes.
- Preserves the current battle rendering and interaction behavior rather than replacing it.
