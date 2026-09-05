## MODIFIED Requirements

### Requirement: Versioned non-economic profile
The project SHALL use one current player profile schema containing identity/revision metadata, settings, last selected level, shell preferences, item balances, activity receipts, owned outgame growth-equipment and loadout, and cultivation ranks; obsolete profile schemas SHALL NOT be interpreted or migrated by the runtime.

#### Scenario: New player
- **WHEN** no valid local profile exists
- **THEN** the store returns a valid current default profile with a new profile ID, `orchard-01` as the default level, and normalized empty progression collections

#### Scenario: Unsupported schema
- **WHEN** stored JSON declares `PlayerProfileEnvelopeV1` or another unsupported schema
- **THEN** loading returns a structured incompatibility result without interpreting, migrating, or partially copying it into the current schema

#### Scenario: Current progression profile
- **WHEN** a current profile contains valid catalog-backed balances, receipts, ranks, and loadout entries
- **THEN** the store round-trips the complete aggregate without loss or reordering-dependent behavior

## RENAMED Requirements

- FROM: `Versioned non-economic profile`
- TO: `Versioned progression-capable profile`
