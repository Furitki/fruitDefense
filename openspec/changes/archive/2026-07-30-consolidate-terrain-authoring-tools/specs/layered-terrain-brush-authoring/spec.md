## MODIFIED Requirements

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
An author SHALL be able to leave a landform on its reusable generic edge or select one registered second-pass edge style for the requested material pair and contour. Every future pair-edge registration SHALL use the same resolution contract: an exact directed binding wins; otherwise the opposite ordered binding for the same unordered material pair, contour, and edge style SHALL serve the request with the complemented four-corner mask, but only when that TileSet has a renderable mask-00 endpoint. The renderer MUST reject an empty source mask before complementation, MUST render a full reverse source through the shared mask-00 endpoint, and MUST NOT generate pixels, flip textures, or cross-apply another material pair, contour, or edge style.

#### Scenario: Edge refinement is disabled
- **WHEN** A is painted over B with no edge style
- **THEN** presentation draws B base and A landform only

#### Scenario: Registered refinement is enabled
- **WHEN** A on B requests an exact registered edge style
- **THEN** the renderer uses that directed TileSet and its source mask without complementation

#### Scenario: Reverse-only refinement exists
- **WHEN** only B on A is registered, A on B requests the same contour and edge style, and the B-on-A TileSet contains a renderable mask-00 endpoint
- **THEN** the renderer reuses the reverse-only TileSet with the complemented A-occupancy mask instead of requiring a duplicate resource

#### Scenario: Reverse pair uses the shared registration
- **WHEN** B on A requests the same contour and edge style, no exact B-on-A override exists, and the A-on-B TileSet contains a renderable mask-00 endpoint
- **THEN** the renderer reuses that TileSet with the complemented B-occupancy mask

#### Scenario: Shared reverse brush reaches a full interior
- **WHEN** the B-on-A source mask is full
- **THEN** complementation resolves mask 00 and the renderer draws the shared TileSet's mask-00 endpoint so the B center remains filled

#### Scenario: Shared registration lacks mask 00
- **WHEN** only the opposite ordered TileSet exists but it has no renderable mask-00 endpoint
- **THEN** the reverse brush is unavailable with a validation reason instead of registering a brush that loses its center

#### Scenario: Pair, contour, or edge style differs
- **WHEN** a requested edge differs from a registered binding by unordered material pair, contour, or edge style
- **THEN** the request is unavailable and the renderer does not cross-apply that binding
