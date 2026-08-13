## MODIFIED Requirements

### Requirement: Layered terrain demo and manual art acceptance
The developer demo SHALL provide base-only, landform-only, and ordered-pair paint modes for two configured materials, SHALL require the exact registered directed refined edge for every landform-bearing laboratory brush, SHALL present pair presets with representative composites built from the real active assets, and SHALL refresh generated output and a lightweight hovered-cell outline while the author paints or erases with Undo support.

#### Scenario: Author paints a pure base
- **WHEN** base mode is active and the author paints material A
- **THEN** the selected cell becomes opaque A base and its previous landform and edge are cleared

#### Scenario: Author paints an ordered pair
- **WHEN** pair mode selects foreground A and background B and the exact directed refined edge is registered
- **THEN** its preset card shows the real material transition and one gesture writes the pair plus refinement, refreshes the affected base cell and Dual-Grid vertices, and remains undoable

#### Scenario: Exact directed refinement is missing
- **WHEN** the active contour has no refined edge for the selected ordered pair but has the reverse direction or a bare contour
- **THEN** that pair brush is unavailable with an actionable reason and neither fallback is substituted

#### Scenario: Pointer moves without painting
- **WHEN** the active pointer crosses logical cells without a pressed paint button
- **THEN** the Scene outline follows the resolved cell and does not render a textured result preview

#### Scenario: Project smoke validation runs
- **WHEN** `FruitDefense.Editor.ProjectSetup.SmokeValidate` executes
- **THEN** pure bases, both refined pair orders, representative preview sources, hover state, erasing, Undo-safe mutation, generated-output separation, alignment, and all sixteen masks are validated
