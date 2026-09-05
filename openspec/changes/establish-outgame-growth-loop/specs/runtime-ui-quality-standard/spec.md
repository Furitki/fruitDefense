## MODIFIED Requirements

### Requirement: Reference-driven route hierarchy
Bootstrap, Home, Activity, Growth, Battle, and Settlement SHALL apply the approved Sunny Orchard hierarchy through shared component anatomy, route-appropriate density, illustrations, and ornament restraint while keeping player copy, balances, states, and controls primary.

#### Scenario: Reference layout is adapted to a compact runtime rectangle
- **WHEN** a reference component is composed inside an existing compact runtime rectangle
- **THEN** its hierarchy and orchard identity remain recognizable without baking text, stretching ornaments, hiding controls, or changing hit geometry implicitly

#### Scenario: Hub pages share chrome
- **WHEN** Home, Activity, and Growth are compared at the same viewport
- **THEN** top chrome and bottom navigation preserve identical anchors and anatomy while each page retains a distinct content hierarchy

### Requirement: Concept references remain non-production evidence
Generated full-page UI concepts SHALL remain review evidence and SHALL NOT be used as runtime screens, baked-copy surfaces, or direct layout geometry; every production raster SHALL have one owned text-free master, semantic ArtSet binding, deterministic export record, stable GUID, and validated importer metadata.

#### Scenario: Gameplay-stage art enters production
- **WHEN** the approved concept direction is implemented
- **THEN** a standalone text-free nine-slice gameplay-stage master is exported through the production pipeline and the full-page generated concept has no release dependency

#### Scenario: Production ArtSets are validated
- **WHEN** the production ArtSet is imported after hub semantics are added
- **THEN** every slot required by the single current ArtSet schema is present exactly once and all stage, hub-navigation, activity, equipment, cultivation, and growth-preview bindings have approved geometry, safe inset, optical bounds, source/runtime ownership, hashes, and stable binding

## ADDED Requirements

### Requirement: Shared hub component state language
Hub navigation, resource balances, activity cards, equipment slots, cultivation nodes, and growth previews SHALL use shared semantic anatomy with finite normal, selected, pressed, loading, disabled, locked, success, completed, warning, and error states as applicable.

#### Scenario: Hub state is communicated
- **WHEN** a navigation item is selected, a reward is claimed, an upgrade is unaffordable, or a growth source is suppressed
- **THEN** copy and at least one outline, shape, icon, marker, or opacity cue communicate the state without relying only on color

### Requirement: Page-level primary action hierarchy
Home Start SHALL be the only Home page-level primary action, an eligible Activity Claim SHALL be the only primary action in its activity card, and Growth SHALL expose at most one enabled primary mutation action for the selected detail.

#### Scenario: Multiple growth entries are visible
- **WHEN** Equipment or Cultivation displays a list of entries
- **THEN** list selection remains secondary and only the selected detail's valid equip or upgrade command receives primary-action emphasis

