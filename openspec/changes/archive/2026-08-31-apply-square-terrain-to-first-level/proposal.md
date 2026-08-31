## Why

The approved grass-and-soil square-tile preview is still isolated in a trial scene, so the player cannot judge it in the real first-level battle. `orchard-01` needs to use the approved clean square-grid treatment through the normal release flow while later levels retain their current terrain presentation.

## What Changes

- Promote the approved normalized 64×64 grass and soil tiles into production terrain assets; review masters and provenance remain outside the release dependency graph.
- Add a dedicated production terrain palette for `orchard-01` and register it in the release Battle scene.
- Bind only the `orchard-01` theme to the new palette; `orchard-02` and `orchard-03` continue to use `palette.orchard.default`.
- Render the first-level canonical 8×7 map as base-only square cells: the 7×5 plantable interior is grass and the U-shaped route border is soil, with no landform or pair-edge overlays.
- Preserve gameplay cells, route topology, markers, simulation, persistence, interaction geometry, level order, and the `Bootstrap → Lobby → Battle → Settlement` flow.
- Validate the result in editor smoke tests and an ordinary portrait WebGL first-level capture. This change does not authorize Douyin or WeChat support.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `level-map-catalog`: Require `orchard-01` to resolve its own production square-terrain palette and base-only square visual layout without affecting later bundled levels or gameplay behavior.

## Impact

- Runtime catalog/theme construction in `Assets/Scripts/Content/BundledLevelCatalogFactory.cs`.
- Default first-level visual-map construction in `Assets/Scripts/Core/BattlefieldMap.cs` and the existing layered-map factory/compiler path.
- Production terrain sprites and palette assets under `Assets/Battlefield/Terrain/`.
- Release Battle-scene palette registration and project setup tooling.
- Focused first-level terrain validation, aggregate editor smoke, and ordinary WebGL visual evidence.
