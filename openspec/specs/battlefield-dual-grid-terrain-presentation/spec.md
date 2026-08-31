# battlefield-dual-grid-terrain-presentation Specification

## Purpose
Define deterministic runtime binding, mask resolution, projection, interaction preservation, and release validation for layered Dual-Grid terrain on canonical battlefield maps.
## Requirements
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

The required project smoke SHALL validate visual-cell coverage, contour identity, material and ordered-pair bindings, base/landform/edge sprite availability, square and organic mask topology, projection, clipping, connected-style constraints, and all bundled levels. Ordinary WebGL acceptance SHALL inspect square isolated cells and strips, both ordered pair directions, edge enabled/disabled comparisons, diagonal masks, organic coexistence, controls, and release flow on a real portrait canvas.

#### Scenario: Project smoke runs
- **WHEN** `FruitDefense.Editor.ProjectSetup.SmokeValidate` executes
- **THEN** every bundled map and the layered authoring sample have valid base, contour, landform, pair-edge, mask, binding, component, and geometry evidence

#### Scenario: Ordinary WebGL build is accepted
- **WHEN** the normal WebGL build and portrait acceptance run against the release flow
- **THEN** the Battle scene shows seamless square gameplay terrain plus any reviewed organic regions below readable gameplay content without changing `Bootstrap → Lobby → Battle → Settlement`

### Requirement: Cell-aligned square terrain coexists with Dual-Grid terrain
The battlefield terrain renderer SHALL present a base-only visual cell as one opaque, cell-aligned square using its bound base texture. Dual-Grid landforms SHALL continue to use their existing transparent mask, contour, and pair-edge layers, and the system MUST NOT generate sixteen mask variants for a pure-square surface.

#### Scenario: Render a pure-square cell
- **WHEN** a visual cell has a registered base surface and empty landform, contour, and pair-edge identifiers
- **THEN** the renderer draws exactly the opaque base layer for that gameplay cell

#### Scenario: Render separated square and Dual-Grid regions
- **WHEN** a valid map contains spatially separated base-only and Dual-Grid regions
- **THEN** each region uses its own existing rendering path without changing draw order or gameplay geometry

#### Scenario: Render two unlike square surfaces
- **WHEN** base-only grass and base-only soil cells share an edge
- **THEN** the renderer presents an intentional hard boundary aligned to the gameplay grid

### Requirement: Isolated square-terrain trial evidence
The project SHALL provide an isolated grass-and-soil square-terrain trial whose assets and palette do not replace release content. Its base textures MUST be opaque, use Repeat wrapping, and follow the approved clean cartoon palette with broad hand-painted variation rather than fine noise. The user-approved soft inset cell frame SHALL appear once per gameplay cell as intentional grid language; accidental discontinuities, double borders, and unrelated seam colors remain invalid.

#### Scenario: Generate the comparison board
- **WHEN** the square-terrain trial generator runs
- **THEN** it produces a comparison board at battlefield cell scale showing repeated pure grass and pure soil plus a spatially separated Dual-Grid reference

#### Scenario: Validate trial texture imports
- **WHEN** automated trial-art validation inspects the grass and soil base textures
- **THEN** each texture is opaque, imported with Repeat wrapping, matches its recorded approved-source crop, and passes the configured regular-frame and interior-noise thresholds

#### Scenario: Render the approved game-grid composition
- **WHEN** the square-terrain trial generator builds its primary review region
- **THEN** it presents an exact `8 × 7` cell grid with a `7 × 5` grass region touching the left edge and soil occupying the full top row, right column, and bottom row at real Battle cell scale

#### Scenario: Protect release content
- **WHEN** the trial assets and comparison board are generated
- **THEN** the release palette, playable map catalog, release scenes, and gameplay rules remain unchanged
