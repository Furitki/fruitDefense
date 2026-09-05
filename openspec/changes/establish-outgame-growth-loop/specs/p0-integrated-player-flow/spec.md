## MODIFIED Requirements

### Requirement: Complete player flow
The app SHALL support a complete `Activity reward → Growth upgrade/equip → Home preview/start → Battle → Settlement → Home` loop, plus Settlement Retry, using atomic profile revisions, immutable launch requests, and single-submit battle results.

#### Scenario: Starter outgame loop
- **WHEN** a fresh player claims the starter Activity reward, performs an affordable Growth action, selects a level on Home, and activates Start
- **THEN** the app persists the reward and growth revision before creating a new session ID and seed and launches Battle with the selected level, current bundled content identity, and exact previewed growth snapshot

#### Scenario: Return from settlement
- **WHEN** the player returns from Settlement
- **THEN** the completed session and result are cleared before Lobby becomes interactive on Home and a current growth preview is resolved

#### Scenario: Retry from settlement
- **WHEN** the player retries from Settlement
- **THEN** the app uses the same level, content version, and growth snapshot with a new session ID, a new seed, and a clean battle state

### Requirement: Integrated acceptance surfaces
The project SHALL retain all existing named battle acceptance states in the dedicated acceptance WebGL profile and SHALL provide that profile with a real acceptance sequence for Home, Activity reward, Equipment and Cultivation growth, policy preview, Battle, Settlement, return, retry, and failure-safe routing. The ordinary release profile MUST expose neither direct acceptance routing nor an acceptance bridge.

#### Scenario: Direct battle acceptance route
- **WHEN** the dedicated acceptance WebGL build loads with `acceptance=1` and `route=battle`
- **THEN** Bootstrap loads Battle with an explicit valid acceptance growth snapshot, exposes the acceptance bridge, and forwards named state requests after the battle host is ready

#### Scenario: Acceptance-shaped query reaches the ordinary release
- **WHEN** the ordinary release WebGL build loads with `acceptance=1`, `route=battle`, or another acceptance-only query value
- **THEN** Bootstrap follows the production cold-start route to Lobby Home
- **AND** no acceptance bridge, direct fixture route, named-state request, synthetic reward, or synthetic growth becomes available

#### Scenario: Existing battle states
- **WHEN** the existing battle acceptance matrix executes through the dedicated acceptance route
- **THEN** every existing screenshot, interaction, delivery, and geometry assertion still passes with a validated explicit growth snapshot

#### Scenario: Full-flow acceptance
- **WHEN** the end-to-end acceptance starts from the dedicated acceptance profile's default URL
- **THEN** it claims the starter reward once, performs one equipment or cultivation upgrade, proves the selected level's applied/suppressed preview, captures active Battle and Settlement, returns to Home, and retries with no duplicate grant, debit, session, result, black frame, or transparent frame

