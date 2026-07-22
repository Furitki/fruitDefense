## Context

The game presents the canonical `BattlefieldMapDefinition` through immediate-mode GUI in `FruitDefenseGame`, while the existing Dual-Grid authoring pipeline produces `TileBase` assets intended for Unity Tilemaps. The release Battle scene has no Tilemap and should not gain a second map representation merely to show terrain. The runtime integration therefore needs to consume the same generated TileSet and map semantics directly, remain aligned with `BattlefieldProjection`, and work in ordinary WebGL and portrait safe areas.

## Goals / Non-Goals

**Goals:**

- Show PixelGrass on every canonical plantable cell and StoneFloor on every monster route cell in all bundled maps.
- Resolve both visual layers from canonical cell roles with the established NW=1, NE=2, SE=4, SW=8 contract.
- Keep all terrain geometry on the projection's square tile grid and clipped to `GridRect`.
- Make release-scene asset wiring reproducible through project setup and verifiable through the required smoke entry.
- Preserve readable route, core, pots, plants, enemies, interaction feedback, and effects above the terrain.

**Non-Goals:**

- Changing map topology, plantability, route movement, balance, hit testing, snapshots, progression, rewards, or scene flow.
- Replacing the immediate-mode battle presenter with a runtime Unity Tilemap.
- Adding player-selectable terrain themes or different terrain art per level.
- Claiming Douyin or WeChat support from ordinary WebGL acceptance.

## Decisions

### Render the authored TileSet directly in the existing presenter

`FruitDefenseGame` will hold serialized references to the generated PixelGrass and StoneFloor `DualGridTileSet` assets plus the PixelGrass soil source texture. A small runtime utility will expose validated sprites for masks 0–15 and resolve battlefield masks for either plantable or route roles. The presenter will draw both sprite layers with IMGUI before core and entity layers.

This reuses the exact generated assets and keeps one battle presenter. Adding a hidden runtime Tilemap and camera was rejected because it would create a second projection, extra scene objects, and alignment/safe-area failure modes. Copying the masks into a separate runtime atlas was rejected because it would duplicate generated output and introduce another asset ownership path.

### Derive grass from plantable cells and stone from route cells

The grass layer treats only `Plantable` cells as occupied. The road layer treats `Route`, `Entry`, and `Exit` cells as occupied. Core, blocked, out-of-bounds, and roles belonging to the other layer remain empty for each respective resolver. Every visual vertex derives both masks from `BattlefieldMapDefinition`, with a vertical coordinate adapter that preserves the established north/south bit meanings in top-down GUI space.

This makes the pot-placement space visibly grassy and the monster route visibly stone without changing either role. Storing mask selections in level content was rejected because masks are derived data and would duplicate canonical topology.

### Use projection vertices and clip the half-cell overhang

For an `W × H` logical map the renderer visits `(W + 1) × (H + 1)` visual vertices. Each sprite is one projected tile in size and centered on its corresponding grid vertex, matching the authoring system's negative half-cell alignment. Drawing occurs inside a `GridRect` group so the natural half-tile perimeter overhang cannot cover the battle frame or controls.

The PixelGrass soil texture is sampled beneath both overlays at the same native-pixel-to-projected-tile scale. Grass draws first and StoneFloor draws second so the monster road remains legible at shared edges. The legacy procedural route fill is skipped when the route TileSet is valid; entry and exit markers, core, interactive cells, entities, effects, feedback, and controls continue to draw afterward from their existing projection rectangles.

### Keep asset binding explicit and reproducible

The release `Battle.unity` component will serialize references to both TileSets and the PixelGrass soil texture, which also ensures their dependency chains are included in builds. `ProjectSetup.Configure` will assign the same assets whenever it recreates the Battle scene. Missing or invalid bindings retain the existing flat-color board and procedural route as a defensive runtime fallback, but the required smoke validator will fail the release project until the configured scene and every grass/road mask sprite are valid.

## Risks / Trade-offs

- [Two sixteen-mask layers create extra IMGUI draw calls] → The board has at most 72 visual vertices per layer for the bundled 8-by-7 maps, textures are only 32×32, and the layers are static per GUI repaint; verify WebGL behavior with the normal acceptance path.
- [The source soil texture is larger than one tile] → Tile it with normalized coordinates at the profile's native 32-pixel-per-cell scale and rely on the existing imported texture rather than duplicating art.
- [Pixel art can blur at non-integer portrait scales] → Preserve point filtering on generated sprites and draw every mask from the single square `TileSize`; validate all supported portrait projections.
- [Terrain reduces plantable-cell readability] → Keep route/entity layers above terrain and retain subtle cell/expansion affordances, strengthening them only during actionable interaction states.
- [Scene regeneration can silently drop bindings] → Centralize the two asset paths in `ProjectSetup` and smoke-check both the current release scene and regenerated component contract.

## Migration Plan

1. Add the runtime role-mask/sprite/projection utility and layered terrain drawing stage.
2. Bind the existing PixelGrass and StoneFloor assets in `Battle.unity` and teach project setup to reproduce the binding.
3. Add editor smoke validation and run strict OpenSpec plus Unity smoke/build checks.
4. Roll back by removing the three serialized bindings and terrain draw calls; the pre-existing flat board and procedural route fallbacks remain intact.

## Open Questions

None for this integration. Per-level terrain themes remain a separate future capability.
