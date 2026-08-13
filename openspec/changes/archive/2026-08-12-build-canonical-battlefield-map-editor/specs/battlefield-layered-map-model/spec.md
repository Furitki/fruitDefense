## ADDED Requirements

### Requirement: Unity authoring asset round trip
The canonical layered map model SHALL provide one Unity-serializable authoring representation that converts deterministically into `BattlefieldLayeredMapSource`, preserves stable IDs and source order where order is semantic, and does not expose a second mutable runtime map truth.

#### Scenario: Save and reopen an authored map
- **WHEN** a valid authoring asset is serialized, reloaded, and converted to canonical source
- **THEN** its header, visual cells, gameplay cells, route order, marker groups, markers, compiler diagnostics, and gameplay fingerprint equal the pre-reload result

#### Scenario: Compile an invalid draft
- **WHEN** a serialized draft contains invalid coverage, route, marker, capability, or collision data
- **THEN** conversion preserves the authored data for repair and canonical compilation reports the same structured domain errors instead of normalizing them into a different valid map

### Requirement: Bounded authoring mutations
Every official authoring mutation SHALL operate against the asset's configured grid dimensions, SHALL preserve exact visual and gameplay coverage, and MUST leave the asset unchanged when coordinates, identifiers, or operation preconditions are invalid.

#### Scenario: Resize with default fill
- **WHEN** an author confirms a resize to positive dimensions
- **THEN** retained in-bounds cells preserve their data, new cells receive explicit defaults, removed route or marker coordinates are reported, and one Undo can restore the previous aggregate

#### Scenario: Reject invalid coordinate
- **WHEN** a mutation addresses a negative coordinate or a coordinate at or beyond width or height
- **THEN** no layer, route, marker, identity, or fingerprint-affecting field changes

