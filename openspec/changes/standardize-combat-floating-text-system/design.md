## Context

Combat feedback already crosses a one-way semantic boundary and is merged, prioritized, rate-limited, and capped by the presentation buffer. Floating text nevertheless remains a single mutable `GUIStyle` rendered at 11 pixels, with per-profile raw colors, a fixed linear 18-pixel rise, and lifetimes advanced by scaled battle elapsed time. `PresentationFeedback.Text` also constructs a new string every repaint and each admitted record allocates a new reference object.

The release renderer is immediate-mode GUI, the portrait reference is 402 by 874, and the packaged Noto Sans SC font is already resident for every release route. The design must remain presentation-only, keep pause semantics, survive mowing-style event floods, and avoid a speculative package or renderer migration.

## Goals / Non-Goals

**Goals:**

- Make ordinary, heavy, periodic, resource, control, and defeat feedback readable by typography, outline, motion, and copy as well as color.
- Give hit text a short contact punch, controlled rebound, hold, rise, and fade whose strength follows semantic importance.
- Preserve a real-time reading floor at 2x while using logic ticks for deterministic merge eligibility.
- Bound visible density independently from event volume and preserve higher-priority information.
- Remove recurring floating-record allocation and per-repaint text construction from the hot path.
- Define an evidence-based gate for any later move to a project-authored glyph atlas.

**Non-Goals:**

- Changing damage, statuses, rewards, targeting, snapshots, checksums, RNG, hit-test geometry, or battle outcomes.
- Adding critical-hit gameplay, arbitrary content-authored colors/fonts, TextMeshPro, a new package, or a second release font.
- Claiming Douyin or WeChat compatibility from ordinary WebGL acceptance.

## Decisions

### 1. Profiles choose semantic text roles, not raw typography

`CombatFeedbackProfile` will select a finite `CombatFloatingTextRole` instead of carrying a floating-text boolean, raw color, and lifetime. A presentation-owned style catalog will define the complete role treatment:

| Role | Reference size | Treatment | Nominal lifetime | Rebound |
| --- | ---: | --- | ---: | --- |
| Normal damage | 16 | warm-ivory fill, dark cocoa outline | 0.62 s | light |
| Heavy damage | 20 | coral fill, dark wine outline | 0.82 s | strong |
| Periodic damage | 15 | amber fill, dark cocoa outline | 0.50 s | restrained |
| Resource | 17 | sunlight gold, dark olive outline, `+` copy | 0.86 s | medium |
| Control | 17 | ice blue, navy outline, state copy | 0.78 s | medium |
| Defeat | 18 | gold-white, dark cocoa outline, defeat copy | 0.92 s | strong |

The catalog owns final color, outline width, size, lifetime, rise distance, entry scale, peak scale, hold boundary, fade boundary, priority class, and ordinary-density classification. Ability content cannot invent a seventh visual language by supplying a raw color.

Alternative considered: keep raw color and size on every feedback profile. Rejected because future content would drift into skill-specific color coding and bypass the shared contrast and density contract.

### 2. Motion uses a three-phase analytic envelope

Motion is sampled from normalized progress without animation assets or per-record curves:

1. `0–0.12`: opacity and scale establish quickly from the role start scale to its peak.
2. `0.12–0.38`: an ease-out rebound returns from peak to 1.0 while vertical motion remains small enough to read the contact point.
3. `0.38–1.0`: the label rises to its role distance and fades during the final 35 percent.

Heavy and defeat roles peak near 1.22–1.24; ordinary damage peaks near 1.08; periodic damage peaks near 1.02. Scale is applied around the label center through the existing GUI matrix and restored in `finally` so neither later drawing nor hit testing inherits the transform.

Alternative considered: one spring equation for every role. Rejected because a visually pleasing under-damped spring creates long tails and makes exact readability and density budgets harder to validate.

### 3. Merge time and readable lifetime use different clocks

Merge eligibility continues to use authoritative logic ticks. Local lifetime advances from unscaled display delta only while battle presentation is not paused. At 2x, local time advances at 1.25x, so the real lifetime is 80 percent of 1x rather than 50 percent. This keeps feedback responsive to speed selection without dropping below the reading floor.

The local clock remains excluded from battle state and persistence. A player may see a slightly different animation phase at a different render cadence, but combat results remain identical.

Alternative considered: retain scaled battle elapsed. Rejected because it halves real reading time at 2x. Alternative considered: identical real lifetime at every speed. Rejected because doubled event frequency would leave too many overlapping low-priority labels even after merging.

### 4. Admission is bounded before drawing

The floating channel will keep a small total capacity for exceptional feedback and a stricter ordinary-damage budget. The initial reference budgets are 12 total active records and 8 ordinary/periodic damage records. One feedback profile may retain at most three distinct target labels from the same logic tick, preventing a single area hit from filling the channel. When ordinary capacity is full, a new ordinary record merges, replaces the lowest/oldest eligible ordinary record, or is discarded. Resource, control, heavy, and defeat roles may displace ordinary feedback but never exceed total capacity.

At most three records from one profile and logic tick receive deterministic lanes 0, 1, and 2. Numeric labels use a narrower box than resource copy. Lanes provide meaningful vertical separation, and feedback near the upper route edge unfolds downward into the battlefield instead of collapsing against the clamp; they do not move the gameplay position or maintain a second battlefield projection. Fatal damage does not emit a redundant numeric label, same-tick defeats collapse to the compact `击败×N` copy using glyphs already present in the release UI inventory, and the terminal result uses a fourth semantic display band outside the three damage lanes so it cannot collide with damage still fading from the prior beat.

### 5. The floating channel reuses records and caches display strings

Expired and evicted `PresentationFeedback` instances return to a bounded pool. Record initialization resets all fields explicitly. Display text is updated only when admitted or merged, not on each repaint. Repeated numeric forms use a bounded presentation text cache; defeat and control copy are stable constants. Dense aggregation displays the accumulated magnitude, while count may influence pulse strength without appending an ever-growing suffix.

The renderer creates fill and outline `GUIStyle` objects once per semantic role from the packaged font. A repaint performs no style construction and does not call numeric formatting for unchanged records.

### 6. Do not add a project-authored glyph atlas without performance or final-raster evidence

The imported Noto Sans SC font already uses Unity's glyph texture path and is required by the rest of the release UI. The finite floating-text glyph inventory is requested for every role size during presentation-style initialization so WebGL does not discover a missing Chinese glyph or incur first-use texture work during combat. With only 6–8 ordinary records intended to remain readable, a second bitmap glyph atlas would duplicate glyph ownership, require custom kerning/metrics and multi-language maintenance, and would not solve event routing or record churn.

This change therefore adds no PNG or SpriteAtlas. A later dedicated atlas/batched-mesh renderer is authorized if real WebGL profiling at the accepted visible-record budget shows either:

- floating-text rendering alone exceeds 0.5 ms in the 95th percentile on the reference acceptance environment; or
- steady-state floating-text rendering allocates more than 1 KB per second after warm-up.

It is also authorized when final 402-by-874 WebGL raster review records a repeatable quality failure that this renderer cannot meet, including discontinuous outline scaling or unreliable release glyph coverage. Visual quality is a first-class migration gate rather than an exception to the performance rule.

If that gate is crossed, the atlas must be deterministically generated from the packaged font and a finite reviewed glyph inventory; generated images or manually painted digits are not acceptable sources.

### 7. Acceptance measures the final raster, not nominal tokens

Editor validation will cover role completeness, minimum sizes, outline contrast pairs, motion envelopes, pause/1x/2x clock behavior, merge/admission order, pool reuse, and dense-event allocation. Real WebGL acceptance will exercise grass, brown route, impact effects, 1x, 2x, and a synthetic dense feedback state at 402 by 874. HUD and hit-test geometry must remain unchanged.

## Risks / Trade-offs

- [Risk] Four-pass outline plus fill increases label draw work → Mitigation: strict readable-count budgets, cached styles, and the explicit atlas escalation gate.
- [Risk] Real-time lifetime is render-cadence dependent → Mitigation: it remains local presentation state; merge, gameplay, snapshots, and checksums stay logic-driven.
- [Risk] Larger labels collide near clustered enemies → Mitigation: early merge, three damage lanes, a dedicated terminal-result band, priority replacement, and a 12-record hard cap.
- [Risk] Cached numeric strings grow without bound → Mitigation: use a fixed-capacity cache and fall back without retaining additional keys.
- [Risk] New role copy is missing from the release font → Mitigation: add the finite strings to glyph-coverage validation before WebGL build.

## Migration Plan

1. Add the finite role/style catalog and analytic motion sampler.
2. Migrate bundled feedback profiles from raw floating-text fields to roles and remove the obsolete fields.
3. Convert the floating channel to bounded pooling, cached copy, role-aware admission, and visual lanes.
4. Replace the single 11-pixel label draw with cached outlined role styles and protected matrix scaling.
5. Update clock tests and host advancement, then add dense allocation/admission validation.
6. Synchronize the stable design principles, run aggregate Editor validation, build ordinary WebGL, and capture real 1x/2x/dense evidence.

Rollback is a source revert of this presentation-only change; no content save, snapshot, or player-data migration is required.

## Open Questions

None for the first implementation. Atlas adoption remains a measured future decision rather than an open requirement.
