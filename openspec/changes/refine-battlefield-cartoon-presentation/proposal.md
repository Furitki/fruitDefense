## Why

The release battlefield currently obscures its terrain art with a permanent planting-cell grid and renders enemies as small icons inside opaque square backplates. The release should use the third registered painter choice, `grass-on-soil.forward`, with a dirt monster route before producing the next WebGL package.

## What Changes

- Bind the release orchard terrain palette to the third registered painter choice, `terrain-brush.grass-on-soil.forward`, including its Runtime64 dirt base and composite grass TileSet.
- Present enemy-route cells as base-only dirt without changing their ordered route, movement, markers, or gameplay capabilities.
- Remove idle planting-cell outlines, fills, and markers; retain explicit interaction feedback while placing or dragging pots.
- Remove the opaque enemy backplates and increase the enemy sprite footprint while preserving health and status feedback.
- Validate the result through the aggregate Unity editor smoke, a normal WebGL build, and live portrait canvas evidence from an active wave.
- Keep battle rules, map topology, hit-test geometry, snapshots, persistence, and release scene flow unchanged.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `battlefield-dual-grid-terrain-presentation`: The release palette must use the third registered grass-on-soil painter choice, present monster routes as dirt, and remove the permanent cell grid.
- `portrait-game-interface`: Runtime enemies must render as enlarged readable sprites without opaque square backplates.

## Impact

The change affects the release terrain palette asset, bundled/recommended visual-cell composition, editor regeneration and validation, and battlefield drawing in `Assets/Scripts/FruitDefenseGame.cs`. It reuses existing project assets and adds no dependency, gameplay-topology change, persistence migration, or platform adapter.
