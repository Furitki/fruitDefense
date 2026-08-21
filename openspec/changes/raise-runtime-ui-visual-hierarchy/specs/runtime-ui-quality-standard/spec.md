## ADDED Requirements

### Requirement: Target-size UI art tiers
The runtime UI art contract SHALL distinguish micro assets used at 18 logical pixels from standard assets used at 24 logical pixels or larger, and SHALL export each tier from reviewed masters into its final target canvas without runtime fallback to another tier.

#### Scenario: Battle header resource icon is rendered
- **WHEN** sunlight, core, or wave information appears in the battle header
- **THEN** the presenter uses the required 18×18 micro semantic slot and does not downscale the 96×96 standard slot as a substitute

#### Scenario: Production ArtSet is validated
- **WHEN** any production ArtSet or manifest is imported
- **THEN** all standard and micro slots are present exactly once, their tier-specific canvas, scale, safe inset, optical bounds, source ownership, and hashes match the final runtime files

### Requirement: Micro icon silhouette legibility
Each micro resource icon SHALL remain identifiable from its final 18×18 raster and binary significant-alpha silhouette, SHALL occupy the documented visual envelope, and SHALL preserve a minimum two-pixel critical feature at the target raster.

#### Scenario: Micro resource family is inspected
- **WHEN** the sun, core, and wave micro icons are rendered at actual size and converted to significant-alpha silhouettes
- **THEN** no pair has an identical or over-threshold-confusable silhouette, no critical semantic feature disappears, and each visible envelope remains optically centered

### Requirement: Layered route artwork
Route artwork SHALL separate background atmosphere, structural UI surfaces, and foreground framing so that generated illustrations never own player text, control geometry, or interaction states.

#### Scenario: Generated route master is exported
- **WHEN** a high-resolution route illustration is accepted
- **THEN** deterministic export produces the declared portrait target crop, records its source/runtime hashes, and rejects baked text, uncontrolled alpha fringe, wrong aspect behavior, or missing safe composition space

#### Scenario: Route page is composed
- **WHEN** Lobby or Settlement is drawn
- **THEN** the route illustration remains behind opaque readable content surfaces and the combined page exposes at least three intentional value/depth planes without obscuring player information
