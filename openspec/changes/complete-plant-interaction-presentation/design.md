## Context

The battle runtime is one immediate-mode presenter backed by deterministic `GameSimulation`. Plantable-cell geometry, pot art, hit targets, range projection, plant dragging, and atlas rendering already converge in `FruitDefenseGame`; plant drop legality and mutation live in `GameSimulation`. The release atlases already contain five base plants, three full-size equipment/evolution visuals, and authored projectile/effect sprites, but equipped plants currently render the base sprite plus an 11-point badge and the pea projectile also draws a procedural line.

This change crosses presentation and one formation rule. It must retain the canonical battlefield projection, fixed-step combat, snapshot shape, content catalog, and ordinary WebGL portrait baseline.

## Goals / Non-Goals

**Goals:**

- Keep plantable pot locations discoverable with a quiet idle grid and stronger active expansion feedback.
- Make installed equipment visibly produce the corresponding existing atlas-backed evolution form on every plant rendering surface.
- Keep transient attacks resource-backed and remove the procedural pea trail.
- Produce a crisp range overlay without changing its simulation-derived geometry.
- Treat an occupied incompatible plant destination as a direct swap while retaining compatible merge behavior.
- Add deterministic smoke coverage for the new decision table and presentation resource mapping.

**Non-Goals:**

- New plant, equipment, projectile, balance, economy, persistence, scene-flow, or platform behavior.
- New art generation, a new asset-loading framework, or per-plant/per-equipment combination assets.
- Changing pot, nursery, drag, or safe-area hit geometry.

## Decisions

### Reuse the existing plantable-cell projection for the preview grid

`DrawPlantingCells` will always draw a one-point low-alpha outline for every plantable cell. It will add fill, markers, and stronger legal/illegal colors only while the pot tool or pot drag is active. This restores spatial preview without introducing a second grid or obscuring the reviewed terrain.

Alternative considered: author a separate grid texture. Rejected because it would duplicate projection geometry and could drift from hit rectangles at different map sizes.

### Resolve equipped plant art through one sprite-selection function

All board, nursery, and drag-ghost plant rendering will use a single resolver. Unequipped plants resolve to their plant atlas cell; equipped plants resolve to the full-size existing equipment/evolution atlas cell. The tiny board-only equipment badge will be removed because it is an obsolete parallel representation.

Alternative considered: composite a large equipment sprite over the base plant. Rejected because the current resources are already complete silhouette-level forms and stacking them would reduce clarity at pot scale.

### Keep projectile and impact presentation atlas-only

The pea projectile will render its existing projectile sprite without the procedural `DrawLine` trail. The now-unused line helper will be removed. Other projectile, impact, status, and muzzle effects continue through `combat-vfx-atlas` and the presentation-event buffer.

### Generate one 1024-pixel immutable range texture

The range overlay will retain the same `Projection.MapRect` center and radius while increasing the generated texture from 128 to 1024 pixels. Bilinear filtering and immutable upload remain; the larger source avoids magnification blur at the 1206-pixel-wide portrait acceptance target without adding an asset or per-frame allocation.

Alternative considered: draw a vector circle. Rejected because it conflicts with the resource/raster presentation direction and adds a second procedural rendering path.

### Add `Swap` to the plant drop decision table

Dropping onto an empty destination remains `Plant` or `Move`; dropping onto a compatible same-kind, same-star destination remains `Merge`; dropping onto any other occupied plant destination becomes `Swap`. Swap exchanges the complete location pair (`PotId`, `NurseryIndex`) without changing plant identity, star, weapon, or inventory. During an active wave, both moved plants must satisfy movement cooldown, and each plant that originated on the board receives the existing board-move cooldown after the swap. Attack cooldown and per-skill transient runtimes reset for both moved plants, matching ordinary relocation.

One shared swap helper will serve pot and nursery destinations so board-to-board, board-to-nursery, nursery-to-board, and nursery-to-nursery behavior cannot diverge.

## Risks / Trade-offs

- [A persistent grid can compete with terrain art] → Use outline-only low-alpha idle rendering and reserve fills/markers for active expansion.
- [Equipment evolution visuals do not encode every base plant identity] → Keep authoritative plant identity and labels unchanged; use the existing equipment form as the deliberately dominant evolved silhouette.
- [A 1024-pixel runtime texture uses about 4 MiB uncompressed] → Allocate it once, make it immutable, and destroy it with the presenter.
- [Swap can indirectly move a cooldown-blocked target] → Require both plants to pass the active-wave cooldown gate before reporting `Swap` as legal.
- [Dragging onto a merge-compatible plant could become ambiguous] → Merge retains priority; swap applies only when the merge predicate fails.
