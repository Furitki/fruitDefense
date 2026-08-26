## MODIFIED Requirements

### Requirement: Integrated acceptance surfaces
The project SHALL retain all existing named battle acceptance states in the dedicated acceptance WebGL profile and SHALL provide that profile with a real acceptance sequence for Lobby, Battle, Settlement, return, retry, and failure-safe routing. The ordinary release profile MUST expose neither direct acceptance routing nor an acceptance bridge.

#### Scenario: Direct battle acceptance route
- **WHEN** the dedicated acceptance WebGL build loads with `acceptance=1` and `route=battle`
- **THEN** Bootstrap loads Battle, exposes the acceptance bridge, and forwards named state requests after the battle host is ready

#### Scenario: Acceptance-shaped query reaches the ordinary release
- **WHEN** the ordinary release WebGL build loads with `acceptance=1`, `route=battle`, or another acceptance-only query value
- **THEN** Bootstrap follows the production cold-start route to Lobby
- **AND** no acceptance bridge, direct fixture route, or named-state request becomes available

#### Scenario: Existing battle states
- **WHEN** the 13-state battle acceptance executes through the dedicated acceptance route
- **THEN** every existing screenshot, interaction, delivery, and geometry assertion still passes

#### Scenario: Full-flow acceptance
- **WHEN** the end-to-end acceptance starts from the dedicated acceptance profile's default URL
- **THEN** it captures and verifies Lobby, active Battle, Settlement, return to Lobby, and retry with no black or transparent frames
