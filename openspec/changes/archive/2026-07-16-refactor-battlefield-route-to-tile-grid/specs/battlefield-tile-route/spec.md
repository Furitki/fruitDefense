## ADDED Requirements

### Requirement: Canonical battlefield cell semantics
The system SHALL assign exactly one semantic role to every in-bounds battlefield cell, SHALL distinguish plantable, route, blocked, entry, exit, and core semantics, and SHALL derive plantability and required cell collections from that canonical definition.

#### Scenario: Default orchard map is loaded
- **WHEN** the bundled `orchard-01` battlefield is created
- **THEN** it has an 8-by-7 grid containing 20 ordered route cells, 35 plantable cells, one core cell, no blocked cells, one entry at `(0,0)`, and one exit at `(1,6)`

#### Scenario: Initial flowerpots are resolved
- **WHEN** a new `orchard-01` battle initializes
- **THEN** its three semantic roadside groups place eight initial flowerpots only on plantable cells adjacent to the route area

#### Scenario: Cell role is queried
- **WHEN** simulation or presentation queries any coordinate inside the battlefield dimensions
- **THEN** it receives the same canonical role and does not infer the role from an independent route image or point list

### Requirement: Ordered four-directional route topology
The system SHALL define the enemy route as a unique ordered list of route cells whose consecutive cells are cardinal neighbors, whose first and last cells are the entry and exit, and whose exit is cardinally adjacent to the core.

#### Scenario: Default route order is inspected
- **WHEN** `orchard-01` route cells are enumerated
- **THEN** they run east from `(0,0)` through `(7,0)`, south through `(7,6)`, and west through `(1,6)` without a duplicate, diagonal step, gap, or branch

#### Scenario: Invalid route definition is validated
- **WHEN** a map contains a missing cell role, duplicate route cell, out-of-bounds cell, non-cardinal route step, mismatched endpoint, role overlap, non-adjacent exit and core, or non-plantable initial-pot cell
- **THEN** validation fails and identifies the invalid cell or route index

### Requirement: Derived per-cell route tiles
The system SHALL derive one oriented route tile descriptor for every ordered route cell from its previous and next connections, covering entry, exit, horizontal, vertical, and all four corner orientations.

#### Scenario: Internal straight and corner cells are resolved
- **WHEN** a route cell has two opposite ordered connections
- **THEN** it resolves to the matching horizontal or vertical tile descriptor
- **AND** when the ordered connections are perpendicular it resolves to the matching corner descriptor

#### Scenario: Route endpoints are resolved
- **WHEN** the first or last ordered route cell is resolved
- **THEN** it produces an entry or exit descriptor oriented by its sole in-grid route connection

#### Scenario: Route is rendered
- **WHEN** the battlefield draws its route
- **THEN** each descriptor is rendered wholly inside its own square tile rectangle and no line, texture, or image is stretched across multiple route cells

### Requirement: Cell-center route sampling
The system SHALL build route metrics from the ordered route-cell centers and SHALL use that single sampler for simulation movement and presentation positions.

#### Scenario: Segment boundary is sampled
- **WHEN** route progress equals a cumulative segment boundary
- **THEN** the sampled position equals the corresponding route cell center exactly

#### Scenario: Enemy crosses a corner
- **WHEN** an enemy advances through a corner segment boundary
- **THEN** its position remains continuous, reaches the corner cell center, and continues toward the next ordered cell center

#### Scenario: Legacy-equivalent traversal is measured
- **WHEN** deterministic `orchard-01` route metrics are created
- **THEN** the 19 center-to-center segments total 23 map units and a normal enemy's traversal duration remains within the existing deterministic tolerance

### Requirement: Square grid projection
The portrait presentation SHALL fit the complete battlefield as a centered grid of equal square tiles inside the usable board content rectangle and SHALL derive tile, route, core, entity, effect, and map-to-screen geometry from that projection.

#### Scenario: Grid is constrained by width
- **WHEN** the usable content is proportionally wider or taller than the 8-by-7 grid
- **THEN** tile size is selected from the limiting dimension, every tile remains square, and unused space is centered rather than stretching the grid

#### Scenario: Route entity is projected
- **WHEN** an enemy, projectile target, combat effect, or route marker is projected from map space
- **THEN** its screen position aligns with the same tile centers and route segments used by the per-cell route artwork

#### Scenario: Core is drawn
- **WHEN** the core cell is presented
- **THEN** its visual rectangle is derived from and contained by the core tile rather than using a fixed multi-cell legacy size

### Requirement: Decoupled flowerpot visual and interaction bounds
The presentation SHALL derive a full-tile flowerpot interaction rectangle and a centered flowerpot visual rectangle whose width and height are between 65% and 70% of the tile size.

#### Scenario: Flowerpot is drawn and selected
- **WHEN** an active flowerpot is rendered in a plantable cell
- **THEN** its art uses the visual rectangle, while click selection, drag origin, drop targeting, and expansion highlighting use the full-tile interaction rectangle from the same projection

#### Scenario: Adjacent flowerpots are dense
- **WHEN** flowerpots occupy horizontally or vertically adjacent plantable cells
- **THEN** their visual rectangles do not overlap, each visual remains centered in its own tile, and each full-tile interaction target remains independently addressable

#### Scenario: Visual ratio is validated
- **WHEN** any supported portrait projection is checked
- **THEN** the flowerpot visual is contained by its interaction rectangle and both dimensions remain within the inclusive 0.65-to-0.70 tile ratio

### Requirement: P0 map identity and gameplay preservation
The bundled battlefield SHALL expose the stable map identity `orchard-01` while preserving the existing fifteen-wave content, current numeric combat configuration, eight initial flowerpots, and current launch flow.

#### Scenario: Default battle launches
- **WHEN** the current lobby or standalone compatibility flow starts a battle
- **THEN** it uses the single bundled `orchard-01` definition without presenting a map picker or resolving a second playable map

#### Scenario: Snapshot identity is exported
- **WHEN** a battle snapshot is exported for the bundled battlefield
- **THEN** its map identity is taken from the active definition and equals `orchard-01`

#### Scenario: Content parity is checked
- **WHEN** deterministic acceptance compares the migrated build with the recorded P0 baseline
- **THEN** the fifteen ordered waves, plant/enemy/equipment numeric configuration, rewards, and eight-pot initial count are unchanged

### Requirement: Multi-viewport battlefield acceptance
The project SHALL validate tile topology and projection in editor smoke tests and SHALL inspect the built game on a real WebGL canvas at 360-by-800, 375-by-812, 402-by-874, and 430-by-932 portrait viewports, including representative non-zero top and bottom safe-area insets.

#### Scenario: Projection matrix is validated
- **WHEN** each required viewport and safe-area case is evaluated
- **THEN** every tile is square and contained, the grid is centered, route tiles connect without gaps or stretching, the core remains tile-local, and flowerpot visual and interaction bounds satisfy their ratios

#### Scenario: Dense WebGL battlefield is captured
- **WHEN** initial, active-wave, and dense-adjacent-pot states are inspected on the built WebGL canvas
- **THEN** evidence shows a continuous per-cell route, enemies centered on that route, individually legible pots, aligned interaction targets, and no clipping by the safe area or battle controls

#### Scenario: In-app acceptance surface is unavailable
- **WHEN** the in-app browser crashes or disconnects before canvas evidence is complete
- **THEN** the same WebGL artifact and viewport matrix are accepted through a stable external Chromium browser with the browser and viewport recorded in the evidence
