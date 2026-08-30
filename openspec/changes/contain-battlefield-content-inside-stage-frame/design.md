## Context

Battle uses immediate-mode GUI with one `BattlefieldProjection` for world drawing and hit testing. The release gameplay-stage nine-slice declares a 20px interaction-safe inset at 2 pixels per logical point, while its visible rail opens at the component-owned 8pt boundary. `BattlefieldProjection` keeps only a 2pt terrain padding and does not own a renderer-wide clip. `DrawBoard` draws the gameplay-stage frame before pots, plants, enemies, projectiles, combat effects, and board drop cues; `DrawDragGhost` is later still. The existing projection is correct, but late pixels can cover the protected rail or escape the stage. Treating the 10pt interaction-safe inset as a presentation clip also creates a visible 2pt seam and is therefore not valid.

The board also supports deliberate board-to-nursery drag feedback. The dashed connector must remain a cross-region overlay, so containment cannot be implemented by masking the entire Battle page or changing target geometry.

## Goals / Non-Goals

**Goals:**

- Hard-clip battlefield-owned world pixels to the gameplay-stage opening without changing their authoritative design coordinates or leaving an inset seam.
- Keep the gameplay-stage frame above world pixels and board-target drag feedback in final composition.
- Preserve cross-region connector visibility, existing target selection, and the same draw/hit projection.
- Add deterministic coverage for the renderer architecture, supported portrait projection, and 1280×720 fractional PC scaling.

**Non-Goals:**

- Full Canvas/uGUI migration, a render-texture pipeline, or a second battlefield camera. The existing IMGUI/GPU path will own the equivalent rectangular stage-opening mask.
- Moving grid cells, shrinking hit rectangles, changing drag legality, or changing simulation coordinates.
- Removing the board-to-nursery connector or clipping free cross-region drag feedback to the board.
- Changing the gameplay-stage raster, ArtSet metadata, or approved visual direction.

## Decisions

### Use one stage-opening mask around battlefield-owned drawing

`DrawBoard` will open a nested IMGUI group clip around the complete stage. A shared rectangular mask applies the gameplay-stage component's explicit 8pt opening inset and contains terrain, cells, expansion visuals, pots/plants, enemies, projectiles, combat effects, and flash inside that opening. The mask boundary is separate from the ArtSet interaction-safe inset and is not inferred from transparent pixels. Invisible board hit targets remain in the unchanged outer `BattleStage` clip, so visual masking does not shrink gameplay geometry. Each group pair uses an inner group shifted by the negative mask position to restore the existing absolute design-coordinate origin, and `try/finally` always restores the clip stack.

The square-tile `GridRect` remains centered inside the wider stage and therefore leaves symmetric top/bottom aspect-ratio gutters. The terrain renderer will paint the palette's base surface across the full mask before drawing the unchanged square grid. The grid, tile size, cell coordinates, and hit geometry remain untouched; only the previously transparent gutters receive the same terrain backing.

The custom single-quad nine-slice path uses `Graphics.DrawTexture`, so board-target frames also receive the same mask as an explicit screen-pixel shader rectangle. Ordinary IMGUI content remains under the nested group mask; the shader rectangle closes the one renderer path that does not inherit IMGUI clipping reliably in WebGL. Together these two paths are the stage-opening mask; no parallel Canvas hierarchy is introduced.

Alternative considered: subtract `BattleStage.position` from every world and hit rectangle. Rejected because it duplicates projection logic across many renderers and makes draw/hit drift more likely.

Alternative considered: migrating Battle to a Canvas `RectMask2D` hierarchy. Rejected because the same mask contract can be applied directly to the current IMGUI/GPU renderers without splitting Battle across two UI stacks.

### Make the gameplay-stage frame the final stage occluder

The frame draw will move out of the middle of `DrawBoard` and run after the drag overlay but before blocking modals and screen-corner decoration. The safe-inset content clip keeps owned pixels out of the rail, while the final frame preserves the intended border composition even when an entity, rotated plant, combat reaction, board target frame, or board drag ghost reaches an edge.

The stage viewport is the outer hard clip; the explicit 8pt opening is the visible-content clip, and the frame's opaque rail is the final visual occluder. Simulation and hit rectangles remain unchanged, and terrain may continue beneath the transparent-center frame up to its visible inner opening.

### Clip board-target drag content without clipping the cross-region connector

`DrawDragGhost` will keep the connector outside the stage clip. When the resolved target is a board pot, plant, or expansion cell, the target frame, cue, and ghost will be drawn inside the same component-owned stage opening clip. Nursery/free cross-region feedback remains design-space contained under the existing contract. The final stage-frame pass then owns the protected rail.

Alternative considered: clamp the drag preview center inward. Rejected because it would move feedback away from the pointer and could imply a different drop target.

### Validate the contract at geometry, source architecture, and final pixels

Focused Editor coverage will assert that the stage clip uses the authoritative `BattleStage`, that the frame is drawn after the drag overlay, that board-target feedback enters the clip while the connector remains outside, and that all required viewport projections remain finite. Real WebGL evidence will include an edge board target at 402×874 full/inset and 1280×720; the protected rail/outside-stage pixels must remain uncontaminated except for the explicitly permitted connector path.

## Risks / Trade-offs

- **[Incorrect clip offset moves input or drawing]** → Use the documented nested-group coordinate origins, keep absolute rectangles unchanged in the restored inner design group, and cover draw/hit identity in focused smoke.
- **[A thrown draw call corrupts later IMGUI clipping]** → Pair every clip with `try/finally` and the matching nested `GUI.EndGroup()` calls.
- **[The connector is accidentally masked]** → Draw it before opening the board-target clip and assert the method ordering in focused coverage.
- **[A future renderer bypasses containment]** → Keep all battlefield-owned drawing behind the single `DrawBoard` clip and add a structural source gate plus real-canvas rail analysis.
- **[The mask prevents bleed but leaves a visible seam]** → Use the component-owned 8pt opening rather than the broader 10pt interaction-safe inset, assert its exact reference rect, and require edge-state WebGL evidence to touch the opening band.
- **[The square grid still leaves top/bottom gaps]** → Paint the same base terrain behind the grid through the full mask and verify both gutter bands from final WebGL pixels; never stretch tiles to fill the stage.
- **[Frame occlusion hides a few edge pixels]** → This is intentional ownership of the visible rail; gameplay positions and hit regions remain unchanged.

## Migration Plan

1. Add the scoped IMGUI clip and move the stage frame to the final stage-occlusion pass.
2. Clip only board-target portions of drag feedback while preserving the connector overlay.
3. Run focused Editor validation, aggregate project smoke, ordinary WebGL build, and edge-state captures.
4. Roll back by reverting this presentation-only change; no serialized or gameplay data migration is required.

## Open Questions

None. The current connector remains the only explicit cross-region exception.
