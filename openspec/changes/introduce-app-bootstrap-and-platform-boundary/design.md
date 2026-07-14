## Context

`FruitDefenseGame` is currently created by `RuntimeInitializeOnLoadMethod` after every scene load and marks itself `DontDestroyOnLoad`. That is sufficient for the current single-scene battle, but it gives future lobby, settlement, platform initialization, update, login, and lifecycle code no composition boundary. Platform-specific APIs must also remain outside gameplay and presentation code so Douyin and WeChat SDK work can be isolated later.

This increment intentionally introduces dormant infrastructure. It must compile and be contract-testable while the existing Main scene and direct battle bootstrap remain untouched until the later player-flow integration change.

## Goals / Non-Goals

**Goals:**

- Define platform-neutral launch, initialization, identity, and visibility contracts.
- Provide concrete Editor and Web adapters and explicit unavailable Douyin/WeChat slots.
- Define a guarded, scene-independent route state machine for Lobby, Battle, and Settlement.
- Add one persistent `AppBootstrap` composition-root component with duplicate protection.
- Keep SDK types and host selection out of gameplay and presentation code.

**Non-Goals:**

- Adding or changing scenes, build settings, project settings, lobby UI, or settlement UI.
- Removing `FruitDefenseGame.EnsureBootstrap` or its current persistence behavior.
- Loading scenes from the navigator in this increment.
- Adding real Douyin/WeChat SDKs, login, updates, remote content, backend, or persistence.
- Changing gameplay rules, simulation timing, or the current WebGL player flow.

## Decisions

### Keep contracts in platform and app runtime folders

Platform contracts and implementations live under `Assets/Scripts/Platform`; routing and composition live under `Assets/Scripts/App`. `FruitDefenseGame` and `Assets/Scripts/Core` receive no references to either folder in this change. Later SDK adapters can be moved into their own assembly without changing the contracts.

Putting host checks directly in `FruitDefenseGame` was rejected because it would bind every future gameplay change to platform SDK compilation symbols.

### Use coroutine initialization with one completion callback

`IPlatformAdapter.Initialize` returns `IEnumerator` and reports one `PlatformInitResult` through a callback. Visibility is an event. `AppBootstrap` owns coroutine execution and forwards Unity pause/focus signals to adapters that implement an internal lifecycle receiver.

This matches Unity and WebGL execution without relying on worker threads. A `Task`-first API was rejected because it adds no benefit for callback-based mini-game SDKs and can obscure WebGL execution constraints.

### Select hosts through an explicit factory

`PlatformAdapterFactory.CreateCurrent` selects Editor in the editor and Web in a normal WebGL build. Reserved compile symbols can request Douyin or WeChat, but until their SDK implementations are registered the factory returns an `UnavailablePlatformAdapter` carrying the requested platform ID and a stable `adapter-not-installed` error.

`Create(PlatformId)` is also exposed for deterministic validation and later dependency injection. Douyin and WeChat requests never return a Web adapter. Silent fallback was rejected because it can ship a package that appears initialized while platform login, update, and lifecycle behavior is absent.

### Parse launch query into an immutable snapshot

`PlatformLaunchContext` contains the selected `PlatformId`, the original launch URL, and a copied read-only query dictionary. Web launch context parses `Application.absoluteURL`; Editor context is empty for now. Later platform adapters translate their SDK launch options into the same value object.

Keeping raw SDK launch objects in the public contract was rejected because those objects have host-specific lifetimes and prevent headless contract validation.

### Make navigation a two-phase state machine

`AppNavigator` starts at Lobby and exposes `TryBeginTransition`, `CompleteTransition`, and `FailTransition`. A transition first enters Loading with a pending route; only completion changes the current route. Failure retains the current route and records a stable error. A second transition while Loading is rejected.

Allowed edges are Lobby to Battle, Battle to Settlement, and Settlement to Lobby or Battle. Scene loading remains a later integration responsibility. Direct mutable route fields were rejected because asynchronous scene operations could expose half-completed state or accept duplicate clicks.

### Keep the bootstrap dormant until scene integration

`AppBootstrap` is a normal component rather than a new runtime-initialize hook. Its `Awake` enforces a single persistent instance, constructs the navigator and current adapter, subscribes lifecycle events, and initializes the adapter. It exposes readiness and failure state but draws no UI and loads no scene.

Adding a second automatic bootstrap now was rejected because it would coexist unpredictably with the required unchanged `FruitDefenseGame` automatic bootstrap.

## Risks / Trade-offs

- [Infrastructure is not player-visible yet] -> Validate factory, unavailable slots, query parsing, transition guards, and duplicate bootstrap behavior independently; activate it only in the later scene-flow change.
- [A mini-game build symbol is enabled before its adapter exists] -> Return a failed `PlatformInitResult` with the requested host identity and `adapter-not-installed`; never return Web.
- [Focus and pause callbacks duplicate the same visibility state] -> Adapter bases suppress repeated visibility notifications.
- [Future scene-loading requirements need cancellation] -> Keep loading outside `AppNavigator`; a later scene router can fail the pending transition without changing route contracts.
- [Bootstrap is destroyed during domain/application shutdown] -> Clear the static instance only when the destroyed object owns it and safely unsubscribe/dispose the adapter.

## Migration Plan

1. Add platform value types, adapter contract, factory, Editor/Web implementations, and unavailable mini-game implementations.
2. Add the two-phase application navigator and deterministic contract validation.
3. Add the dormant `AppBootstrap` component and lifecycle forwarding.
4. Compile and run the existing editor smoke validation to prove no current behavior changed.
5. A later change adds Bootstrap/Lobby/Battle/Settlement scenes, removes the old battle self-bootstrap, and activates this root.

Rollback removes the new App and Platform folders. No scenes, player data, or existing runtime entry points require migration.

## Open Questions

None. Real SDK installation and scene routing are deliberately deferred to their named follow-up changes.
