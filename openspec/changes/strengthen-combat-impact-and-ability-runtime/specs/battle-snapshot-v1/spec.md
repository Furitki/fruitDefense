## MODIFIED Requirements

### Requirement: Versioned snapshot envelope
The current resolved-level battle snapshot SHALL identify schema, catalog, content, map, and level versions and SHALL contain every simulation value that can affect future battle outcomes, including unified Ability runtime phase, cooldown, windup, recovery, burst, pending event context, root sequence, and projectile Ability/delivery identity.

#### Scenario: Export during active effects
- **WHEN** a snapshot is exported while an Ability is winding up or recovering, a reactive cooldown is active, burst shots are pending, or a projectile payload is in flight
- **THEN** all pending outcome-affecting values required for identical continuation are present in the snapshot

#### Scenario: Presentation exclusion
- **WHEN** a snapshot is exported while interpolation, attacker/target reaction, battlefield motion, floating text, audio routing, or temporary effects are active
- **THEN** those presentation-only values are absent

### Requirement: Deterministic round-trip continuation
Snapshot JSON round trips MUST preserve deterministic continuation across Ready, Playing, and BetweenWaves phases for every simulation constructor path.

#### Scenario: Branch continuation
- **WHEN** one branch continues for N fixed steps and another exports, JSON-round-trips, restores, and continues for N fixed steps
- **THEN** both branches have the same outcome-state checksum, including unified Ability and root-event state

#### Scenario: Mid-effect continuation
- **WHEN** the branch point includes delayed release, retarget context, projectile delivery, burn, slow, ice count, event-activated cooldown, or machine-gun burst
- **THEN** restored continuation matches the unsaved branch

### Requirement: Layer-aware gameplay map identity
Snapshot and deterministic continuation validation SHALL identify a battlefield's gameplay definition from its dimensions, compiled gameplay cells/collision channels, ordered routes, gameplay marker groups, gameplay markers, and gameplay-affecting references in canonical order, and SHALL exclude semantic visual surfaces, terrain palettes, sprites, and other presentation-only values from gameplay state identity without weakening exact catalog/content pinning. The current runtime SHALL NOT translate a pre-Ability snapshot's separate Skill/Passive state into unified Ability state.

#### Scenario: Presentation-only map change
- **WHEN** a matching supported catalog changes only a map's semantic surface layout or registered terrain palette while retaining identical gameplay topology and markers
- **THEN** the gameplay map fingerprint and deterministic outcome-state checksum are unchanged

#### Scenario: Gameplay topology changes
- **WHEN** a cell capability, collision channel, ordered route, spawn, goal, core, or initial-pot marker changes
- **THEN** the gameplay map fingerprint differs and restore cannot treat the changed gameplay map as the original definition

#### Scenario: Pre-Ability snapshot is supplied
- **WHEN** a snapshot contains separate legacy Skill or Passive runtime state rather than the current Ability state
- **THEN** restore rejects the unsupported schema/content identity without translating or partially mutating the active simulation
