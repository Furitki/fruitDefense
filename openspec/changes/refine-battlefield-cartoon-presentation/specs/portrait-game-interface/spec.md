## ADDED Requirements

### Requirement: Frameless readable enemy presentation

Battlefield enemies SHALL render their character art at a projection-scaled footprint larger than the former 48-logical-point sprite, SHALL NOT render an opaque square backplate or outer square border, and SHALL retain readable health and active combat-status feedback inside the portrait battlefield and safe area.

#### Scenario: Normal enemy enters the route

- **WHEN** a normal enemy is visible during an active wave
- **THEN** its enlarged character art is visible directly over the route without the former brown square fill or dark square border

#### Scenario: Enemy has active status effects

- **WHEN** an enemy is frozen, slowed, burning, or in hit stun
- **THEN** the relevant tint or effect and the centered health bar remain readable without reintroducing an opaque backplate

#### Scenario: Dense wave is captured in portrait WebGL

- **WHEN** the real release WebGL canvas is captured at the 402 by 874 reference viewport during a representative active wave
- **THEN** enlarged enemies remain inside the battlefield, preserve route readability, and do not cover or displace the safe-area-aware control surface
