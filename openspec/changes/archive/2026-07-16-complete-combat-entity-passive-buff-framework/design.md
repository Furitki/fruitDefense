## Context

`GameSimulation` already runs at a deterministic 20 Hz and compiles a finite trigger/target/effect catalog. The remaining asymmetry is structural: `Plant` owns skill runtimes, `Zombie` owns statuses, equipment-triggered behavior is represented as ordinary skills, and attribute changes are read directly from concrete fields. Adding future positive buffs, enemy abilities, summons, traps, or passive reactions would therefore multiply concrete-type branches and snapshot exceptions.

The framework must stay compatible with WebGL/IL2CPP, Unity serialization, the current immediate-mode presentation, existing stable content IDs, and the one-way presentation-event boundary. The working tree also contains an in-progress resolved-level and snapshot-V2 path, so the change must extend that work rather than replace it.

## Goals / Non-Goals

**Goals:**

- Give plants and enemies one serializable combat-entity base contract with stable identity, faction, runtime skills, runtime passives, and statuses.
- Resolve combat attributes through one deterministic pipeline rather than mutating base definition data or concrete fields.
- Compile passive definitions separately from active/polled skills and dispatch them through bounded combat events.
- Make buffs and debuffs target either faction, support deterministic stacking/expiry/periodic ticks, and expose finite attribute modifiers.
- Migrate current ice/chili equipment reactions and opening slow without changing P0 outcomes.
- Continue all future-affecting entity runtime state through the resolved-level snapshot path and outcome checksums.

**Non-Goals:**

- ECS, reflection, arbitrary scripts, nested effect graphs, behavior trees, or hot-loaded code.
- A full RPG attribute list, arbitrary formula language, network-authoritative combat, or presentation changes.
- New player-facing balance, new equipment, or new battle controls.
- Giving plants hit points or enemy attacks in this change; the common contract allows those later without claiming they exist now.

## Decisions

### 1. Serializable base entity, not ECS

`CombatEntityState` is an abstract serializable state owner inherited by `Plant` and `Zombie`. It owns `Id`, `ContentId`, `Faction`, `SkillRuntimes`, `PassiveRuntimes`, and `Statuses`; concrete types retain only their domain fields such as pot placement, star tier, health, and route progress.

This keeps existing call sites and Unity serialization straightforward while removing ownership asymmetry. An interface-only adapter was rejected because it would leave lists duplicated on each concrete type. ECS was rejected because the battle is small, deterministic, and already has stable snapshot DTOs; introducing a package and entity migration would be disproportionate.

### 2. Finite attribute resolver

The runtime exposes `CombatAttributeKind` for the attributes currently needed by reusable mechanics: damage, attack interval, range, move speed, damage taken, and resource gain. Base values come from the compiled definition, star tier, and concrete state; active statuses contribute `Flat`, `Additive`, or `Multiplicative` modifiers in `(status sequence, modifier index)` order. Final clamps are owned by the resolver.

Definitions never mutate, and effective values are computed on demand because current entity counts are small. A free-form string dictionary and arbitrary expression evaluator were rejected because misspellings and evaluation-order drift would weaken validation and replay stability.

### 3. Passives are event subscriptions, not zero-cooldown skills

`PassiveDefinitionDto` compiles to a `CompiledBattlePassive` containing a supported combat event, owner role filter, target selector, priority, optional cooldown, and the existing finite effect list. Plants, enemies, and equipment grant passive IDs independently from skill IDs.

The dispatcher processes owners by entity ID, then passive priority and ID. A root event carries a monotonic sequence; the same `(root sequence, owner, passive)` activation is allowed once, and a fixed activation budget aborts invalid recursive content. This makes reactions deterministic and prevents status/damage feedback loops. Polling skills remain responsible for cooldown and periodic actions; passives react to `BattleStarted`, `WaveFirstSpawned`, `AfterDamageDealt`, `AfterDamageTaken`, `StatusApplied`, and `EntityDefeated`.

Reusing ordinary skills for passives was rejected because it conflates polling state with event state, makes ownership filters implicit, and provides no loop boundary.

### 4. One status model for buffs, debuffs, counters, and control

`StatusDefinitionDto` gains polarity, tags, control flags, attribute modifiers, and periodic effect metadata. Existing stacking modes remain and a deterministic additive-stack mode is added. A status instance remains source-attributed and sequence-ordered; duration and tick progress use fixed ticks.

Periodic effects fire only when the compiled interval elapses. Chili burn stores damage-per-second magnitude and converts it to interval damage, preserving its current result while making `tickInterval` real. Slow is expressed as a move-speed multiplier; freeze and hit stun use a movement-block control flag; hit-count remains a bounded proc counter. Positive buffs use the same instance and modifier pipeline.

Separate `Buff` and `Debuff` runtime classes were rejected because stacking, source, expiry, snapshot, and dispel behavior would be duplicated. Polarity is metadata, not a different execution model.

### 5. Generic effect context with bounded receivers

Skill and passive execution share a `CombatEffectContext` containing owner, event source/target, origin, range, and event magnitude. Target selection returns combat entities. Each finite effect executor validates receiver capability: projectile launch still requires a plant source and enemy target, damage currently requires an enemy target, while status and cue effects work for either entity faction.

Unsupported source/receiver combinations fail catalog validation when statically knowable and otherwise are deterministic no-ops with no presentation event. This preserves current behavior while keeping the external execution boundary reusable.

### 6. Snapshot V1 remains frozen; resolved-level V2 carries a runtime sidecar

Legacy snapshot V1 keeps its existing plant-skill/enemy-status shape. Snapshot V2 gains an entity-runtime sidecar keyed by entity ID containing passive cooldowns and generalized statuses for both plants and enemies. Restore builds the normal candidate first, validates the sidecar atomically against the resolved catalog and global entity identities, then commits.

Older V2 payloads without the sidecar derive the existing runtime state and initialize passive cooldowns to zero. This avoids silently changing the V1 protocol while preserving future outcomes for the active P1 path.

## Risks / Trade-offs

- **[Base-class migration breaks old field access]** -> Keep field names and concrete types stable; cover compile, V1 snapshot, V2 snapshot, and P0 regressions.
- **[Passive recursion hangs the simulation]** -> Root-event activation keys plus a fixed dispatch budget fail fast with a content/runtime error.
- **[Modifier order changes replay results]** -> Sort by status sequence and authored modifier order; include resolved runtime state in deterministic checksums.
- **[Positive buffs exist only on paper]** -> Add a data-only smoke fixture that applies a plant damage buff and proves expiry restores the base value.
- **[V2 sidecar disagrees with legacy fields]** -> Treat the sidecar as authoritative only after full validation and reject duplicate/missing entity runtime entries.
- **[Broad refactor obscures current user changes]** -> Add focused files and edit only the overlapping combat/catalog/snapshot seams.

## Migration Plan

1. Add shared entity, attribute, passive, and status runtime types without changing bundled content behavior.
2. Extend DTO compilation and validation; migrate equipment reactions from granted skills to granted passives.
3. Route attribute reads, status ticking, and passive events through the new services while keeping legacy status view fields synchronized.
4. Add V2 entity-runtime snapshot continuation and deterministic checksum coverage while retaining V1 compatibility.
5. Run dedicated framework smoke, composable-skill parity, snapshot V1/V2, P0, P1, and strict OpenSpec validation.

Rollback removes the new change files and runtime types, restores the prior concrete state ownership and equipment skill grants, and leaves content IDs and V1 snapshots untouched.

## Open Questions

- Whether to synchronize the stable game-design overview is awaiting explicit user confirmation; implementation and change-local design do not imply that authorization.
- Enemy attacks, plant durability, active abilities, and a player-facing buff UI remain separate future changes built on this runtime contract.
