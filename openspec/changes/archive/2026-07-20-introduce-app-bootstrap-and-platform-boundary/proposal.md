## Why

The runtime currently creates `FruitDefenseGame` directly after any scene load and makes that presentation object persistent, leaving no reusable application lifecycle for a lobby, settlement, platform initialization, or later mini-game SDK integration. A small composition root and explicit platform/navigation contracts are needed before the player flow can grow without coupling gameplay to host-specific APIs.

## What Changes

- Introduce a unique `AppBootstrap` composition root that selects and initializes the current host adapter and owns application navigation.
- Introduce platform-neutral launch, initialization, visibility, and platform identity contracts using Unity coroutines, callbacks, and events.
- Provide working Editor and Web platform adapters.
- Reserve explicit Douyin and WeChat adapter slots that report unavailable until their SDK-backed implementations are installed; they never silently fall back to Web behavior.
- Introduce an `IAppNavigator` state machine for Lobby, Battle, and Settlement routes with guarded transitions and deterministic failure behavior.
- Preserve the current `FruitDefenseGame` self-bootstrap, Main scene, build settings, and immediate battle flow during this first boundary-only increment.
- Add edit-time/runtime contract validation without changing gameplay, persistence, or player-visible UI.

## Capabilities

### New Capabilities

- `app-platform-boundary`: Application composition, platform adapter selection/initialization, launch context, visibility lifecycle, and unavailable mini-game host behavior.
- `app-route-navigation`: Guarded Lobby/Battle/Settlement route state and transition rules independent from scene loading and presentation.

### Modified Capabilities

None. The repository has no promoted baseline specifications for application lifecycle or platform integration.

## Impact

- New runtime code under `Assets/Scripts/App` and `Assets/Scripts/Platform`.
- Public contracts are established for later lobby, battle-session, settlement, Douyin, and WeChat changes.
- No changes to `FruitDefenseGame`, gameplay simulation, scenes, project/build settings, WebGL build entry, persistence, or backend behavior.
