## ADDED Requirements

### Requirement: Release battle terrain binding

The release Battle scene SHALL bind valid generated PixelGrass and StoneFloor Dual-Grid TileSets plus the PixelGrass opaque soil texture to the battle presenter, and project configuration SHALL reproduce the same bindings when the scene is regenerated.

#### Scenario: Release Battle scene is inspected
- **WHEN** the configured `Assets/Scenes/Battle.unity` presenter is loaded
- **THEN** it references the generated PixelGrass and StoneFloor TileSets plus PixelGrass soil texture and every required grass and road mask resolves to a renderable sprite

#### Scenario: Project configuration recreates the Battle scene
- **WHEN** `Fruit Defense/Configure Project` recreates release scenes
- **THEN** the recreated battle presenter receives the same terrain assets without manual Inspector repair

### Requirement: Canonical map-derived terrain masks

The runtime presenter SHALL treat plantable cells as the occupied grass layer, SHALL treat route, entry, and exit cells as the occupied stone-road layer, SHALL treat core, blocked, out-of-bounds, and other-layer roles as empty for each respective resolver, and SHALL derive both visual layers through the existing NW=`1`, NE=`2`, SE=`4`, SW=`8` mask contract without storing per-map mask choices.

#### Scenario: Interior plantable vertex is grass
- **WHEN** all four logical cells around a battlefield visual vertex are plantable
- **THEN** the grass renderer selects mask `15` and the stone-road renderer selects mask `0`

#### Scenario: Interior monster route is stone
- **WHEN** all four logical cells around a battlefield visual vertex belong to the ordered monster route
- **THEN** the stone-road renderer selects mask `15` and the grass renderer selects mask `0`

#### Scenario: Route borders plantable grass
- **WHEN** plantable and route cells meet around a visual vertex
- **THEN** the grass mask contains only plantable corners, the stone-road mask contains only route/entry/exit corners, and the soil base remains available through uncovered pixels

#### Scenario: Map perimeter is resolved
- **WHEN** a visual vertex touches cells outside the canonical map bounds
- **THEN** the out-of-bounds corners are empty and no terrain mask lookup reads a nonexistent map cell

### Requirement: Projection-aligned layered rendering

The presenter SHALL tile the soil base beneath grass and stone-road overlays, SHALL draw `(GridWidth + 1) × (GridHeight + 1)` one-tile-sized candidates per overlay centered on shared projection vertices, SHALL clip both half-cell Dual-Grid overhangs to `BattlefieldProjection.GridRect`, and SHALL replace the procedural route fill with the stone-road overlay while retaining entry and exit markers.

#### Scenario: Terrain is rendered at a portrait viewport
- **WHEN** the active map is projected at any required portrait viewport or supported safe-area inset
- **THEN** every grass and stone-road sprite remains square, shares the projection tile size, aligns with logical tile corners, and cannot cover the battle frame or controls

#### Scenario: Scene layers are drawn
- **WHEN** a battle frame contains terrain, route, core, pots, plants, zombies, projectiles, effects, and interaction feedback
- **THEN** soil draws first, grass draws second, stone road draws third, and core, route markers, gameplay entities, effects, and feedback draw above them

### Requirement: Interaction and simulation preservation

The terrain integration SHALL NOT change canonical cells, route sampling, battle simulation, persistence, or hit-test geometry, and SHALL retain readable plantable and expansion affordances above the textured terrain.

#### Scenario: Player interacts with a textured plantable cell
- **WHEN** the player selects, drags, drops, or expands on a plantable cell
- **THEN** the same projected hit rectangle and legality rules apply and the relevant visual feedback remains visible above the terrain

#### Scenario: Battle state is saved or restored
- **WHEN** a snapshot is created or restored while the terrain effect is enabled
- **THEN** snapshot content and deterministic simulation outcomes are unchanged because terrain remains presentation-only

### Requirement: Multi-level and release validation

The required project smoke SHALL validate terrain asset binding, mask sprite availability, map-to-mask resolution, projection geometry, clipping, and all bundled level maps, and the ordinary WebGL acceptance path SHALL exercise the integrated Battle scene.

#### Scenario: Project smoke runs
- **WHEN** `FruitDefense.Editor.ProjectSetup.SmokeValidate` executes
- **THEN** every bundled map produces valid grass and stone-road masks for all visual vertices and the release Battle scene has all three configured terrain assets

#### Scenario: Ordinary WebGL build is accepted
- **WHEN** the normal WebGL build and portrait acceptance run against the release flow
- **THEN** the actual Battle scene shows grass under pot-placement cells and stone under the monster route below readable gameplay content without changing the `Bootstrap → Lobby → Battle → Settlement` order
