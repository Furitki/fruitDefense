## Context

The battlefield is rendered in percentage coordinates inside a fixed aspect-ratio element. Its road ends at the bottom exit while the orchard core is a separate circular object in the center. Planting cells are grid-backed but styled as rounded rectangles. The shell switches to two columns only by width, so a short landscape viewport remains a very tall single column. Plant dragging has both native drag and pointer input, while weapon dragging has native drag only. The plant ghost is centered over the pointer and contains a label, obscuring the target.

## Goals / Non-Goals

**Goals:**

- Make the zombie path terminate visibly at a square orchard destination and make all soil cells strict squares.
- Keep the complete game usable at desktop, portrait-mobile, and short-landscape dimensions.
- Support weapon installation by mouse, touch, and pen drag with one consistent target-resolution path.
- Keep the dragged item visible without covering the destination or pointer.
- Preserve click-to-select/install and native desktop drag as fallbacks.

**Non-Goals:**

- Rebalance waves, weapons, plants, or rewards.
- Add free-form placement or change grid expansion rules.
- Redesign the art style beyond geometry and drag feedback.
- Add dependencies or persistence changes.

## Decisions

### Treat the last path point as the orchard destination

The square orchard destination will be positioned from an exported endpoint derived from the last `PATH_POINTS` entry. The old bottom exit marker will be removed and the central core will move to that endpoint. This keeps rendering and gameplay path completion aligned. A decorative-only move was rejected because it could drift from runtime path changes.

### Make soil geometry square in battlefield coordinates

Soil and pot hit targets will use `aspect-ratio: 1` with a width derived from the grid column step; rounded corners are removed from soil cells and reduced on interactive pots. Grid coordinates and expansion logic remain unchanged. Rebuilding the grid model is unnecessary because it already supplies canonical integer coordinates.

### Add an explicit short-landscape shell mode

The existing desktop and portrait flow remains, but `orientation: landscape` plus a low-height media query will use a compact two-column shell. The board is capped from available dynamic viewport height, while the build panel scrolls independently. Height-aware sizing uses `dvh` with a safe fallback. Scaling the whole application with CSS transforms was rejected because it would blur content and distort pointer coordinates.

### Generalize transient drag state by payload kind

App-level transient drag state will support either a plant payload or a weapon payload. Pointer handlers on weapon cards use a movement threshold and pointer capture, then resolve targets with `document.elementFromPoint(...).closest('[data-plant-id]')`. Only the existing reducer command mutates inventory. Native `dataTransfer` drag remains supported for desktop interoperability.

### Put outcome feedback on the destination

The floating ghost is offset diagonally from the pointer, clamped/flipped near viewport edges, compact, and partially transparent. Destination pots receive the strongest valid/invalid outline and a nearby label. The ghost never participates in hit-testing. This avoids relying on a large floating label to explain the target.

## Risks / Trade-offs

- [Moving the core can overlap the bottom gate or soil] → Remove the exit gate, reserve spacing around the last path point, and verify at three viewports.
- [Native drag and pointer drag can both start on desktop] → Apply the same movement-threshold and click-suppression pattern already used for plants.
- [Pointer capture can hide the element under the pointer] → Resolve the target through `elementFromPoint`, with the ghost set to `pointer-events: none`.
- [Short landscape can make controls too dense] → Cap the board by height, keep controls compact, and make only the build panel independently scrollable.
- [Initial weapon inventory is empty] → Component tests use an explicit inventory fixture and browser verification uses a controlled development state or click fallback after a milestone.

## Migration Plan

1. Align map endpoint/core rendering and square geometry.
2. Add short-landscape responsive rules.
3. Add weapon pointer drag state and routing.
4. Refine shared drag ghost and destination feedback.
5. Run unit, type, build, and browser viewport verification.

Rollback is a normal source revert; there is no persisted data migration.

## Open Questions

None. The user confirmed that the current central orchard should move to the bottom road endpoint and become square.
