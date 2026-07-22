## Why

The current battlefield projects the enemy route as a few large line segments while every grid cell is treated as plantable, so route visuals and gameplay geometry can drift or distort across portrait aspect ratios. The oversized flowerpot presentation also consumes the board because its visual bounds are coupled to its full-cell interaction bounds.

## What Changes

- **BREAKING** Replace the default battlefield's independent polyline route representation with an ordered, four-directionally continuous sequence of route cells inside one canonical tile grid.
- Give every battlefield cell an explicit semantic role, including route, plantable, blocked, entry, exit, and core semantics, with construction-time and smoke validation for invalid combinations.
- Derive one route tile shape per route cell from its previous and next connections, covering straight, corner, entry, and exit tiles without stretched multi-cell route artwork.
- Make route rendering, route sampling, enemy movement, and route acceptance consume the same ordered cell-center path.
- Keep projected tiles square and centered inside the usable portrait battlefield for multiple viewport sizes and safe-area insets.
- Separate each flowerpot's visual rectangle from its full-cell interaction rectangle; render the pot at approximately 65% to 70% of the tile size while retaining the tile-sized click and drag target.
- Preserve the single P0 map identity `orchard-01`, the existing fifteen waves, current combat values, and current battle flow while exposing a stable map identity boundary for later `LevelId` binding.
- Extend editor and real WebGL acceptance to cover route topology, per-cell tile selection, pot density, multiple portrait aspect ratios, and safe-area containment.

## Capabilities

### New Capabilities

- `battlefield-tile-route`: Canonical per-cell battlefield semantics, ordered route-cell topology, per-cell route tile selection, cell-center movement, square projection, decoupled flowerpot bounds, and multi-viewport acceptance for `orchard-01`.

### Modified Capabilities

None. The repository has no promoted baseline specification for battlefield route behavior.

## Impact

- Core map, topology, projection, and route sampling types under `Assets/Scripts/Core`.
- Battlefield drawing, hit testing, drag targets, enemies, projectiles, effects, and safe-area layout in `Assets/Scripts/FruitDefenseGame.cs`.
- Geometry and topology coverage in `Assets/Editor/ProjectSetup.cs`, plus real WebGL canvas acceptance evidence.
- Internal callers that consume route nodes, route geometry, or assume every grid cell is plantable must migrate to the ordered route-cell contract.
- No wave, enemy, plant, reward, persistence, platform-adapter, or multi-level selection behavior changes are included.
