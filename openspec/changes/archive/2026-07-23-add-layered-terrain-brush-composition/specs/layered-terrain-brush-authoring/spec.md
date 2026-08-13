## ADDED Requirements

### Requirement: Reusable terrain material roles
Each terrain material SHALL provide one stable surface identity, one opaque seamless cell-aligned base visual, and one transparent sixteen-mask Dual-Grid landform visual, and the same material SHALL be valid in either role without baking another material into its reusable landform assets.

#### Scenario: Material is painted as a pure base
- **WHEN** an author paints material A in base mode
- **THEN** the selected cells contain opaque A base visuals with no required landform or pair-edge asset

#### Scenario: Material is painted as a landform
- **WHEN** an author paints material A in landform mode over an existing material B base
- **THEN** A uses its transparent Dual-Grid silhouette and uncovered pixels continue to show the authored base

### Requirement: Bounded base-plus-landform authoring model
Every authored visual cell SHALL contain exactly one valid base surface, MAY contain one valid landform surface, and MUST contain no edge style unless its landform and ordered base pair have a registered binding.

#### Scenario: Base-only cell is authored
- **WHEN** a cell has a valid base and no landform
- **THEN** compilation succeeds and presentation draws only the base cell

#### Scenario: Third visual overlay is requested
- **WHEN** authoring data attempts to stack more than one landform above the base
- **THEN** validation rejects the unsupported stack instead of silently flattening or discarding a layer

### Requirement: Explicit base, landform, and ordered-pair brushes
The editor SHALL expose pure-base, landform-only, and ordered-pair paint operations with Undo, SHALL permit both `A on B` and `B on A`, and SHALL treat those two pair directions as distinct selections.

#### Scenario: Ordered pair is painted
- **WHEN** the author selects foreground A, background B, and pair mode and paints a cell
- **THEN** the gesture atomically writes base B and landform A and records one undoable authoring operation

#### Scenario: Pair direction is reversed
- **WHEN** the author swaps the selectors and paints B on A
- **THEN** the cell records base A and landform B without reusing the `A on B` edge binding

#### Scenario: Pure base replaces a pair
- **WHEN** the author paints pure base A over a cell containing a landform and edge
- **THEN** the cell retains only base A and clears its landform and edge selection

### Requirement: Optional directed pair-edge refinement
An author SHALL be able to leave a landform on its reusable generic edge or select one registered second-pass edge style for the exact ordered foreground/background pair, and the renderer MUST NOT synthesize or silently reverse a missing edge.

#### Scenario: Edge refinement is disabled
- **WHEN** A is painted over B with no edge style
- **THEN** presentation draws B base and A landform only

#### Scenario: Registered refinement is enabled
- **WHEN** A is painted over B with a valid registered edge style
- **THEN** the matching transparent `A on B` edge tiles draw above A without changing the base or topology

#### Scenario: Reverse-only refinement exists
- **WHEN** only `B on A` is registered and the author requests the same style for `A on B`
- **THEN** validation identifies the unavailable ordered pair and does not substitute the reverse asset

### Requirement: AI-refined edge acceptance contract
Second-pass AI edge art SHALL preserve the approved alpha topology and protected tile-border sockets, SHALL remain transparent outside its material-contact ribbon, and SHALL pass assembled seam and topology review before it is selectable as a pair edge.

#### Scenario: AI edit changes a socket
- **WHEN** a candidate edge set changes RGBA pixels in a protected socket or makes a compatible border mismatch
- **THEN** asset validation rejects the set even if individual tiles look visually plausible

#### Scenario: Edge board is visually accepted
- **WHEN** the candidate is assembled into the required islands, holes, straight edges, turns, and diagonal cases at real battle scale
- **THEN** the acceptance record confirms no seams, no mask-topology drift, and readable optional refinement for both enabled and disabled comparisons

