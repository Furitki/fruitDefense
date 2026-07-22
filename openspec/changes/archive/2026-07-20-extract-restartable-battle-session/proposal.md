## Why

`FruitDefenseGame` currently creates itself after scene load, persists globally, and owns the only battle session. The app cannot safely enter battle from a lobby, dispose it after settlement, or retry without relying on global reset behavior.

## What Changes

- Define immutable battle launch and result contracts plus a battle-session host lifecycle.
- Remove battle-owned runtime bootstrap and `DontDestroyOnLoad`; only the app Bootstrap remains persistent.
- Initialize each battle exactly once from level, seed, session, and content version.
- Submit victory or defeat at most once and let the app navigator choose Settlement, return, or retry.
- Automatically pause on platform Background, reset the fixed-step accumulator, and require player action after Foreground.
- Preserve the existing pause/restart behavior and all named battle acceptance states.

## Capabilities

### New Capabilities

- `restartable-battle-session`: Defines disposable battle-session initialization, result submission, background handling, and cleanup.

### Modified Capabilities

None.

## Impact

- Changes `FruitDefenseGame` lifecycle and simulation construction while preserving its current presentation.
- Adds battle-session contracts consumed by the app navigator and shell.
- Does not add Lobby/Settlement UI, persistence, cloud services, or platform SDKs.
