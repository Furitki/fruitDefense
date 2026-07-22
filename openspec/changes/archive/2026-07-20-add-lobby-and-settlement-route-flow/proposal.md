## Why

The player currently enters combat immediately and there is no stable surface for future level selection, growth, settings, or post-battle decisions. P0 needs a minimal but real Lobby and Settlement flow before platform, save, and content-delivery work can integrate safely.

## What Changes

- Add portrait Lobby and Settlement presenters and scene-ready composition components.
- Make Lobby start level `orchard-01` with a new session ID, seed, and current bundled content version.
- Reserve visible but disabled areas for level selection, growth, and settings without implementing their systems.
- Show outcome, reached wave, and remaining lives in Settlement.
- Support Return to Lobby and Retry; Retry keeps level/content version but uses a new session ID and seed.
- Recover safely to Lobby if Settlement has no valid result.

## Capabilities

### New Capabilities

- `lobby-settlement-route-flow`: Defines the minimal app shell, route actions, reserved extension surfaces, and portrait interaction behavior.

### Modified Capabilities

None.

## Impact

- Adds shell presentation code and scene content that consumes the app navigator and battle contracts.
- Does not change battle rules, introduce economy/rewards, or persist progression.
- Requires real WebGL portrait capture during final P0 integration.
