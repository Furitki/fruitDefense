## Context

Combat floating text's collision resolver runs during every presentation sync. Even after its candidate catalog was tightened, candidate re-selection can visibly move a label whenever nearby active records change.

## Goals / Non-Goals

**Goals:**

- Admit up to 9999 total and ordinary floating-text records.
- Remove collision-based placement re-selection.
- Keep BattleStage containment, projection, and the presentation-only boundary intact.

**Non-Goals:**

- Do not change gameplay simulation, persistence, event ordering, target-follow semantics, or BattleStage geometry.
- Do not add a new runtime configuration path, fallback capacity, or adaptive candidate range.

## Decisions

- Use fixed constants of 9999 for both total and ordinary admission. The ordinary sub-cap must match the total cap; leaving it at 8 would silently negate the requested pool increase for ordinary damage.
- Keep each record's authored visual-lane offset and semantic offset as its only placement offsets. Remove the candidate table, overlap accumulator, and candidate-selection branch rather than leaving a dormant avoidance setting.
- Resize fixed overlay arrays from the same total-capacity constant, including the glyph-command bound. This preserves allocation-free steady-state rendering. An adaptive collection was rejected because it would add runtime allocation and a second capacity policy.
- Index assigned slots by event sequence and retain a compact active-slot list. Synchronization and release therefore scale with active labels, not the 9999-slot backing capacity, while the fixed pool remains fully preallocated.
- Update editor and WebGL acceptance from the old fixed-12 contract to validate the 9999 capacity while retaining dense-fixture admission, authored-lane placement, deterministic re-sync, and the existing WebGL performance evidence scope. The 12-record fixture does not require zero overlap.
- Replace the former entry/hold/late-fade motion sequence with a lifetime-driven fast-start rise and linear fade. The upward offset is evaluated from whole-lifetime progress, so it starts while target following is still active rather than waiting for detachment.

## Risks / Trade-offs

- [Larger startup memory and worst-case immediate-mode draw work] → The requested 9999 fixed pool preallocates its bounded arrays; focused smoke, aggregate smoke, and WebGL acceptance will verify it does not break normal 12-record fixtures.
- [Dense labels may overlap] → This is intentional: target proximity and trajectory stability take priority over collision avoidance.
- [Existing fixed-capacity assertions become stale] → Update each capacity assertion in the focused and WebGL acceptance paths in the same change.
