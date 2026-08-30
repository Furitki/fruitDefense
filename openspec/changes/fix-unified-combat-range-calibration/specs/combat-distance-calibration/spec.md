## ADDED Requirements

### Requirement: Unified per-cell combat-distance calibration
Every `BattlefieldMapDefinition` SHALL convert legacy combat distances from the active map's `MapUnitsPerCell` relative to `LegacyReferenceMapUnitsPerCell` and `LegacyReferenceDistanceScale`. The conversion MUST NOT derive from any route's total length, route count, route order, or execution profile.

#### Scenario: Equal-pitch maps have different route lengths
- **WHEN** two maps use the same `MapUnitsPerCell` but contain routes with different total lengths
- **THEN** the same legacy combat distance resolves to the same map distance and the same number of cells on both maps

#### Scenario: A map uses a different valid cell pitch
- **WHEN** a map has a valid `MapUnitsPerCell` different from the canonical reference pitch
- **THEN** its converted legacy distance covers the canonical-equivalent number of map cells without inspecting a route length

### Requirement: Normal and GM combat use one calibration path
Normal battle maps and GM battle maps SHALL construct their combat-distance conversion through the same `BattlefieldMapDefinition` calibration rule. GM MUST NOT supply a separate fixed combat-distance override.

#### Scenario: Standard and GM maps share a cell pitch
- **WHEN** a standard map and the GM map use the canonical map-units-per-cell value
- **THEN** their legacy-distance scales and converted per-cell coverage are equal even when their routes differ in count and length

### Requirement: Combat and inspected-range presentation stay aligned
Target selection, projectile behavior, and inspected attack-range presentation SHALL consume the same effective map distance after unified calibration.

#### Scenario: A target sits on each side of the calibrated boundary
- **WHEN** deterministic target points are placed immediately inside and outside an inspected plant's calibrated effective range on a short-route map
- **THEN** combat selects only the inside point and the projected range overlay boundary separates the same points
