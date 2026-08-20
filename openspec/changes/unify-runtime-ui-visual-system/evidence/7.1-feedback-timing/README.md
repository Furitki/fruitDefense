# Task 7.1 shared feedback timing evidence

## Runtime contract

The release theme remains the sole duration authority:

| Semantic feedback | Release value | Runtime consumption |
|---|---:|---|
| Focus | 0.08 s | Bootstrap retry, Lobby cards/start, Settlement retry/return, Battle drag-hover status |
| Press | 0.08 s | Bootstrap, Lobby, and Settlement actions retain a short pressed visual after the click frame |
| Selection | 0.12 s | Lobby level-card and Battle tool selection receive a short emphasis while the selected identity and marker remain persistent |
| Transition | 0.18 s | Bootstrap startup and route actions receive visual emphasis; navigation and commands are not delayed |
| Status | 2.6 s | Bootstrap/Shell error emphasis and Battle transient status, return warning, and nursery confirmation |

`RuntimeUiFeedbackPulse` is a readonly value type containing only explicit start/deadline
floats. Callers supply unscaled time; the type does not read Unity time or another clock,
allocate, schedule work, or invoke commands.

Shared components receive only an `emphasized` value. Disabled/loading semantic state,
non-color indicators, and the original hit rectangle remain authoritative. A button click
starts its feedback pulse and invokes its existing command in the same IMGUI event.

## Boundary and behavior coverage

`RuntimeUiFeedbackTimingSmoke` verifies:

- `Time.timeScale = 0`, active start, active pre-deadline, exclusive deadline, zero duration,
  non-finite input, negative duration, and overflow;
- value-type/no-reference-field ownership and absence of an internal wall-clock read;
- Loading and Disabled precedence over transient pointer feedback;
- persistent selected identity and semantic indicator ownership;
- same-frame Bootstrap retry, Lobby start, and Settlement retry/return source order;
- unchanged Lobby, Settlement, and Battle hit geometry;
- consumption of all five release-theme durations and removal of Battle's duplicated UI
  feedback deadlines without touching world/VFX/gameplay timing.

## Unity validation

All runs used Unity `6000.3.19f1` in batch mode and returned code 0:

| Validation | Marker | Log |
|---|---|---|
| Direct feedback timing | `RUNTIME_UI_FEEDBACK_TIMING_OK` | `Logs/runtime-ui-feedback-timing-direct-final.log` |
| Battle presentation boundary | `FRUIT_DEFENSE_PRESENTATION_BOUNDARY_OK` | `Logs/runtime-ui-feedback-presentation.log` |
| Battle session | `Fruit Defense battle session host validation passed.` | `Logs/runtime-ui-feedback-session.log` |
| Deterministic simulation | `Fruit Defense deterministic simulation validation passed.` | `Logs/runtime-ui-feedback-deterministic.log` |
| Aggregate editor smoke | `FRUIT_DEFENSE_SMOKE_OK` | `Logs/runtime-ui-feedback-aggregate.log` |
| P0 release gate | `FRUIT_DEFENSE_P0_RELEASE_GATE_OK` | `Logs/runtime-ui-feedback-p0-final.log` |

Strict OpenSpec validation and scoped diff checks also pass. This task changes presentation
timing only; it makes no gameplay, simulation, persistence, route-legality, platform-support,
art-set, or design-overview claim.
