# layered-terrain-brush-authoring Specification

## Purpose
Define reusable material roles, bounded base-plus-landform composition, directed edge refinement, editor brush operations, and acceptance requirements for layered terrain authoring.
## Requirements
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

### Requirement: Bounded base-plus-landform authoring model
Every authored visual cell SHALL contain exactly one valid base surface, MAY contain one valid landform surface, and MUST contain no edge style unless its landform and ordered base pair have a registered binding.

#### Scenario: Base-only cell is authored
- **WHEN** a cell has a valid base and no landform
- **THEN** compilation succeeds and presentation draws only the base cell

#### Scenario: Third visual overlay is requested
- **WHEN** authoring data attempts to stack more than one landform above the base
- **THEN** validation rejects the unsupported stack instead of silently flattening or discarding a layer

### Requirement: Explicit base, landform, and ordered-pair brushes
The editor SHALL expose pure-base, landform-only, and ordered-pair paint operations with Undo, SHALL permit both `A on B` and `B on A`, and SHALL treat those two pair directions as distinct authoring selections while allowing their registered same-contour edge art to share one TileSet through the complemented-mask contract.

#### Scenario: Ordered pair is painted
- **WHEN** the author selects foreground A, background B, and pair mode and paints a cell
- **THEN** the gesture atomically writes base B and landform A and records one undoable authoring operation

#### Scenario: Pair direction is reversed
- **WHEN** the author swaps the selectors and paints B on A
- **THEN** the cell records base A and landform B while edge resolution uses an exact B-on-A override first or the compatible A-on-B TileSet with a complemented mask second

#### Scenario: Pure base replaces a pair
- **WHEN** the author paints pure base A over a cell containing a landform and edge
- **THEN** the cell retains only base A and clears its landform and edge selection

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

### Requirement: Reversible registered terrain resource
Each registered terrain brush resource SHALL own one stable resource identity, two material endpoints, one contour and edge identity, one primary sixteen-mask composite TileSet, and a validated complemented TileSet view of that same resource, and SHALL produce exactly two reciprocal paint choices without copying, generating, or substituting source pixels at selection time.

#### Scenario: Both reusable landforms exist
- **WHEN** the palette provides contour-compatible landforms for both endpoint materials
- **THEN** the two direction choices use those reusable landforms and share the resource's edge through normal and complemented mask resolution

#### Scenario: Reverse reusable landform is absent
- **WHEN** the background endpoint has no reusable landform for the registered contour but the full-composite resource has renderable endpoint masks
- **THEN** the reverse choice uses the definition-owned complemented TileSet view and remains directly paintable without registering a global pair-specific landform

#### Scenario: Complemented view is invalid
- **WHEN** a registered definition lacks a complemented view or its mask mapping differs from `primary[Complement(mask)]`
- **THEN** registry validation rejects the resource instead of exposing a one-direction-only ordinary brush

### Requirement: Preserved authored resource registration
The brush registry SHALL support a project-authored terrain family through the same `TerrainBrushDefinition` contract as imported composite packages while allowing its source record to identify preserved existing assets rather than an external pipeline manifest.

#### Scenario: Original square family is registered
- **WHEN** editor setup discovers the preserved original square grass/soil endpoints, landforms, and edge TileSet
- **THEN** it creates or updates one definition and complemented view that reference those assets without rewriting their pixels, `.meta` files, or GUIDs

#### Scenario: Palette is refreshed
- **WHEN** registered brush definitions include a laboratory-only original family whose semantic key overlaps a newer production definition
- **THEN** the original remains paintable in the laboratory without replacing production Palette authority or removing existing organic compatibility bindings

### Requirement: Explicit pure-square terrain presets
The canonical battlefield map authoring workflow SHALL expose named pure-square presets for the registered grass and soil surfaces. Applying a pure-square preset MUST set the selected cell's base surface and clear its landform, contour, and pair-edge identifiers in one undoable edit, and MUST NOT create or select a synthetic sixteen-mask TileSet.

#### Scenario: Paint a grass square
- **WHEN** an author applies the grass-square preset to a visual cell
- **THEN** the cell stores grass as its base surface and stores empty landform, contour, and pair-edge identifiers

#### Scenario: Replace a layered cell with a soil square
- **WHEN** an author applies the soil-square preset to a cell that previously contained a Dual-Grid landform
- **THEN** the cell stores soil as its base surface and removes every optional layered-terrain identifier in the same undoable edit

#### Scenario: Preview a pure-square cell
- **WHEN** a base-only visual cell is previewed by the canonical map workflow
- **THEN** it is presented from the surface's opaque base texture without resolving a Dual-Grid mask

### Requirement: Single representation per touching surface
The battlefield visual-cell compiler SHALL reject a map when the same surface appears as base-only terrain on one cell and as a Dual-Grid landform on another cell that shares an edge or vertex. The compiler SHALL allow disconnected regions to use different representations and SHALL allow different base-only surfaces to touch.

#### Scenario: Reject edge contact across representations
- **WHEN** a grass base-only cell shares an edge with a cell whose landform surface is grass
- **THEN** compilation fails with a focused `surface.shared-representation-mix` diagnostic identifying both cells and the surface

#### Scenario: Reject diagonal contact across representations
- **WHEN** a soil base-only cell shares only a vertex with a cell whose landform surface is soil
- **THEN** compilation fails with a focused `surface.shared-representation-mix` diagnostic identifying both cells and the surface

#### Scenario: Allow disconnected use of both representations
- **WHEN** base-only grass and Dual-Grid grass regions do not share an edge or vertex
- **THEN** visual-cell compilation succeeds if all other terrain rules are satisfied

#### Scenario: Allow unlike square surfaces to touch
- **WHEN** a base-only grass cell touches a base-only soil cell
- **THEN** visual-cell compilation succeeds and preserves their intentional cell-aligned boundary

