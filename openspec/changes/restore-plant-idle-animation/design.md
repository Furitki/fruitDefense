## Context

`FruitDefenseGame` renders each map plant from one atlas sprite. Its current `DrawAnimatedPlant` path changes the draw rectangle only while `ActionUntil` is in the future, so the visual returns to a perfectly static pose between attacks. Combat timing already lives in `GameSimulation` and must remain authoritative.

The fix must preserve the existing immediate-mode hit rectangles, portrait safe-area layout, single-frame atlas, deterministic simulation, and ordinary WebGL release path.

## Goals / Non-Goals

**Goals:**

- Keep planted fruits visibly alive between actions with subtle continuous motion.
- Preserve the stronger, plant-specific attack silhouettes and allow them to repeat after cooldown.
- Keep draw-only motion independent from click, drag, range, targeting, damage, and snapshot state.
- Validate both repeat-attack simulation behavior and the built WebGL portrait surface.

**Non-Goals:**

- Add sprite sheets, Animator controllers, new content fields, or third-party animation dependencies.
- Change combat balance, cooldown semantics, target selection, save formats, or mini-game platform readiness.
- Animate nursery icons or drag ghosts.

## Decisions

1. Compute the idle pose directly in `DrawAnimatedPlant` from simulation elapsed time and a stable plant-ID phase offset. This needs no mutable presentation state, pauses naturally with the battle simulation, and avoids all plants moving in lockstep. A new Animator or sprite-frame system would add unnecessary assets and lifecycle complexity for the current single-frame art.
2. Apply the idle transform only to the local draw rectangle, then layer the existing attack transform over it. The previously computed GUI hit rectangle remains unchanged, so safe-area and interaction geometry do not drift.
3. Keep the motion deliberately small: a slight vertical bob, scale breath, and rotation. Existing attack motion remains visually dominant.
4. Add a simulation smoke that holds a durable enemy in range and asserts that the same plant receives a later `ActionStartedAt` value after cooldown. WebGL portrait acceptance remains the runtime presentation check.

## Risks / Trade-offs

- [Fixed-step elapsed time can make idle motion slightly stepped at low frame rates] → Use slow, low-amplitude motion so the fixed 20 Hz simulation cadence remains unobtrusive.
- [Added transform could visually overlap nearby content] → Keep translation and scale below the existing attack amplitudes and preserve the unchanged logical cell bounds.
- [A test of repeated action timing does not measure pixels] → Pair the editor regression with the existing real-canvas WebGL acceptance and deployed acceptance pipeline.
