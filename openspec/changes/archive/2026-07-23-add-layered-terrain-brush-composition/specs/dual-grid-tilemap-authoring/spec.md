## MODIFIED Requirements

### Requirement: Reusable binary terrain layers
The system SHALL preserve independent Dual-Grid topology per landform while allowing a configured authoring surface to combine one cell-aligned base layer, one optional landform layer, and one optional ordered-pair edge output. Base, landform, and edge generated outputs MUST remain distinct from logical authoring data.

#### Scenario: Two material roles share a Grid
- **WHEN** material A is configured as base and material B as landform under one shared Grid
- **THEN** base cells render on the logical grid, B resolves on vertex-aligned Dual-Grid output, and neither output overwrites logical authoring cells

#### Scenario: Pair direction changes
- **WHEN** the author switches from B on A to A on B
- **THEN** the corresponding base, landform, and exact directed edge outputs rebuild without changing mask numbering or half-cell alignment

### Requirement: Layered terrain demo and manual art acceptance
The developer demo SHALL provide base-only, landform-only, and ordered-pair paint modes for two configured materials, SHALL expose an optional registered second-pass edge toggle, and SHALL refresh generated previews while the author paints or erases with Undo support.

#### Scenario: Author paints a pure base
- **WHEN** base mode is active and the author paints material A
- **THEN** the selected cell becomes opaque A base and its previous landform and edge are cleared

#### Scenario: Author paints an ordered pair
- **WHEN** pair mode selects foreground A, background B, and an available edge style
- **THEN** one gesture writes the pair, refreshes the affected base cell and Dual-Grid vertices, and remains undoable

#### Scenario: Author disables the refined edge
- **WHEN** the same A on B landform is viewed with the edge toggle disabled
- **THEN** the transparent A silhouette remains visible over B while the pair-edge output is absent

#### Scenario: Project smoke validation runs
- **WHEN** `FruitDefense.Editor.ProjectSetup.SmokeValidate` executes
- **THEN** pure bases, both pair orders, edge on/off, erasing, Undo-safe mutation, generated-output separation, alignment, and all sixteen masks are validated

