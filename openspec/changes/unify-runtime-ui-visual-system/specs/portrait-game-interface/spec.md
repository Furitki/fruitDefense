## ADDED Requirements

### Requirement: Unified release-flow visual hierarchy
Bootstrap, Lobby, Battle chrome, blocking overlays, and Settlement SHALL apply the shared runtime UI visual system with consistent surface depth, typography roles, spacing rhythm, action hierarchy, state cues, and artwork treatment while retaining each route's existing information and commands.

#### Scenario: Player traverses the release flow
- **WHEN** the player moves from Lobby through Battle to Settlement
- **THEN** screen titles, primary and secondary actions, cards, metrics, status feedback, and modals remain recognizably related without confusing application chrome with battlefield content

#### Scenario: Startup or route error is visible
- **WHEN** Bootstrap displays initialization, blocking failure, recoverable failure, or retry presentation
- **THEN** it uses the same typography, surface, status, and action hierarchy as the remaining release routes rather than the default Unity skin

### Requirement: Shared artwork scales safely in portrait
Shared panel, button, card, indicator, and icon artwork SHALL preserve protected corners, outline weight, transparent padding, and legibility when rendered at the supported full and inset portrait viewports, and visual scaling MUST NOT create a second interaction rectangle or displace existing draw/hit geometry.

#### Scenario: Supported portrait sizes are validated
- **WHEN** release UI renders at 360-by-800, 375-by-812, 402-by-874, and 430-by-932 with full and supported inset safe areas
- **THEN** shared artwork is neither clipped nor visibly stretched, essential Chinese text remains readable, primary targets remain touch-sized, and pointer or drag behavior stays aligned with the rendered controls

