## MODIFIED Requirements

### Requirement: Release battle terrain binding
Each resolved level theme SHALL reference a valid stable terrain-palette identity whose registered runtime palette supplies every required base material, transparent landform TileSet, and requested ordered pair-edge TileSet. Project configuration and the release Battle scene SHALL reproduce the palette registry without hard-coding one level's active composition or silently substituting a missing pair.

#### Scenario: Release Battle scene is inspected
- **WHEN** the configured `Assets/Scenes/Battle.unity` presenter and palette registry are loaded
- **THEN** every bundled theme resolves its palette and every referenced base, landform, and edge-style asset resolves to renderable sprites

#### Scenario: Project configuration recreates the Battle scene
- **WHEN** `Fruit Defense/Configure Project` recreates release scenes
- **THEN** the recreated battle presenter receives the same validated layered terrain palette registry without manual Inspector repair

#### Scenario: Ordered pair is unavailable
- **WHEN** a map requests an edge style for a foreground/background direction that its resolved palette does not register
- **THEN** validation fails with the foreground, background, and style identities and does not use the reverse pair

### Requirement: Canonical map-derived terrain masks
The runtime presenter SHALL draw each cell's required base directly, SHALL derive landform and pair-edge masks from the canonical optional landform layout using the established NW=`1`, NE=`2`, SE=`4`, SW=`8` contract, and SHALL NOT infer any visual layer from plantability, routes, collision, or markers. Migrated bundled maps MUST use soil bases with their existing grass and stone-road landforms and no pair edge unless deliberately changed under visual acceptance.

#### Scenario: Base-only cell is rendered
- **WHEN** a visual cell declares soil as its base and no landform
- **THEN** the presenter draws the soil base and no landform or edge contribution for that cell

#### Scenario: Interior landform is rendered
- **WHEN** all four logical cells around a visual vertex declare grass as their landform
- **THEN** the grass landform renderer selects mask `15` regardless of gameplay capability or the valid base material

#### Scenario: Ordered edge is rendered
- **WHEN** a connected grass landform over water selects a valid refined shoreline style
- **THEN** the grass mask and the exact `grass on water` edge mask share topology while the reverse `water on grass` edge remains unused

#### Scenario: Visual and gameplay layers differ
- **WHEN** a non-plantable cell declares a grass landform or an enemy-route cell declares no landform
- **THEN** terrain follows the authored visual cell while placement and movement continue to follow compiled gameplay topology

#### Scenario: Map perimeter is resolved
- **WHEN** a visual vertex touches cells outside the canonical map bounds
- **THEN** out-of-bounds corners are empty for landform and edge resolution and no lookup reads a nonexistent visual cell

### Requirement: Projection-aligned layered rendering
The presenter SHALL draw cell-aligned opaque bases first, transparent Dual-Grid landforms second, optional ordered pair edges third, and gameplay presentation above them. It SHALL center landform and edge candidates on shared projection vertices, clip their half-cell overhangs to `BattlefieldProjection.GridRect`, and keep base, landform, and edge native dimensions and sampling compatible.

#### Scenario: Terrain is rendered at a portrait viewport
- **WHEN** the active map is projected at any required portrait viewport or supported safe-area inset
- **THEN** every base cell remains aligned, every landform and edge sprite remains square and vertex-aligned, and no terrain covers the battle frame or controls

#### Scenario: Scene layers are drawn
- **WHEN** a battle frame contains terrain, pair edges, core, pots, entities, effects, and interaction feedback
- **THEN** bases draw below landforms, edges draw above their landforms, and all gameplay-readable content remains above terrain

### Requirement: Interaction and simulation preservation
The layered terrain integration SHALL NOT change canonical gameplay cells, route sampling, battle simulation, persistence, gameplay map identity, or hit-test geometry, and SHALL retain readable plantable and expansion affordances above every visual composition.

#### Scenario: Player interacts with a layered terrain cell
- **WHEN** the player selects, drags, drops, or expands on a cell whose base, landform, or edge differs from its gameplay capability
- **THEN** the same projected hit rectangle and legality rules apply and feedback remains visible above terrain

#### Scenario: Only terrain composition changes
- **WHEN** two valid maps differ only in base surfaces, landforms, edge styles, or palette assets
- **THEN** their gameplay fingerprints, snapshots, deterministic simulation checksums, and outcomes remain equal

### Requirement: Multi-level and release validation
The required project smoke SHALL validate visual-cell coverage, material and ordered-pair references, base/landform/edge sprite availability, mask topology, projection, clipping, and all bundled levels. Ordinary WebGL acceptance SHALL inspect base-only cells, both ordered pair directions, edge enabled/disabled comparisons, diagonal masks, controls, and release flow on a real portrait canvas.

#### Scenario: Project smoke runs
- **WHEN** `FruitDefense.Editor.ProjectSetup.SmokeValidate` executes
- **THEN** every bundled map and the layered authoring sample have valid base, landform, pair-edge, mask, binding, and geometry evidence

#### Scenario: Ordinary WebGL build is accepted
- **WHEN** the normal WebGL build and portrait acceptance run against the release flow
- **THEN** the Battle scene shows seamless layered terrain below readable gameplay content without changing `Bootstrap → Lobby → Battle → Settlement`

