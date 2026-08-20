## MODIFIED Requirements

### Requirement: Visually centered component groups
Actions, metrics, status rows, title ribbons, modal hints, and result rows that combine surfaces, icons, indicators, labels, or values SHALL align their final rendered visual mass as one composition rather than centering text line boxes independently from separately anchored art. Shared semantic typography SHALL apply the packaged font's role-level optical correction without presenter-specific offsets.

#### Scenario: Action contains an icon and Chinese label
- **WHEN** a primary, secondary, quiet, danger, loading, or disabled action is rendered
- **THEN** the combined actual icon alpha and corrected Chinese glyph group is optically centered within the action, maintains its semantic gap, and does not become left-heavy, right-heavy, top-heavy, or bottom-heavy at any supported scale

#### Scenario: Paired actions are rendered
- **WHEN** two actions share one row in a modal or route footer
- **THEN** their hit rectangles and final visible surface dimensions match, their content groups use the same vertical center, and neither surface or feedback extends outside its owner

#### Scenario: Paused modal title and hint are rendered
- **WHEN** the paused Battle modal displays its title, warning indicator, instruction copy, continue action, and restart action
- **THEN** the title glyph mass is centered in its ribbon, the warning indicator and instruction copy form one centered row, and both action surfaces and their icon-label groups are visibly aligned

#### Scenario: Repeated metric rows are rendered
- **WHEN** multiple comparable metrics appear in a header or result card
- **THEN** icon alpha, corrected label baseline, corrected value baseline, and group insets align consistently and no icon straddles the component border
