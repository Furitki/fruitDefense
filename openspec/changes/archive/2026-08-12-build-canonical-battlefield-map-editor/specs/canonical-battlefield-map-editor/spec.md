## ADDED Requirements

### Requirement: Canonical bounded map asset
The editor SHALL create and edit one ScriptableObject authoring asset that owns only a stable map identity, positive fixed grid dimensions, complete visual and gameplay cell coverage, the current primary ordered route, typed marker groups, and typed markers, and every cell mutation MUST reject coordinates outside those dimensions. Level identity, template level identity, and publish order MUST belong only to a publication-manifest entry that references the map asset.

#### Scenario: Create a blank map
- **WHEN** an author creates an `8 × 7` map with a valid map ID
- **THEN** the asset contains exactly 56 default visual cells, 56 default gameplay cells, no out-of-bounds data, and can be saved without editing C#

#### Scenario: Paint beyond the canvas
- **WHEN** a brush, rectangle, fill, route, or marker operation resolves a cell outside the configured dimensions
- **THEN** the asset remains unchanged and the editor reports the rejected coordinate

### Requirement: Shared two-dimensional editing canvas
The official map editor SHALL provide gameplay, route-and-marker, presentation, and validation workspaces on one bounded top-down grid, SHALL derive cell drawing, hover, selection, overlays, and pointer hit testing from the same canvas layout, and SHALL render presentation through the selected template level's real theme and terrain palette using the same semantic base, landform mask, and exact directed-edge rules as Battle.

#### Scenario: Switch authoring workspace
- **WHEN** an author switches from gameplay to presentation
- **THEN** the grid, zoom, scroll, hovered coordinate, and canonical cell positions remain aligned while only tools and overlays change

#### Scenario: Use an area tool
- **WHEN** an author applies a rectangle or flood-fill operation
- **THEN** only the bounded resolved cell set changes and one Undo restores the entire operation

#### Scenario: Palette binding is missing
- **WHEN** the selected template palette lacks a base, landform, or exact directed edge used by a cell
- **THEN** the canvas identifies the missing binding and coordinate and does not present a placeholder color as successful runtime-equivalent terrain

### Requirement: Gameplay topology authoring
The editor SHALL expose only reviewed gameplay capability and collision identifiers, SHALL allow compatible capabilities on one cell, and MUST NOT change gameplay cells when presentation is painted.

#### Scenario: Mark a plantable cell
- **WHEN** an author enables the plantable capability on a valid cell
- **THEN** the gameplay cell changes independently of its base, landform, and edge presentation

#### Scenario: Paint grass over a blocked cell
- **WHEN** an author paints grass presentation over a cell that blocks placement
- **THEN** the cell remains blocked and the editor may show a mismatch warning without changing gameplay

### Requirement: Ordered route and typed marker authoring
The editor SHALL edit the current `route.main` as an ordered list of in-bounds cardinally adjacent cells, SHALL expose route direction and endpoints, and SHALL author stable typed spawn, goal, core, and initial-pot markers without free-form executable payloads.

#### Scenario: Append a connected route cell
- **WHEN** an author appends an in-bounds cell cardinally adjacent to the current route tail
- **THEN** the ordered route extends by one cell and its direction overlay, spawn, and goal relationship remain inspectable

#### Scenario: Attempt a disconnected append
- **WHEN** an author tries to append a diagonal or non-adjacent cell
- **THEN** the route remains unchanged and the editor identifies the disconnected candidate

#### Scenario: Place a required marker
- **WHEN** an author places the core or an initial-pot candidate
- **THEN** the marker receives a stable typed identity and compilation validates its cell, group, multiplicity, and capability requirements

### Requirement: Semantic presentation tools and explicit suggestions
The editor SHALL paint semantic base, optional landform, and optional exact directed edge style within map bounds, SHALL derive Dual-Grid masks automatically, and MAY apply an explicit undoable topology-to-presentation suggestion without granting presentation gameplay authority.

#### Scenario: Apply recommended presentation
- **WHEN** an author confirms the recommendation command
- **THEN** route, plantable, and remaining cells receive the configured recommended semantic surfaces in one undoable operation while gameplay cells, routes, and markers remain byte-equivalent

#### Scenario: Exact edge direction is missing
- **WHEN** a requested refined edge is unavailable for the authored landform/base direction
- **THEN** that edge selection is rejected without reversing the pair or silently changing the style

### Requirement: Draft diagnostics and publication gate
The editor SHALL allow invalid drafts to be saved, SHALL aggregate authoring, canonical compiler, and catalog diagnostics with stable codes and affected identities, and MUST disable publication and Battle playtest until all blocking errors are resolved.

#### Scenario: Save an incomplete route draft
- **WHEN** a map has a disconnected route or missing core marker
- **THEN** the asset can be saved, blocking diagnostics remain visible, and Publish and Playtest remain unavailable

#### Scenario: Repair all blocking errors
- **WHEN** the map compiles and the publication-manifest entry that references it, including its level ID and template level ID, passes catalog validation
- **THEN** the editor reports publish-ready status and enables manifest rebuild followed by normal catalog-reloaded Battle playtest

### Requirement: End-to-end author acceptance
Acceptance MUST create a fresh map through the official editor without C# or raw TileSet edits, save and reopen it, publish it, compile it through the level catalog, and run it through the normal Battle presentation and gameplay flow using shared projection.

#### Scenario: Author a new playable map
- **WHEN** the acceptance workflow completes a valid map from a blank asset
- **THEN** its identity, layers, route order, markers, diagnostics, and catalog references survive reload, the generated resource is rebuilt and reloaded, AppFlow reports the expected levelId and mapId, and the map supports spawning, route traversal, planting, core damage, and settlement in Battle

#### Scenario: Capture acceptance evidence
- **WHEN** editor and portrait runtime evidence is recorded
- **THEN** it shows the complete bounded map editor and the same published map in normal Battle; the terrain material laboratory or mask board is not accepted as substitute evidence
