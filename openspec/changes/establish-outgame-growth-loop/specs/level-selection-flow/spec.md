## MODIFIED Requirements

### Requirement: Three-level Lobby selection
The Home page inside Lobby SHALL present `orchard-01`, `orchard-02`, and `orchard-03` as available selectable levels, SHALL make the current selection visually explicit, SHALL preview the exact growth allowed by that level, and SHALL launch only the selected `LevelId` with that previewed immutable growth snapshot.

#### Scenario: Select and start the coverage level
- **WHEN** the player selects the `orchard-02` card and activates Start with a valid growth preview
- **THEN** the shell creates a battle launch request whose `LevelId` is `orchard-02` and whose growth policy and fingerprint equal the visible preview

#### Scenario: Change selection without starting
- **WHEN** the player selects a different level card
- **THEN** Home updates the visible selected state and growth preview and does not begin a battle transition until Start is activated

#### Scenario: Prevent an unknown launch
- **WHEN** a caller submits a `LevelId` that is not in the compiled playable catalog or lacks a valid growth policy
- **THEN** the flow rejects the launch with a structured error and remains or recovers to a usable Home state

### Requirement: Portrait-safe selection geometry
The Home layout SHALL derive the drawn level cards, selected state, growth preview, and hit-test regions from the same safe-area-aware page-host geometry and SHALL keep all three choices, the preview, shared navigation, and Start usable at supported portrait viewports.

#### Scenario: Validate supported portrait sizes
- **WHEN** layout validation runs at 360×800, 375×812, 402×874, and 430×932 with both full and inset safe areas
- **THEN** all required level and growth content remains inside the page host, card hit targets select only their drawn card, and Start does not overlap a card, preview, or bottom navigation

#### Scenario: Use a real WebGL canvas
- **WHEN** the built WebGL Home page is exercised at the target portrait sizes
- **THEN** the selected level and its applied growth are readable, all three cards can be selected, and Start launches the visibly selected map and growth fingerprint without clipping or input offset

## RENAMED Requirements

- FROM: `Three-level Lobby selection`
- TO: `Three-level Home selection`
- FROM: `Portrait-safe selection geometry`
- TO: `Portrait-safe Home selection geometry`
