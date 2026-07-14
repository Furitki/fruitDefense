## Context

`GameSimulation` is deterministic at 20 Hz and now supports version-pinned snapshots. The authoritative `GameState` still contains `CombatEffects`, `Feedback`, and per-step `Cues`; `GameSimulation.Step` ages or clears those collections, while `FruitDefenseGame` renders two of them directly. Those collections are intentionally absent from `BattleSnapshotV1`, but their presence inside `GameState` leaves a presentation back-channel into simulation state and makes visual lifetime depend on logic ticks.

This change must keep the immediate-mode combat UI and all current player-visible flows intact. It must also remain usable by WebGL and future Douyin/WeChat presentation adapters without threads, reflection, or a new package dependency.

## Goals / Non-Goals

**Goals:**

- Define a one-way, ordered event boundary from simulation to presentation.
- Make each pending event consumable once, explicitly discardable, and safe to omit entirely.
- Preserve stable catalog cue and visual IDs in emitted events.
- Keep snapshot payloads and outcome checksums independent of presentation events.
- Move transient visual lifetime and drawing state to `FruitDefenseGame`.
- Preserve the current WebGL canvas, safe-area layout, controls, and 13 acceptance states.

**Non-Goals:**

- Changing damage, targeting, rewards, wave timing, content definitions, or snapshot compatibility.
- Rebuilding the battle UI, changing its draw/hit geometry, or introducing a retained-mode UI framework.
- Replaying historical transient effects after snapshot restore.
- Adding audio, analytics, platform SDKs, arbitrary event scripting, or server battle authority.

## Decisions

### Use a simulation-owned, state-external event stream

`GameSimulation` owns a `BattlePresentationEventStream` that is not reachable through `GameState`. Emission records a monotonically increasing local sequence, current logic tick, event kind, stable cue/visual identifiers, entity references, map position, duration, and optional feedback payload. The stream exposes destructive `DrainTo(...)` and `DiscardPending()` operations, plus a pending count for diagnostics.

This keeps event production next to the rule that caused it while excluding the queue from authoritative state. An alternative event callback was rejected because subscriber exceptions and re-entrant callbacks could run during a simulation step and create a path back into rules. A pull queue keeps consumption outside `Step`.

### Bound the pending queue and drop oldest events

The stream keeps a fixed maximum number of pending events. When no presentation consumer exists, oldest transient events are discarded while sequence numbers continue increasing. This prevents an absent consumer from creating unbounded memory growth, and dropping presentation events cannot affect battle state.

An unbounded list was rejected because headless validation, background operation, or a temporarily unavailable view could accumulate effects indefinitely. Blocking the simulation was rejected because presentation delivery is explicitly non-authoritative.

### Keep stable content IDs separate from local delivery sequence

`CueId` and `VisualId` remain the versioned catalog IDs used by current skill content. `Sequence` only orders delivery within one simulation instance and is reset with a new battle/reset. Snapshot restore discards pending events and restarts the local presentation stream; sequence numbers are therefore not persisted and are not gameplay identities.

This avoids adding delivery state to snapshot v1 while retaining stable asset routing identifiers across save/restore and platform clients.

### Own effect lifetime in a presentation buffer

`FruitDefenseGame` owns a `BattlePresentationBuffer`. Each `Update` drains the simulation stream, converts cue events to the existing combat-effect shapes, appends feedback entries, and ages those entries using unscaled frame delta. `OnGUI` reads only this local buffer for transient effects. Persistent visuals such as plants, enemies, and projectiles continue to be drawn from authoritative state and naturally rebuild after a view restart.

The buffer never invokes simulation commands while consuming, aging, clearing, or drawing events. Existing buttons and input handlers remain the only presentation-to-simulation command path.

### Clear transient presentation at lifecycle boundaries

`GameSimulation.Reset` and successful snapshot restore clear the pending stream. `FruitDefenseGame` clears its local buffer when configuring acceptance state, restarting a run, or replacing/restoring the active simulation. No historical transient cue is replayed. This is acceptable because transient effects are disposable; authoritative entities provide the rebuild surface.

## Risks / Trade-offs

- [A consumer that drains late can display stale effects] -> The queue is bounded and consumers may discard pending events at route/resume boundaries.
- [Snapshot restore no longer recreates an impact animation visible just before save] -> Transient effects are explicitly excluded; persistent entities/projectiles redraw from restored state.
- [Moving TTL to frame time can make cosmetic timing differ slightly from old logic-step timing] -> Durations remain unchanged and only cosmetic lifetime changes; battle checksums and timing remain fixed-step.
- [Legacy tests directly mutate transient `GameState` lists] -> Migrate fixtures to drain/inspect events or clear the stream and add a dedicated boundary smoke.

## Migration Plan

1. Add the event model, bounded stream, and presentation buffer without changing event sources.
2. Route `EmitCue` and feedback helpers into the stream and remove transient collections/fading from `GameState` and `Step`.
3. Update `FruitDefenseGame` and existing smoke fixtures to consume the new boundary.
4. Run dedicated boundary, composable-skill, snapshot, deterministic, project, WebGL build, and 13-state acceptance gates.

Rollback is a single change revert because the snapshot schema, content catalog, scene/build configuration, and public battle-session contracts do not change.

## Open Questions

None. Audio and platform-specific event consumers will be separate changes built on this boundary.
