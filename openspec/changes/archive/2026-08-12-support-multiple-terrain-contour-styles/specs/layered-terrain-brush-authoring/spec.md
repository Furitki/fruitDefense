## MODIFIED Requirements

### Requirement: Reusable terrain material roles
Each terrain material SHALL provide one stable surface identity, one opaque seamless cell-aligned base visual, and one or more explicitly identified transparent sixteen-mask contour TileSets, and the same semantic material SHALL be valid in either base or landform role without baking another material into its reusable landform assets.

#### Scenario: Material is painted as a pure base
- **WHEN** an author paints material A in base mode
- **THEN** the selected cells contain opaque A base visuals with no required landform, contour, or pair-edge asset

#### Scenario: Material is painted as a square landform
- **WHEN** an author paints material A with square contour over an existing material B base
- **THEN** A uses its square transparent TileSet and uncovered pixels continue to show the authored base

#### Scenario: Material is painted as an organic landform
- **WHEN** an author paints the same material A with organic contour in a disconnected region
- **THEN** A retains the same semantic identity while using its organic transparent TileSet

### Requirement: Optional directed pair-edge refinement
An author SHALL be able to leave a landform on its reusable contour edge or select one registered second-pass edge style for the material pair and contour-style combination. Every future pair-edge registration SHALL use the same resolution contract: an exact directed binding wins; otherwise the opposite ordered binding for the same unordered material pair, contour, and edge style SHALL serve the request with the complemented four-corner mask, but only when that TileSet has a renderable mask-00 endpoint. The renderer MUST reject an empty source mask before complementation, MUST render a full reverse source through the shared mask-00 endpoint, and MUST NOT generate pixels, flip textures, or cross-apply another contour, edge style, or material pair.

#### Scenario: Edge refinement is disabled
- **WHEN** square A is painted over B with no edge style
- **THEN** presentation draws B base and the square A landform only

#### Scenario: Registered square refinement is enabled
- **WHEN** square A over B requests a valid registered painted edge
- **THEN** the matching transparent square `A on B` edge tiles draw above A without changing semantic surface or topology

#### Scenario: Reverse pair uses the shared edge
- **WHEN** square B is painted over A, only the square `A on B` edge binding exists, and that TileSet has a renderable mask-00 endpoint
- **THEN** the renderer selects that TileSet with the complemented B-occupancy mask so the same authored material-side edge serves the reverse brush

#### Scenario: Reverse pair reaches a full interior
- **WHEN** the square B-on-A source mask is full
- **THEN** the complemented mask 00 selects the shared TileSet's mask-00 endpoint so the B center remains filled

#### Scenario: Shared edge lacks mask 00
- **WHEN** only the opposite ordered TileSet exists but its mask-00 endpoint is not renderable
- **THEN** the reverse brush remains unavailable with a validation reason instead of losing its center

#### Scenario: Only organic refinement exists
- **WHEN** the ordered material pair has an organic edge but the author requests the same edge style for a square contour
- **THEN** validation identifies the unavailable contour-specific binding and does not substitute the organic asset

#### Scenario: Contour, style, or material pair differs
- **WHEN** a requested edge differs from a registered binding by contour style, edge style, or unordered material pair
- **THEN** the request is unavailable and the renderer does not cross-apply that binding

#### Scenario: Edge style is partially enabled inside one region
- **WHEN** only part of one connected exact foreground/background/contour region selects a painted edge style
- **THEN** authoring refuses the partial selection or updates the complete region so no false internal material band is created

### Requirement: AI-refined edge acceptance contract
Second-pass AI or artist-authored edge art SHALL preserve the approved contour topology and protected tile-border sockets, SHALL package a direction-neutral top-down blend whose outside contribution remains narrow at real Battle scale, and SHALL pass assembled seam, topology, style, and real-scale review before it is selectable as a pair edge.

#### Scenario: Authored edge changes a socket
- **WHEN** a candidate edge set changes RGBA pixels in a protected socket or makes a compatible border mismatch
- **THEN** asset validation rejects the set even if individual tiles look visually plausible

#### Scenario: Square edge board is visually accepted
- **WHEN** the candidate is assembled into the required squares, strips, turns, islands, holes, and diagonal cases at real battle scale
- **THEN** the acceptance record confirms no seams, no topology drift, a cell-aligned square footprint, continuous top-down material texture, no raised soil skirt, and no uniform dark or secondary outer contour

#### Scenario: Painted edge reaches its background material
- **WHEN** the square grass-on-soil transition extends outward into the soil base
- **THEN** it fades using grass-derived pixels over the existing cell-aligned soil texture, contributes no opaque soil wall or directional shadow, and leaves no visible line where the overlay ends

### Requirement: Authorized production full-composite brushes
The terrain palette SHALL register the authorized A grass-on-soil and B stone-on-water Runtime64 families as exact square refined pair edges, SHALL use each family's mask-00 background and mask-15 foreground as its opaque semantic endpoints, SHALL retain all sixteen source masks and the unaltered Review256-derived pipeline provenance in a versioned production folder, and MUST NOT infer a missing water landform or change the recorded seam-safety claim.

#### Scenario: Grass and soil production brush is installed
- **WHEN** the production palette resolves square refined grass over soil
- **THEN** it returns the A family without mask complementation, uses A mask 00 for soil and A mask 15 for grass, and retains sixteen renderable masks

#### Scenario: Stone and water production brush is installed
- **WHEN** the production palette resolves square refined stone-road over water
- **THEN** it returns the B family without mask complementation, uses B mask 00 for water and B mask 15 for stone-road, and retains sixteen renderable masks

#### Scenario: Reverse edge is queried
- **WHEN** either installed pair is queried in the opposite direction
- **THEN** the shared full-composite TileSet resolves through complemented masks because its mask-00 endpoint is renderable, while authoring still requires a real landform binding before offering that reverse direction as a paint preset

### Requirement: Reusable pipeline brush package
Every importable full-composite brush candidate SHALL carry one machine-readable Unity brush descriptor owning stable identity, labels, semantic foreground/background surfaces, contour, edge style, endpoint masks, runtime-mask location, and runtime tile size. One generic importer SHALL create or update the production files, TileSet, endpoint tiles, and brush definition without pair-specific code, configure Sprite PPU from the declared size, remove only an obsolete runtime-resolution folder for the same brush after successful replacement, and keep repeated import idempotent.

#### Scenario: Accepted brush is repackaged for clarity
- **WHEN** an accepted Review256 family declares a 64-pixel runtime size
- **THEN** the pipeline deterministically emits sixteen Runtime64 masks and the importer binds them at 64 PPU while retaining the separate 32-pixel stress-atlas contract

#### Scenario: A future brush is imported
- **WHEN** an author chooses a valid candidate containing a pipeline manifest, `BrushImport.json`, and sixteen Runtime masks
- **THEN** one action creates or updates its versioned production folder and registered brush definition without editing importer source code

#### Scenario: Import metadata or masks are incomplete
- **WHEN** the descriptor omits semantic identity, names a mask outside `0..15`, disagrees with the pipeline profile, or any Runtime mask is missing
- **THEN** import stops with an actionable error and does not partially register the brush

#### Scenario: The same brush is imported again
- **WHEN** a candidate with the same stable brush id and output folder is imported again
- **THEN** the importer deterministically updates the same assets and registry entry rather than adding duplicate authoring buttons
