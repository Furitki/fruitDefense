## ADDED Requirements

### Requirement: Release registered grass-and-soil presentation

The release orchard terrain palette SHALL bind its soil base and square grass landform to the exact Runtime64 assets represented by the third registered painter choice, `terrain-brush.grass-on-soil.forward`. Enemy-route visual cells SHALL use that brush's dirt base with no stone-road landform, while retaining their ordered route and traversability. At rest, the battlefield SHALL present the authored terrain continuously without a permanent planting-cell outline, fill, or marker overlay, while retaining projected placement feedback whenever a pot placement interaction is active.

#### Scenario: Bundled battlefield is idle

- **WHEN** a bundled battle is rendered with no pot tool selected and no pot drag active
- **THEN** soil and plantable grass use the third registered grass-on-soil brush assets and no planting-cell grid, translucent cell fill, or idle cell marker is drawn above them

#### Scenario: Monster route is rendered

- **WHEN** a bundled or recommended battlefield route is presented
- **THEN** its route cells show the Runtime64 dirt base without a stone-road overlay while route movement, endpoints, and gameplay capabilities remain unchanged

#### Scenario: Pot placement is active

- **WHEN** the player selects the pot tool or drags a pot across the battlefield
- **THEN** legal and illegal placement feedback remains aligned to the existing projected hit rectangles above the grid-free terrain

#### Scenario: Release palette is regenerated

- **WHEN** project configuration or editor validation rebuilds the release terrain palette
- **THEN** the exact third-choice Runtime64 soil and composite grass assets are restored without changing other registered surface and contour bindings

#### Scenario: Bundled map semantics are compared

- **WHEN** the third-brush presentation and dirt route are enabled for any bundled map
- **THEN** only the authored route presentation changes while gameplay capabilities, route samples, markers, hit-test geometry, and deterministic simulation data remain unchanged

## MODIFIED Requirements

### Requirement: Canonical map-derived terrain masks

The runtime presenter SHALL draw each cell's required base directly, SHALL derive landform and pair-edge masks from equality of canonical landform surface plus contour style using the established NW=`1`, NE=`2`, SE=`4`, SW=`8` contract, and SHALL NOT infer any visual layer from plantability, routes, collision, or markers. Migrated bundled maps MUST explicitly select square contours for their grass landforms, while monster-route cells MUST explicitly remain base-only soil unless deliberately changed under visual acceptance.

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
