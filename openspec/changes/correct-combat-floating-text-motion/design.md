## Context

`CombatFloatingTextSdfOverlay` projects presentation-event map points through the same `BattlefieldProjection`, viewport transform, and battlefield shake offset used by live entities. Its upper-edge branch currently reverses the vertical direction to keep labels inside BattleStage; the collision catalog then chooses the first non-overlapping lane from a bounded set. The behavior is presentation-only and must not alter simulation events, map coordinates, BattleStage geometry, or input rectangles.

## Goals / Non-Goals

**Goals:**

- Make normal damage and defeat labels use one upward-facing placement direction at every vertical anchor.
- Increase the deterministic collision envelope from 24 candidates / 140 logical points to 33 candidates / 224 logical points.
- Preserve stage containment, viewport/safe-area projection, the 12 logical-point horizontal anchor tolerance, pooling, and deterministic first-fit collision selection.

**Non-Goals:**

- Changing combat events, target following, defeat aggregation/centroid behavior, map coordinates, battlefield shaking, BattleStage layout, font assets, ArtSets, or player input.

## Decisions

### Remove directional reversal rather than change coordinates

The overlay will always use the existing upward branch for motion, semantic defeat separation, and collision lanes. It will no longer infer a `towardBattlefieldInterior` direction from the upper edge. This leaves the shared projection untouched and removes the falling behavior at its source.

Allowing a second coordinate path or applying a per-role screen-space correction was rejected because live entities and feedback already share the same projection and offset.

### Expand in discrete deterministic lane tiers

The current 24 candidates end at 140 logical points. Three additional three-column tiers at 168, 196, and 224 logical points produce 33 ordered candidates. The overlay retains its current selection algorithm: it searches near candidates first and only chooses a farther tier when nearer candidates overlap.

An unbounded search was rejected because label preparation must retain its fixed capacity, predictable cost, and repeatable ordering.

### Preserve containment and horizontal anchoring

The existing BattleStage clamping and horizontal anchor tolerance remain in force. Disabling inward redirection does not introduce a second viewport, change safe-area transforms, or permit unbounded horizontal displacement.

## Risks / Trade-offs

- [Upper-edge labels can touch the Stage ceiling rather than reverse downward] → This is the requested direction policy; containment remains enforced by the existing clamp.
- [A larger envelope can place labels farther from their targets during an extreme burst] → The added tiers are selected only after nearer placements overlap, and the bound is fixed at 224 logical points.
- [Additional candidates add a small fixed search cost] → The pool remains capped at 12 labels and candidate iteration remains allocation-free and deterministic.

## Migration Plan

1. Replace the upper-edge directional branch with the fixed upward placement path.
2. Extend the candidate catalog and its focused smoke assertions.
3. Run focused floating-text validation and the aggregate editor smoke, then rebuild/capture the existing Battle acceptance surface if the environment is available.

No runtime data, save data, or compatibility migration is required. Reverting the scoped source and spec change is the rollback path.

## Open Questions

None.
