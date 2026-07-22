## Why

Deterministic simulation and versioned content are not sufficient for save/resume unless every outcome-affecting runtime value has a stable serialization boundary. P0 needs a V1 battle snapshot protocol and round-trip proof before P1 attaches lifecycle or cloud storage.

## What Changes

- Define `BattleSnapshotV1` with schema, catalog/content/map identity, logical step, random state, phase, resources, wave/spawn state, and next entity ID.
- Serialize plants, pots, enemies, statuses, equipment inventory, projectiles, skill cooldowns, and pending burst state that can affect future outcomes.
- Exclude visual effects, floating text, color, transient notifications, selection, drag, modal, and inspection state.
- Restore only against the exact catalog/content version and validate IDs, references, entity uniqueness, and numeric ranges.
- Reset the frame accumulator after restore while retaining logical step and random state.
- Prove JSON round trips and continuation equivalence; do not add disk/cloud storage or automatic resume.

## Capabilities

### New Capabilities

- `battle-snapshot-v1`: Defines the versioned battle-state envelope, validation, restore behavior, and deterministic continuation contract.

### Modified Capabilities

None.

## Impact

- Adds snapshot DTOs and export/restore operations to the core battle runtime after skill migration.
- Requires a pinned compiled catalog but has no network, filesystem, UI, or platform dependency.
- Establishes the payload consumed later by local/cloud battle snapshot stores.
