## ADDED Requirements

### Requirement: Equipped plants use evolution resources
The battle presentation SHALL resolve every equipped plant to the existing full-size atlas resource for its installed equipment, while unequipped plants SHALL continue to use the resource for their plant kind. This visual resolution MUST NOT change authoritative plant identity, equipment effects, star level, or persisted state.

#### Scenario: Equipped plant renders on the battlefield
- **WHEN** a plant has a non-`None` weapon and is rendered in an occupied flowerpot
- **THEN** its dominant plant visual uses the corresponding equipment evolution atlas cell instead of the base plant plus a tiny equipment badge

#### Scenario: Equipped plant appears outside its flowerpot
- **WHEN** an equipped plant is rendered in the nursery or as an active drag ghost
- **THEN** the same equipment evolution resource is used consistently

#### Scenario: Unequipped plant renders
- **WHEN** a plant has no installed weapon
- **THEN** its visual continues to resolve from its plant-kind atlas cell

### Requirement: Transient plant attacks are resource-backed
Projectile, impact, muzzle, and status attack presentation SHALL use the authored combat VFX atlas and SHALL NOT add temporary procedural line or vector trails.

#### Scenario: Pea projectile is in flight
- **WHEN** a pea projectile is rendered between its origin and current position
- **THEN** the authored pea projectile sprite is visible and no procedural origin-to-projectile line is drawn

#### Scenario: Attack cue is consumed
- **WHEN** the battle view consumes an attack cue from the presentation-event boundary
- **THEN** it resolves the corresponding authored combat effect resource without mutating simulation state

### Requirement: Resource presentation is validated
The required editor smoke and ordinary portrait WebGL acceptance SHALL verify equipment evolution mapping and resource-backed attack presentation.

#### Scenario: Editor smoke runs
- **WHEN** the aggregate project validation executes
- **THEN** every non-empty equipment kind resolves to its designated evolution atlas cell and every unequipped plant kind retains a base resource

#### Scenario: Portrait WebGL battle is inspected
- **WHEN** equipped plants attack during a real WebGL battle capture
- **THEN** the evolved silhouettes, projectile sprites, and transient effects remain legible with no temporary vector trail
