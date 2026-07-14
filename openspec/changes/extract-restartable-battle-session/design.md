## Context

The app boundary introduces a persistent composition root and navigator, while fixed-step simulation and content catalogs supply deterministic construction inputs. `FruitDefenseGame` still self-instantiates after every scene load and persists, which conflicts with explicit app routing and session disposal.

## Goals / Non-Goals

**Goals:**

- Construct one Battle from one immutable request and dispose it completely.
- Submit terminal results exactly once and preserve local pause/restart controls.
- Make platform visibility transitions safe for fixed-step simulation.
- Retain current presenter and acceptance entrypoints during P0.

**Non-Goals:**

- Lobby/Settlement UI, scene build order, cloud persistence, or automatic resume.
- Battle UI or simulation-rule redesign.

## Decisions

1. `BattleLaunchRequest` contains session ID, level ID, seed, and content version; invalid or repeated initialization returns a structured failure.
2. `IBattleSessionHost.Initialize` receives the request, compiled content/map dependencies, and navigator/result sink. It may succeed only once per host instance.
3. `BattleResult` contains session identity, level, seed, outcome, reached wave, and remaining lives. A submission latch prevents duplicate terminal results.
4. `FruitDefenseGame` removes its runtime initialization hook and persistent lifetime. It owns only objects for the current Battle scene.
5. Background visibility pauses the battle and clears the fixed-step frame accumulator. Foreground never resumes automatically.
6. Pause-menu Restart remains a clean restart of the current request; Settlement Retry is a new request with new session identity and seed.

## Risks / Trade-offs

- **[Old scene still relies on self-bootstrap]** -> Keep a clearly marked editor/acceptance compatibility installer until the final build-scene integration activates Bootstrap.
- **[Terminal state submits repeatedly from OnGUI/Update]** -> Centralize terminal transition and guard it with a result-submitted flag.
- **[Callbacks survive scene destruction]** -> Unsubscribe visibility/navigation callbacks in `OnDestroy` and assert no active session host remains.

## Migration Plan

1. Add session contracts and explicit initialization alongside current bootstrap.
2. Move simulation construction and reset into the session host.
3. Route terminal state and visibility through the contracts.
4. Remove self-bootstrap when the Bootstrap scene integration is ready, then rerun all existing acceptance states.
