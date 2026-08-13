## ADDED Requirements

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
