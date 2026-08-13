## MODIFIED Requirements

### Requirement: Release battle terrain binding
Each resolved level theme SHALL reference a valid stable terrain-palette identity whose registered runtime palette supplies the opaque soil base and renderable Dual-Grid TileSets required by that theme's semantic surfaces. Project configuration and the release Battle scene SHALL reproduce the required palette registry without hard-coding one active level's grass/road choice or requiring manual Inspector repair.

#### Scenario: Release Battle scene is inspected
- **WHEN** the configured `Assets/Scenes/Battle.unity` presenter and palette registry are loaded
- **THEN** every bundled theme resolves its stable palette ID to the required soil texture and renderable surface-mask sprites

#### Scenario: Project configuration recreates the Battle scene
- **WHEN** `Fruit Defense/Configure Project` recreates release scenes
- **THEN** the recreated battle presenter receives the same validated terrain-palette registry without manual Inspector repair

#### Scenario: Active level changes
- **WHEN** two bundled levels reference different valid terrain palettes
- **THEN** the presenter resolves the active level's palette and never silently retains or substitutes the previous level's palette

### Requirement: Canonical map-derived terrain masks
The runtime presenter SHALL derive each terrain mask from the canonical semantic visual-surface layout, SHALL use the established NW=`1`, NE=`2`, SE=`4`, SW=`8` contract without storing per-map mask choices, and SHALL NOT infer surface occupancy from plantability, route membership, collision, or markers. Migrated current maps MUST default plantable cells to grass, route/spawn/goal cells to stone road, and remaining cells to soil so their accepted terrain presentation remains unchanged.

#### Scenario: Interior grass surface is rendered
- **WHEN** all four logical cells around a battlefield visual vertex declare the grass surface
- **THEN** the grass renderer selects mask `15` regardless of whether those cells are plantable

#### Scenario: Interior stone-road surface is rendered
- **WHEN** all four logical cells around a battlefield visual vertex declare the stone-road surface
- **THEN** the stone-road renderer selects mask `15` regardless of route membership

#### Scenario: Visual and gameplay layers differ
- **WHEN** a non-plantable cell declares grass or an enemy-route cell declares soil
- **THEN** terrain follows the declared surface while placement and movement continue to follow compiled gameplay topology

#### Scenario: Surface border is resolved
- **WHEN** grass, stone road, soil, and out-of-bounds cells meet around a visual vertex
- **THEN** every requested overlay mask contains only corners with its matching semantic surface and no lookup reads a nonexistent map cell
