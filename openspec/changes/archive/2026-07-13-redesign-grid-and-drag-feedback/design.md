## Context

The battlefield currently stores pots as free-form percentage coordinates and renders each pot as an isolated control over a looping road. Plant selection already exposes legal/illegal destinations, but pointer dragging has no shared visual drag session, native drag relies on the browser ghost, merging has no preview, and selling is wired through several UI and economy surfaces.

The change crosses configuration, state commands, economy rules, battlefield rendering, pointer/native drag input, CSS, specifications, and tests. It must remain dependency-free and preserve the existing deterministic game engine.

## Goals / Non-Goals

**Goals:**

- Make every initial and expandable pot occupy a canonical orthogonal cell in contiguous soil regions.
- Give native mouse drag and pointer/touch drag the same legible source, preview, hovered-target, validity, cancellation, placement, and merge feedback.
- Remove selling completely rather than merely hiding its controls.
- Double the runtime and displayed range of all four attacking fruit kinds.
- Preserve current battle, merge, weapon recovery on merge, and expansion mechanics unless explicitly changed.

**Non-Goals:**

- Replacing emoji artwork, changing zombie paths or wave balance, adding new fruits, or redesigning the HUD/build panel.
- Adding a free-placement system; all destinations remain predefined cells.
- Giving sunflower an attack range.

## Decisions

### Use grid coordinates as the source of truth

Pots and expansion candidates will gain integer `column` and `row` coordinates. Rendering percentages will be derived through shared grid helpers, so adjacency and visuals cannot drift apart. Keeping only redesigned percentage coordinates was considered, but it would preserve fragile distance-based adjacency and make contiguous regions difficult to validate.

### Render soil cells as a battlefield layer

The battlefield will render every available/expandable planting cell as a square soil tile below pots and plants. Neighbor-aware classes will visually join adjacent tiles into large orthogonal regions while preserving a button per active pot for accessibility and interaction. A single background image was rejected because it could not reflect expansion state or expose cell semantics.

### Keep transient drag state in React UI state

A shared drag-session value will contain the payload, pointer position, hovered pot, and movement status. It will be updated by nursery and battlefield pointer handlers and native drag events. Game state remains deterministic and only receives a command on a legal drop; animation state does not enter the reducer.

### Separate selection from active dragging

Selecting a plant will continue to show its attack range, but destination validity classes and the pointer-following preview will be driven by an active drag session. This prevents a click selection from looking like a drag and enables one stronger highlight for the current hovered cell.

### Use status-derived merge presentation

`getPlantDropStatus` remains the authority for whether a destination plants, moves, merges, cancels, or rejects. UI labels and classes will derive from its action and reason, including next-star preview and four-star rejection. Successful merge feedback will use the existing transient feedback system plus a short target animation keyed from the result.

### Delete the selling command path

The sell zone, plant-detail action, `sell-plant` command, `plant-sold` economy event, sell-value helper, and reducer handling will be removed. Keeping a hidden command was rejected because tests or future UI could still invoke behavior that the product explicitly forbids.

### Double base range in configuration

Pea and watermelon range become 44, banana 38, and durian 18; sunflower remains 0. Runtime targeting, details, selected range circles, and placement previews already consume computed stats, so one configuration source keeps them synchronized.

## Risks / Trade-offs

- [Large doubled circles can cover most of the board] → Keep the range layer clipped to the battlefield and verify that it stays below interactive targets.
- [Pointer capture can make `elementFromPoint` hover detection inconsistent] → Resolve the underlying target with `closest('[data-pot-id]')` on every move and test mouse plus touch-style pointer events.
- [Native HTML drag and pointer drag can both start on desktop] → Gate the custom pointer session behind the movement threshold and suppress duplicate click/drop completion.
- [Grid migration can affect expansion legality] → Centralize grid lookup and add invariant tests for unique cells and Manhattan adjacency.
- [Removing selling can strand a full nursery] → This is intentional product behavior; refresh remains blocked until plants are placed or merged.

## Migration Plan

1. Introduce grid constants/helpers and migrate initial pots plus expansion candidates.
2. Render grid soil regions without changing commands.
3. Add the shared drag presentation and merge feedback.
4. Remove all selling surfaces and command paths, then update tests/specs.
5. Apply doubled ranges and run unit, component, build, and browser interaction verification.

Rollback is a normal source revert because there is no persisted save migration or external API.

## Open Questions

None. The implementation will preserve the current road and overall battlefield aspect while changing planting areas into regular blocks, matching the requested direction without expanding into a full map redesign.
