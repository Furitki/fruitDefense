## Why

The ordinary WebGL release currently carries acceptance-only URL routing, JavaScript bridge exposure, and mutable battle-state injection, so a production payload can enter fixture states and bypass the normal player flow. We need a hard build boundary now: release builds must expose only player-facing runtime behavior, while a dedicated acceptance build must keep the deterministic evidence contract used by existing Editor and WebGL gates.

## What Changes

- **BREAKING**: Remove acceptance activation from the ordinary WebGL release. `acceptance=1`, direct named-state injection, synthetic safe-area query overrides, the acceptance JavaScript bridge, and access to the Unity instance SHALL NOT become available in the release payload.
- Add a dedicated acceptance WebGL build profile whose identity is distinguishable from the release profile and which retains the existing named battle states, integrated-flow routing, screenshot matrix, manifest fields, payload identity checks, safe-area inset cases, and browser-runner contract.
- **BREAKING**: Stop exposing the mutable `GameSimulation` aggregate through the production battle host contract. Normal app flow and presentation code SHALL use bounded commands and read-only battle state; acceptance-only fixture replacement SHALL be reachable only through the dedicated acceptance surface.
- Add build and validation gates that prove both sides of the boundary: release output contains no acceptance bridge or state-injection entry point, and acceptance output still passes the current Editor smoke and WebGL evidence matrix.
- Preserve the normal `Bootstrap → Lobby → Battle → Settlement` player flow, gameplay rules, save/snapshot formats, deterministic outcomes, runtime UI semantics, visual standard, control behavior, and safe-area behavior derived from the real host platform. This change introduces no balance, content, persistence, or player-visible UI redesign.

## Capabilities

### New Capabilities

- `webgl-acceptance-build-isolation`: Defines separate release and acceptance WebGL surfaces, explicit build identity, release-absence checks, and acceptance-only fixture access.

### Modified Capabilities

- `webgl-visual-acceptance`: Assigns injected canonical states, synthetic inset overrides, manifest capture, and the existing runner to the dedicated acceptance build while retaining real-host release checks that do not require a bridge.
- `p0-integrated-player-flow`: Restricts `acceptance=1`, direct Battle routing, and acceptance bridge forwarding to the acceptance build; the ordinary release always follows the production routing contract.
- `battle-presentation-event-boundary`: Replaces mutable simulation exposure on the production battle host with bounded commands and read-only state, while preserving current presentation-event behavior and acceptance-state parity in the dedicated build.

## Impact

- Runtime and app boundaries: `IBattleSessionHost`, `FruitDefenseGame`, `AppFlowCoordinator`, `RuntimeSafeAreaResolver`, and acceptance-specific state/telemetry entry points.
- WebGL surfaces: `FruitDefenseAcceptance.jslib`, the WebGL template/bootstrap callback, release build generation, and a new dedicated acceptance build output and identity.
- Validation and automation: Editor smoke coverage plus `scripts/accept-webgl-host.ps1` and `scripts/accept-webgl-portrait.ps1` must target the correct build profile and verify release absence as well as acceptance parity.
- Existing automation or local workflows that point acceptance commands at `Builds/WebGL` must move to the dedicated acceptance output; no compatibility bridge or release fallback will remain.
