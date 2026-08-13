## Context

The terrain laboratory currently carries a `refinedEdge` boolean through its Scene session even though the accepted content path already provides exact directed refined TileSets. The UI therefore offers a choice that authors should no longer make. Its four preset cards use one material thumbnail as the complete button image, so pair cards omit the background, contour, and edge assets that define their visible result.

The Scene cell indicator has a separate responsiveness problem. `LayeredTerrainPaintSession` resolves and draws a cell whenever `duringSceneGui` runs, but it neither requests ordinary mouse-move events nor tracks the last resolved cell. It repaints after mutation, while panel entry and window exit return without clearing the previous hover. The outline can consequently lag, jump only after another editor repaint, or remain on an old cell.

## Goals / Non-Goals

**Goals:**

- Make refined directed edges the only landform-bearing authoring result in the terrain laboratory.
- Make each preset card representative of the real active material, contour, direction, and refined edge assets.
- Keep Scene feedback to a responsive cell outline and label that follow mouse movement without a textured ghost.
- Preserve gesture Undo, panel input isolation, contour selection, target validation, and runtime terrain output.

**Non-Goals:**

- Removing the low-level landform TileSets that supply foreground fill beneath the refined overlay.
- Breaking the serialized edge-logical layer or legacy low-level APIs that can still read older unrefined content.
- Rendering a temporary textured brush into the Scene, interpolating skipped drag cells, or changing Scene camera controls.
- Changing gameplay, persistence, release scenes, player-visible UI, WebGL safe areas, or platform behavior.

## Decisions

### Make the editor session refined-only while keeping low-level compatibility

Remove the session's public refinement state and all edge-choice controls. Landform-bearing validation and mutation always request `refinedEdge: true`; pure-base brushes continue to clear landform and edge state. A missing exact directed refinement disables the affected brush with the existing actionable pair/direction reason and never falls back to the bare contour or the reverse pair.

The lower-level `LayeredTerrainTilemap` boolean APIs remain temporarily compatible so existing scenes and focused serialization tests can still be inspected or migrated. Ordinary laboratory authoring, acceptance setup, and new editor fixtures use only refined pair writes. Removing the serialized compatibility path would be a separate data migration and is not required to simplify the tool.

### Compose preset cards directly from real sprites

Expose read-only active preview sources from `LayeredTerrainTilemap`: the selected material's base tile plus the active contour's landform and exact directed edge TileSets. The editor draws the Sprite sub-rects directly with their real UVs and layer order.

Pure cards fill the preview with their real base Sprite. Pair cards draw the real background base and a four-quadrant island assembled from the same single-corner masks already used by the Dual-Grid TileSet gallery, first for landform and then for refined edge. This shows material contact, direction, contour, and refinement without generating persistent preview assets or maintaining a second renderer. Asset or contour changes are reflected on the next GUI repaint.

A saved generic thumbnail remains only as a fallback for invalid developer configuration; it is not the successful pair preview.

### Treat hover as explicit transient state

While a paint session is active, record each participating Scene view's original `wantsMouseMove` value and enable it. Resolve pointer-to-cell on Scene callbacks, cache the current cell and center, and request a repaint of only that Scene view when the cached cell changes. Repaint events draw the cached outline and label.

Clear the cached hover when the pointer enters the reserved panel rectangle, leaves the Scene window, the target/tool changes, painting stops, play mode begins, or the session is disposed. Restore every recorded `wantsMouseMove` value during teardown. This avoids global repaint traffic and prevents the editor setting from leaking after the tool closes.

No textured Scene ghost is introduced. The outline geometry continues to use the target Grid's real cell center and size, so draw and hit resolution share the same world-to-cell calculation.

### Keep validation editor-only and proportional

Focused smoke covers refined-only mutation, missing-direction refusal, real preview-source resolution, representative mask availability, hover-state change/clear behavior, gesture Undo, and lifecycle teardown. Visual evidence confirms that edge mode controls are absent and pair cards show composed transitions. Aggregate editor smoke and strict OpenSpec validation remain required. A new WebGL build or safe-area capture is unnecessary because no player-visible surface changes.

## Risks / Trade-offs

- [A pair card is representative rather than a prediction of surrounding map cells] → Use real active assets and a stable four-quadrant island, and label it as the brush preset rather than a Scene result preview.
- [A refined TileSet is missing for one direction or contour] → Disable that exact pair card with a specific reason; never display a misleading fallback.
- [Repeated mouse movement can cause excessive editor repaints] → Cache the cell and repaint only the Scene view whose resolved cell changed.
- [Changing `wantsMouseMove` can affect other editor tools] → Store the previous value per Scene view and restore it on every stop/close/reload path.
- [Legacy scenes may still contain unrefined serialized regions] → Preserve read compatibility, migrate accepted laboratory content explicitly, and ensure all new laboratory writes are refined.

## Migration Plan

1. Add read-only active preview-source access and the editor composite-card renderer.
2. Remove refinement mode state from the laboratory session and route all landform writes through exact refinement.
3. Add cached hover state, mouse-move opt-in, targeted repaint, and teardown restoration.
4. Update the acceptance board and editor tests to represent refined-only authoring, then capture the embedded laboratory evidence.

Rollback restores the edge toggle and generic preset thumbnails. The serialized terrain model remains readable in either direction.

## Open Questions

None. The user explicitly excluded textured Scene brush previews; the Scene surface retains only the responsive cell outline.
