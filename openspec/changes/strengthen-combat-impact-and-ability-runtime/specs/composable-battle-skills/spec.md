## MODIFIED Requirements

### Requirement: Finite skill composition
The simulation SHALL execute combat capabilities as validated unified Ability definitions with supported activation, owner role, target, delivery, projectile, and payload-effect kinds and SHALL reject unknown mechanism kinds before Battle starts. Separate Skill and Passive execution definitions SHALL NOT remain after migration.

#### Scenario: Existing mechanism combination
- **WHEN** a test plant combines an existing activation, target, projectile delivery, damage payload, and status payload
- **THEN** it runs without adding or changing simulation executor code

#### Scenario: Unknown delivery or effect
- **WHEN** a catalog contains a Delivery or payload Effect kind with no supported executor
- **THEN** catalog compilation fails with a structured validation error

### Requirement: Current projectile behaviors
The Ability runtime MUST reproduce Tracking, TimedArc, and LinearReturn projectile behavior using fixed-step timing, and projectile payloads MUST resolve only when the supported delivery reaches an eligible impact.

#### Scenario: Tracking pea
- **WHEN** a pea projectile loses its target before impact
- **THEN** it deterministically retargets according to the current frontmost-target rules and applies its payload only to the resolved impact target

#### Scenario: Timed watermelon arc
- **WHEN** a watermelon launches
- **THEN** it resolves its stored impact point after the authored fixed-tick flight and applies its area payload using the authored blast radius

#### Scenario: Banana return
- **WHEN** a banana completes its outward and return paths
- **THEN** each eligible target is hit at most once per path and each collision resolves the configured payload once

### Requirement: General status instances
Slow, freeze, hit-count, burn, and explicitly authored gameplay control SHALL use general status definitions and instances rather than plant-, equipment-, or presentation-specific state fields. Ordinary direct-hit reaction SHALL NOT add a movement-blocking status.

#### Scenario: Fifth ice hit
- **WHEN** an enemy receives its fifth valid ice-count hit
- **THEN** the count clears and the configured freeze status is applied

#### Scenario: Burn cap
- **WHEN** more than three burn applications would remain active on one enemy
- **THEN** no more than three independent burn instances are retained according to the deterministic replacement rule

#### Scenario: Ordinary damage reaction
- **WHEN** a direct attack damages an enemy without an authored control payload
- **THEN** no movement-blocking status is added and visual hit reaction remains presentation-only

### Requirement: Equipment grants and modifiers
Equipment SHALL grant unified abilities and apply validated finite typed modifiers selected by stable ability ID or tag, and catalog validation MUST prevent silent zero-match, ambiguous match, and unsupported attribute behavior.

#### Scenario: Machine-gun equipment
- **WHEN** machine-gun equipment modifies a ranged-projectile ability
- **THEN** the ability emits four shots separated by 0.2 seconds through one cached resolved loadout

#### Scenario: Producer equipment combinations
- **WHEN** ice or pepper equipment is installed on a producer-tagged plant
- **THEN** the catalog-granted event or periodic ability reproduces the opening slow or resource bonus without plant-ID branches

### Requirement: Current battle parity
All five plants, three equipment types, fifteen waves, milestone rewards, and existing drag/move/merge interactions MUST retain their current rules except for the explicitly authored release timing, effective area radius, and removal of automatic direct-hit movement blocking defined by this change.

#### Scenario: Existing acceptance suite
- **WHEN** Unity smoke and WebGL battle acceptance run after migration
- **THEN** all unaffected assertions and visual interactions pass and updated assertions prove the three intentional rule corrections

## ADDED Requirements

### Requirement: Authoritative ability release timing
Outcome effects SHALL resolve on fixed authored release ticks and the runtime SHALL deterministically revalidate or retarget invalid targets at release.

#### Scenario: Durian landing
- **WHEN** durian begins an attack against enemies in its authored area
- **THEN** no damage resolves before the release tick and damage, the heavy impact event, and visible landing share that tick

### Requirement: Cached resolved ability loadouts
The compiled content catalog SHALL cache immutable plant/equipment ability loadouts and the fixed-step combat path SHALL NOT clone, sort, or recompile definitions on every tick.

#### Scenario: Repeated fixed steps
- **WHEN** a stable plant/equipment combination executes across repeated fixed steps after warm-up
- **THEN** it reuses the same ordered compiled loadout and does not allocate a replacement definition collection
