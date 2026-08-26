## ADDED Requirements

### Requirement: Semantic combat feedback
The simulation SHALL emit ordered transient semantic events for ability phases, resolved damage, status transitions, resource grants, and entity defeats without embedding rendering kinds, asset references, durations, colors, audio data, or shake values.

#### Scenario: Direct projectile impact
- **WHEN** a projectile resolves damage against an enemy
- **THEN** one damage-resolved event identifies the logic tick, sequence, source, target, ability or projectile identity, impact position and direction, applied damage, and defeat result
- **AND** the event contains no presentation asset or lifetime policy

#### Scenario: Presentation consumer is absent
- **WHEN** identical simulations run with and without draining semantic feedback events
- **THEN** their outcome checksums and random states remain identical

### Requirement: Presentation-owned feedback profiles
The battle presentation SHALL resolve semantic combat events through a validated presentation-owned profile catalog and SHALL require every production feedback key to declare either a concrete profile or an explicit no-feedback policy.

#### Scenario: Missing production profile
- **WHEN** bundled battle content can emit a semantic feedback key that the active presentation catalog does not declare
- **THEN** aggregate validation fails before the release build

#### Scenario: Heavy and light impacts
- **WHEN** pea and durian damage events are consumed
- **THEN** their profiles produce distinct bounded reaction, VFX, floating-text, audio-routing, and battlefield-motion policies without a simulation-code branch for either plant

### Requirement: Smooth non-authoritative battle rendering
The battle view SHALL interpolate moving authoritative samples between fixed ticks and SHALL layer attacker and target reactions without writing those offsets, tints, scales, or lifetimes into battle state.

#### Scenario: Render frames outnumber logic steps
- **WHEN** three render frames occur between two 0.05-second logic samples
- **THEN** enemies and projectiles render at interpolated positions rather than repeating one position and jumping directly to the next

#### Scenario: Hit reaction expires
- **WHEN** a damage event starts a target reaction and the presentation buffer advances
- **THEN** flash, squash, and visual displacement expire locally without changing target path progress, status collection, checksum, or random state

### Requirement: Bounded dense-combat feedback
The presentation SHALL prioritize, merge, rate-limit, and cap feedback independently by channel so that dense combat and 2x speed do not create unbounded visual records, floating text, audio voices, or battlefield motion.

#### Scenario: Repeated burn ticks
- **WHEN** periodic burn damage produces multiple events inside one configured merge window
- **THEN** floating damage is aggregated and light impact/audio feedback respects its minimum interval

#### Scenario: Defeat competes with light impacts
- **WHEN** feedback capacity is saturated by light hits and an entity-defeated event arrives
- **THEN** the higher-priority defeat feedback remains visible and the channel count stays within its configured cap

### Requirement: Bundled combat feedback coverage
The shared WebGL battle SHALL provide differentiated feedback for all five bundled plants, the gatling, ice and chili equipment families, status proc, and enemy defeat while preserving portrait safe-area and control geometry.

#### Scenario: Portrait WebGL acceptance
- **WHEN** the bundled battle feedback acceptance state is captured from the real iPhone 17-aspect WebGL canvas at 1x and 2x
- **THEN** contact timing is readable, the battlefield remains inside its safe area, HUD and hit targets do not move with battlefield shake, and no fallback or missing-profile marker appears
