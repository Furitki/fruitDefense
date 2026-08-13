## Context

Bundled battle maps explicitly use soil as their base surface, grass landforms for plantable areas, and previously used stone-road landforms for the monster route. The paint panel's third choice is the forward `terrain-brush.grass-on-soil` composite: its Runtime64 soil base is the dirt layer and its Runtime64 composite TileSet is the grass layer. `FruitDefenseGame` then adds a permanent per-cell planting overlay and two opaque rectangles beneath every enemy sprite.

The change crosses serialized palette data, bundled and recommended visual-cell composition, editor regeneration/validation, and immediate-mode runtime drawing. It must remain presentation-only and work on the existing safe-area-aware 402 by 874 portrait composition in WebGL.

## Goals / Non-Goals

**Goals:**

- Make the paint panel's third grass-on-soil choice the authoritative release binding for bundled battlefield terrain.
- Present monster-route cells as the same brush's base-only dirt without changing their gameplay route.
- Keep the battlefield visually continuous at rest and show cell feedback only during an actual pot placement/drag interaction.
- Increase the visible enemy art footprint and remove its opaque square backplate while retaining health, frozen, burning, slow, and hit feedback.
- Keep editor regeneration, release-scene validation, WebGL build, and live portrait evidence aligned with the same assets.

**Non-Goals:**

- Changing map cells, route sampling, combat balance, enemy collision/hit tests, pot hit rectangles, safe-area layout, persistence, or platform adapters.
- Regenerating or modifying the registered grass-on-soil source textures and masks.
- Redesigning the core, pots, plants, combat effects, or bottom control surface.

## Decisions

### Reuse the exact third registered brush outputs

The release soil binding will use `CompositeBrushes/GrassSoil/Runtime64/Mask-00.png`, and the square grass landform binding will use `GrassSoilCompositeTileSet.asset`. Together these are the exact assets selected by the paint panel's third choice, `terrain-brush.grass-on-soil.forward`. Creating another copy or a release-only texture override was rejected because either would diverge from the registered authoring brush.

Other registered palette bindings remain intact. The editor palette builder resolves the release square grass binding directly to the registered composite TileSet, ensuring `Fruit Defense/Configure Project` and smoke fixtures reproduce the same third-choice result.

### Make the authored monster route base-only dirt

The bundled map factory and the recommended-presentation authoring action will leave route cells on the soil base with no landform or contour. Enemy traversability, ordered route samples, endpoints, markers, hit rectangles, and deterministic simulation inputs remain unchanged. Keeping the old stone-road overlay was rejected because the clarified requirement explicitly calls for dirt.

### Remove only the idle planting overlay

`DrawPlantingCells` will return before drawing when no pot tool or pot drag is active. During those interactions it will continue to derive rectangles from `ExpansionRect`, apply legality colors, and expose the existing button/drop logic in `DrawExpansionCandidates`. This removes the permanent grid without creating a second geometry path or weakening touch interaction.

### Separate enemy art size from health feedback size

Enemy art will use a larger projection-scaled footprint than the previous 48-point rectangle, while the health bar will stay compact and centered against that footprint. The two opaque background rectangles will be deleted. Frozen and burning effects remain anchored to the enlarged sprite; slow and hit state remain visible through sprite tint/status effects rather than a backplate. Enemy position continues to come from the same route sample and projection helpers.

### Accept from the real release WebGL canvas

The aggregate editor smoke will verify exact palette bindings, base-only dirt route semantics, and runtime geometry contracts. The normal WebGL build will then run through the existing portrait acceptance route, with an active-wave screenshot manually inspected for the Runtime64 grass/soil, dirt monster route, absence of idle grid lines and enemy backplates, enlarged enemy sprites, readable status/health feedback, safe-area containment, and unchanged controls.

## Risks / Trade-offs

- [The dirt route can be less visually explicit than the old stone-road overlay] → Preserve route markers, ordered route motion, and gameplay topology, then confirm readability in a live active-wave screenshot.
- [Larger enemies can overlap route edges or nearby entities] → Use a modest projection-scaled increase and confirm dense active-wave evidence at the reference viewport.
- [Removing idle cell markers can make expansion discoverability weaker] → Preserve the pot tool, pot-drag, legal/illegal target highlights, and expansion-pot icons whenever placement is active.
- [Editor regeneration could silently restore old bindings] → Resolve the registered third-choice assets in project setup and assert their exact paths in the aggregate terrain smoke.

## Migration Plan

Update the editor palette builder and its exact-binding validation, serialize the same bindings into the release palette asset, change bundled/recommended route visuals to base-only dirt, adjust runtime overlays and enemy drawing, then run editor smoke, WebGL build, and live visual acceptance. Rollback is a normal source revert of those scoped presentation files; no saved game or content migration is required.

## Open Questions

None. The paint-panel ordering identifies the requested brush as `terrain-brush.grass-on-soil.forward`, and the route is explicitly base-only dirt.
