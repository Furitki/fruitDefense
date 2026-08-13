## MODIFIED Requirements

### Requirement: Release battle terrain binding
Each resolved level theme SHALL reference a valid stable terrain-palette identity whose registered runtime palette supplies every required base material, surface-plus-contour landform TileSet, and requested material-pair-plus-contour edge TileSet. One ordered edge binding MAY satisfy the reverse order through complemented mask lookup. Project configuration and the release Battle scene SHALL reproduce the palette registry without hard-coding one level's active composition or silently substituting a missing contour or unrelated pair.

#### Scenario: Release Battle scene is inspected
- **WHEN** the configured `Assets/Scenes/Battle.unity` presenter and palette registry are loaded
- **THEN** every bundled theme resolves its palette and every referenced base, contour-specific landform, and contour-specific edge asset resolves to renderable sprites

#### Scenario: Project configuration recreates the Battle scene
- **WHEN** `Fruit Defense/Configure Project` recreates release scenes
- **THEN** the recreated battle presenter receives the same validated layered terrain palette registry without manual Inspector repair

#### Scenario: Contour pair is unavailable
- **WHEN** a map requests an edge style for a foreground/background/contour combination whose exact or reverse order is not registered
- **THEN** validation fails with the foreground, background, contour, and style identities and does not use another contour or unrelated material pair

#### Scenario: Base rendering has no contour dependency
- **WHEN** a palette contains contour TileSets with different native pixel sizes or registration order
- **THEN** every required opaque base uses its own configured texture and stable cell-space UV scale instead of a landform full-tile fallback

### Requirement: Canonical map-derived terrain masks
The runtime presenter SHALL draw each cell's required base directly, SHALL derive landform and pair-edge masks from equality of canonical landform surface plus contour style using the established NW=`1`, NE=`2`, SE=`4`, SW=`8` contract, and SHALL NOT infer any visual layer from plantability, routes, collision, or markers. Migrated bundled maps MUST explicitly select square contours for their gameplay-aligned landforms unless deliberately changed under visual acceptance.

#### Scenario: Base-only cell is rendered
- **WHEN** a visual cell declares soil as its base and no landform
- **THEN** the presenter draws the soil base and no landform, contour, or edge contribution for that cell

#### Scenario: Square interior landform is rendered
- **WHEN** all four logical cells around a visual vertex declare grass with square contour
- **THEN** the square grass renderer selects mask `15` regardless of gameplay capability or valid base material

#### Scenario: Contour styles are separated
- **WHEN** a disconnected organic grass component and square grass component share one map
- **THEN** each renderer resolves masks only from cells matching both its surface and contour identity

#### Scenario: Pair edge mask is resolved
- **WHEN** a square grass-on-soil painted edge binding is rendered
- **THEN** mask membership matches the exact grass foreground, soil background, square contour, and painted edge style without accepting a cell that differs in any key member

#### Scenario: Visual and gameplay layers differ
- **WHEN** a non-plantable cell declares square grass or an enemy-route cell declares no landform
- **THEN** terrain follows the authored visual cell while placement and movement continue to follow compiled gameplay topology

#### Scenario: Map perimeter is resolved
- **WHEN** a visual vertex touches cells outside the canonical map bounds
- **THEN** out-of-bounds corners are empty for landform and edge resolution and no lookup reads a nonexistent visual cell

### Requirement: Multi-level and release validation
The required project smoke SHALL validate visual-cell coverage, contour identity, material and ordered-pair bindings, base/landform/edge sprite availability, square and organic mask topology, projection, clipping, connected-style constraints, and all bundled levels. Ordinary WebGL acceptance SHALL inspect square isolated cells and strips, both ordered pair directions, edge enabled/disabled comparisons, diagonal masks, organic coexistence, controls, and release flow on a real portrait canvas.

#### Scenario: Project smoke runs
- **WHEN** `FruitDefense.Editor.ProjectSetup.SmokeValidate` executes
- **THEN** every bundled map and the layered authoring sample have valid base, contour, landform, pair-edge, mask, binding, component, and geometry evidence

#### Scenario: Ordinary WebGL build is accepted
- **WHEN** the normal WebGL build and portrait acceptance run against the release flow
- **THEN** the Battle scene shows seamless square gameplay terrain plus any reviewed organic regions below readable gameplay content without changing `Bootstrap → Lobby → Battle → Settlement`
