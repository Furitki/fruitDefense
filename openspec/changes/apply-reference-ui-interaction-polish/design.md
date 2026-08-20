## Context

The runtime UI is an immediate-mode system built around `RuntimeUiTheme`, semantic ArtSet slots, authoritative portrait layout helpers, and route presenters. It already records short feedback pulses and draws pressed offsets/opacity, but those pulses are mostly boolean windows: they do not produce a shared eased sample, route reveal, stagger, cancellable visual owner, or one press lifecycle. Lobby, Settlement, and Battle also repeat pointer-inside/pressed detection while activation remains delegated to separate `GUI.Button` calls.

Static analysis of the reference APK shows that its transferable interaction language is comparatively small: high-frequency feedback around 0.10–0.20 seconds, programmatic scale/offset/alpha sequences, cancellation before replay, and separate use of Animator/Spine for high-value authored moments. The reference's outward overshoot, Lua/C# ownership split, overlapping button subclasses, and large family of atomic Tween classes are unsuitable for this project.

The first implementation remains on the existing IMGUI/WebGL architecture. The UI is already mid-flight as a coherent shared system, so adding a second UGUI authority would create the compatibility layer prohibited by project rules. No maintained Tween dependency is already present, and adding one solely for four scalar curves would increase build and lifecycle complexity.

## Goals / Non-Goals

**Goals:**

- Give every shared motion a deterministic unscaled-time sample with scale, alpha, and offset outputs.
- Keep one replaceable pulse per owned feedback target so replay and route changes cancel old motion by replacement/reset.
- Give Shell actions a single press lifecycle with pointer capture, drag cancellation, and activation on valid release.
- Apply visible but restrained motion to Lobby, Battle, and Settlement while preserving their authoritative layout and hit rectangles.
- Make reduced motion a first-class policy that renders the same semantic end states without travel, stagger, or transient impulse.
- Record provenance before any reference-derived raster is bound to a production semantic slot.
- Validate math, lifecycle, layout stability, input behavior, allocations, and live WebGL resting states.

**Non-Goals:**

- Changing gameplay, balance, progression, persistence, level data, or route order.
- Migrating to UGUI, importing the reference APK's Lua architecture, or adding DOTween/LeanTween.
- Reproducing reference art indiscriminately or shipping protected/unverifiable bundle contents.
- Adding continuous blur, general Spine infrastructure, particle-heavy chrome, or virtualized lists without an observed product need.

## Decisions

### 1. Extend the current pulse model into a small motion sampler

`RuntimeUiFeedbackPulse` will become the timing owner used by a new pure `RuntimeUiMotion` evaluator. The evaluator returns a `RuntimeUiMotionSample` containing scale, alpha, and vertical offset for semantic patterns: press, pop, fade-slide, and staggered reveal. Evaluation is allocation-free and depends only on a pulse, unscaled time, tokens, pattern, and optional stagger index.

This keeps the existing project's simplest end-to-end path and makes the motion math unit-testable. A third-party Tween package was considered, but rejected because no package is currently installed and the required behavior is four deterministic scalar curves with no scene-object ownership.

### 2. Tokens own amplitude as well as duration

The theme feedback block adds validated values for press scale, a separate short pop duration, inward pop scales, reveal offset, reveal duration, stagger interval, and reduced-motion default. Press and pop samples are constrained to `(0, 1]`: routine feedback briefly contracts toward `0.97`, strong feedback may contract farther, and no pattern may enlarge past its authoritative rectangle.

Pop timing is independent from the owning pulse lifetime. A status can remain visible for seconds, but its geometric impulse completes within the short pop token so a long status lifetime can never produce a slow rebound.

Hard-coded per-screen animation constants are not allowed. Invalid, non-finite, negative, or excessive values fail the existing theme validation path.

### 3. Visual geometry moves; authoritative hit geometry does not

Shared draw helpers will accept an optional motion sample and derive a transient visual rectangle around the component center. The transparent input target continues to use the original layout rectangle. Text/icon sub-layout is recalculated inside the visual rectangle, so the group remains optically centered during motion.

This preserves safe-area and pointer mapping requirements and prevents animated artwork from changing the gameplay or touch contract.

### 4. Shell uses one explicit press tracker

A small IMGUI press tracker will own the active control ID, pointer-down position, and drag-cancel flag. Activation occurs only when the primary pointer is released inside the same enabled control without crossing the movement threshold. Leaving the control, disabling the route, beginning navigation, or resetting the presenter cancels the owner.

Lobby cards/start and Settlement return/retry will migrate from duplicate `ContainsPointer`/`IsPointerPress` plus `GUI.Button` paths to this tracker. Battle's existing drag capture remains authoritative for board/tool drags; the first wave applies shared motion samples to Battle feedback and actions without replacing the established drag state machine.

### 5. Route-specific restraint

- Lobby: title and level cards fade/slide in with a capped stagger; selected-card and Start feedback use short pop/press samples.
- Battle: wave/status/resource feedback uses pop samples, while action press visuals use the shared press curve. No whole-board motion is introduced.
- Settlement: result surface reveals first, then outcome/metrics, then actions with a short capped stagger. Final rectangles are identical to the existing layout.

Only a bounded set of elements animate simultaneously. No perpetual UI shine or idle bounce is added.

### 6. Reduced motion collapses travel, stagger, and transient impulse

The theme exposes a reduced-motion policy consumed by the evaluator. When active, transition samples immediately resolve to scale 1, alpha 1, and zero offset; semantic pressed/selected/loading/error visuals and action availability remain unchanged. This provides a deterministic binding point for a later settings surface without adding that surface now.

### 7. Reference resources are provisional and replaceable by semantic slot

A reference-derived raster may enter runtime only if a manifest records source APK hash/path, extraction method, output format, pixel dimensions, alpha/color-space/import settings, semantic ArtSet slot, and `provisional` replacement status. The runtime references only the semantic slot, never an APK path or filename. Assets that remain inside protected UnityFS data blocks are not copied or guessed.

The current Sunny Orchard ArtSet remains authoritative until a candidate passes import, nine-slice, optical, and WebGL validation. Because no useful protected UI texture is currently decoded, the first implementation is expected to improve motion and interaction while retaining the existing art set.

## Risks / Trade-offs

- [IMGUI control IDs can change if draw order changes] → Keep pressable calls in stable route order, derive IDs from stable hints, and cover down/drag/up/cancel sequences with tests.
- [Animation can hide layout defects or make screenshots nondeterministic] → Validate both named motion checkpoints and the exact resting state; keep hit rectangles fixed.
- [Outward or slow rebound breaks component containment] → Reject scale above `1.0`, give pop its own short duration token, and validate long-lived status pulses against that independent deadline.
- [Route motion can continue after navigation] → Reset route-owned pulses and press ownership on initialize/disable/transition.
- [Reduced motion is not yet exposed in Settings] → Treat the theme policy and validation hook as the authoritative first layer; a later settings change may bind it without replacing the evaluator.
- [Reference assets may have legal, format, or technical uncertainty] → Require provenance and import validation; do not add protected bundle payloads to production.

## Migration Plan

1. Add motion tokens, pure evaluator, press tracker, and focused editor tests.
2. Extend shared drawing helpers so visuals can animate independently of input geometry.
3. Migrate Lobby and Settlement to the new press owner and route samples; delete their duplicate pointer helpers.
4. Apply shared motion to selected Battle feedback without changing its drag owner or simulation.
5. Update the theme asset and UI documentation, run aggregate editor smoke and WebGL build, then capture the canonical routes at start, midpoint, and rest.
6. If the result regresses input or readability, revert the scoped branch; there is no runtime fallback or parallel legacy motion path.

## Open Questions

- Which reference textures, if any, will pass extraction and provenance review after the bundle protection is solved?
- Should the later user-facing Settings surface bind reduced motion globally or per profile? This does not block the theme-level policy used by this change.
