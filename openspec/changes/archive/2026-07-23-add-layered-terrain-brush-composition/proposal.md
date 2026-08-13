## Why

The current terrain presentation flattens each battlefield cell into one exclusive visual surface over one palette-wide base, so authors cannot reuse a terrain as a transparent landform, paint it as a pure base, or explicitly choose ordered `A on B` and `B on A` combinations. Terrain-pair edge art is also baked into one mandatory result instead of remaining an optional, independently authored visual refinement.

## What Changes

- **BREAKING (map presentation source):** replace the single row-major visual-surface value with one required base-surface layer plus one optional terrain/landform layer, while preserving the existing gameplay topology, routes, markers, simulation, and projection.
- Add three authoring operations: pure-base painting, foreground terrain over the existing base, and an ordered pair brush that paints both the selected base and foreground (`A on B` and `B on A` are distinct choices).
- Add an optional second-pass edge binding for each ordered foreground/background pair. Turning it off keeps the reusable transparent terrain silhouette; turning it on adds pair-specific AI-authored edge art without runtime raster processing.
- Keep pure opaque base tiles, reusable transparent 16-mask terrain tiles, and optional transparent 16-mask pair-edge tiles as distinct assets with shared size, mask, alignment, and seam contracts.
- Validate unavailable ordered pairs explicitly and never silently substitute the reverse pair or another palette edge.
- Migrate bundled maps to the new two-layer presentation source with player-visible terrain parity unless a deliberately selected new presentation sample is under visual acceptance.
- Add editor smoke coverage and real portrait WebGL visual acceptance for base-only regions, both pair orders, edge-on/edge-off comparison, seams, diagonal masks, clipping, and unchanged controls.

## Capabilities

### New Capabilities

- `layered-terrain-brush-authoring`: Defines the base-plus-optional-landform source model, ordered pair brush operations, asset roles, and optional pair-edge authoring contract.

### Modified Capabilities

- `battlefield-dual-grid-terrain-presentation`: Changes terrain rendering from one global base plus exclusive surface overlays to deterministic base, landform, and optional ordered-pair edge composition.
- `dual-grid-tilemap-authoring`: Adds base, terrain, and ordered-pair brush modes while preserving the existing sixteen-mask Dual-Grid topology and generated-output ownership rules.
- `level-map-catalog`: Validates layered terrain coverage, palette materials, ordered edge-pair references, and migration of every bundled level.

## Impact

- Layered battlefield presentation DTOs, compilation, validation, and presentation-only identity under `Assets/Scripts/Core` and `Assets/Scripts/Content`.
- Terrain palette/material/pair-edge assets and mask resolution under `Assets/Scripts/Tilemaps`.
- Battle rendering and theme/palette resolution in `Assets/Scripts/FruitDefenseGame.cs` without changing gameplay state, save/snapshot authority, combat balance, or `Bootstrap → Lobby → Battle → Settlement`.
- Unity editor authoring UI, manual paint workflow, project setup, release scene bindings, generated or AI-authored raster terrain assets, and focused smoke/visual acceptance evidence.
- The completed `separate-battlefield-visual-topology-and-markers` change remains the dependency that separates presentation data from gameplay topology; this change extends only its presentation source and does not make art authoritative for placement or movement.
