## Context

`FruitDefenseGame` currently uses `_selectedPlantId` for two unrelated concepts: the plant whose details are visible and the plant waiting for a later click to place, move, return, or merge. `HandlePotClick`, `HandlePlantClick`, and nursery-slot click handlers therefore interpret a normal inspection selection as a pending formation edit. Drag-and-drop already supports the same spatial actions with legal, merge, invalid, and return feedback.

This change follows `restructure-battlefield-map-and-tiles` so a selected plant's range can use the same board projection as routes, plants, and enemies.

## Goals / Non-Goals

**Goals:**

- Make normal plant clicks observational and incapable of changing plant location.
- Display plant information for clicked field and nursery plants.
- Display an accurate projected attack range for an inspected on-board attacker.
- Keep all plant placement, movement, return-to-nursery, and merging available through drag-and-drop.
- Keep explicit tool modes separate from passive plant inspection.
- Prove click-versus-drag behavior in deterministic smoke and WebGL interaction checks.

**Non-Goals:**

- Changing movement cooldown, merge eligibility, plant stats, or combat targeting.
- Removing drag-and-drop or its click-threshold handling.
- Redesigning weapon installation or flowerpot expansion tool behavior.
- Adding a new plant detail data model.

## Decisions

### Replace pending click movement with inspection state

Rename or reinterpret `_selectedPlantId` as an inspection-only identifier such as `_inspectedPlantId`. Clicking a plant without an active explicit tool sets this identifier, clears mutually exclusive passive tool selection as appropriate, and opens the information surface. It never records a pending spatial command.

Empty flowerpot and nursery-slot click handlers no longer call `MoveOrMergePlant` or `MoveToNursery` based on the inspected plant. Clicking another occupied plant switches inspection to that plant rather than attempting a merge. The information close action clears inspection.

A second hidden pending-move flag was rejected because it would preserve two click workflows and keep accidental relocation possible.

### Keep drag sessions as the only plant relocation authority

Pointer down can still create a candidate `DragSession`. Crossing the existing drag threshold activates the drag; release resolves the best overlapping target and calls the existing simulation validation/commit methods. Releasing without crossing the threshold performs inspection only.

This retains touch usability while making the difference between click and drag observable. Removing click handling entirely was rejected because players still need a direct way to inspect plants.

### Keep explicit tool modes independent

Weapon and flowerpot tool selections remain action modes. A weapon tool can continue to use its existing supported install paths, and a flowerpot tool can continue to expand legal cells. Passive plant inspection does not arm movement and must not be consumed as an input to those operations.

This change does not broaden the meaning of the user's request to unrelated tools. If weapon installation is later required to become drag-only, that should be a separate interaction change.

### Render range through the shared battlefield projection

For an inspected plant on an active flowerpot, calculate effective range from its plant stats and star multiplier. The range overlay is drawn beneath interactive entities but above the map background, centered on the same projected point as the plant. Projection helpers convert map-distance extents to screen-space radii; if the projection is non-uniform, the overlay uses the corresponding ellipse so it represents the actual simulation area.

Plants with an effective attack range of zero show their information surface and a clear no-attack-range value but no misleading battlefield circle. Nursery plants show information but no battlefield range because they do not have a field position.

### Make inspection lifecycle explicit

Inspection follows the plant ID when a drag moves it between flowerpots, so the range recenters at its new position. It disappears when the plant moves to the nursery, is replaced by refresh, or the information surface closes. Reset clears inspection and transient drag state.

## Risks / Trade-offs

- [Players accustomed to two-click movement think movement was removed] -> Keep concise drag guidance in the status surface and cover the first click on an empty pot with a non-destructive hint.
- [Short pointer movement is misclassified as drag] -> Preserve the tested activation threshold and make a below-threshold release inspect only.
- [Range rendering disagrees with simulation after map scaling] -> Use the same map metrics and projection as simulation entities and validate representative in-range/out-of-range points.
- [Explicit weapon mode conflicts with inspection] -> Define tool-mode precedence in the handler and add a regression test for weapon installation plus passive inspection.

## Migration Plan

1. Add regression coverage for the current drag placement, move, return, and merge paths.
2. Separate inspection identity from active tool and drag session state.
3. Remove click-triggered calls to plant relocation methods from flowerpot, occupied-plant, and nursery-slot handlers.
4. Add projected range rendering and information behavior for field, nursery, and zero-range plants.
5. Update guidance copy and WebGL acceptance to click a plant, click a destination without movement, then drag and verify movement.

Rollback restores the click relocation branches; no game-state or persisted-data migration is involved.

## Open Questions

None. Weapon and flowerpot tool semantics intentionally remain outside this change.
