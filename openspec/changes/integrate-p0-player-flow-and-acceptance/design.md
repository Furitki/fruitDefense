## Context

The baseline has one `Main` scene and `FruitDefenseGame` creates itself after scene load, persists across scenes, and immediately owns both battle simulation and immediate-mode presentation. P0 changes introduce an app composition root, content provider, deterministic simulation, restartable battle session, lobby/settlement surfaces, snapshot protocol, and local service ports. The integration change is the only owner of shared scene configuration and end-to-end acceptance.

## Goals / Non-Goals

**Goals:**

- Produce one release scene order and one active app composition root.
- Preserve the existing battle experience while making battle creation and destruction repeatable.
- Prove cold start, start battle, result, return, retry, background pause, and failure recovery.
- Retain the existing 13 deterministic battle acceptance states behind an explicit development route.

**Non-Goals:**

- Rebuilding the battle UI or implementing the full meta game.
- Platform SDK, cloud save, content CDN, or automatic battle resume.
- Changing current battle balance or acceptance geometry.

## Decisions

1. **Release scenes are explicit:** Bootstrap is build index 0, followed by Lobby, Battle, and Settlement. Only Bootstrap is `DontDestroyOnLoad`.
2. **Cold start enters Lobby:** The acceptance URL may request `route=battle`; production launch data cannot bypass Lobby unless a supported route explicitly authorizes it.
3. **Navigation is serialized:** A transition guard rejects duplicate start/result/retry input while an asynchronous scene load is active.
4. **Battle is disposable:** The battle scene receives one immutable launch request, emits at most one result, and is destroyed before Lobby or a retry session starts.
5. **Acceptance is forwarded by Bootstrap:** `?acceptance=1&route=battle` waits for the Battle scene, then forwards named states to the active battle presenter. Existing state names and canvas controls remain stable.
6. **Failure states are player-visible:** Platform initialization failure remains on Bootstrap with Retry; missing scenes or launch/result data return to a safe route with a structured error rather than producing a blank canvas.
7. **Shared-file ownership remains centralized:** Only this change modifies project setup, build scene lists, WebGL acceptance injection, or the main acceptance script.

## Risks / Trade-offs

- **[Immediate-mode battle presenter assumes global lifetime]** -> Extract only lifecycle/session ownership and retain its rendering methods until the P1 state/presentation change.
- **[Asynchronous load changes acceptance timing]** -> Make the browser bridge wait on Bootstrap's route readiness instead of fixed sleeps.
- **[Retry leaks old entities or callbacks]** -> Allocate a new session ID and seed, dispose the old host, and assert one active battle host.
- **[New shell changes the portrait flow]** -> Derive lobby/settlement hit geometry from their draw layout and capture real 402x874 WebGL frames.

## Migration Plan

1. Merge and validate first-wave app/content/simulation contracts.
2. Merge battle session and shell changes without changing the build scene list.
3. Generate or update all four scenes through `ProjectSetup` and make Bootstrap index 0.
4. Extend smoke validation, build WebGL, and run both existing battle states and full-flow acceptance.
5. If integration fails, restore the previous scene list and direct acceptance route while retaining the independent contracts for correction.
