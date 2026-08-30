## Context

Battle content already has stable IDs, serializable DTOs, validation, deterministic canonical JSON, and compiled read-only indexes. However, `BundledBattleContentFactory` remains the production source, the Editor rebuild command overwrites the authored asset from that factory, and runtime level compilation calls the factory directly. Global star tiers, nursery selection constraints, movement cooldown, and presentation-ID switches also make content identities behave as code branches.

The implementation must preserve deterministic simulation and the existing `Bootstrap → Lobby → Battle → Settlement` flow. It must not introduce remote code execution, runtime ScriptableObject mutation, silent fallback catalogs, or a second combat executor system.

## Goals / Non-Goals

**Goals:**

- Make one manifest-selected exported bundle the authoritative production input.
- Keep authoring modular and typed while flattening it into one deterministic runtime DTO.
- Allow two fruit definitions to share one presentation identity while retaining different base stats, abilities, upgrade profiles, and nursery weights.
- Move maximum star, tier multipliers, nursery guarantees/caps, pot chance, and relocation cooldown into validated configuration.
- Make every affected simulation and presentation consumer resolve configuration by stable ID.
- Preserve current bundled gameplay values and deterministic outcomes for the original roster.

**Non-Goals:**

- Remote gameplay-content delivery or runtime hot reloading.
- Arbitrary scripts, reflection-driven effects, inheritance chains, or free-form override dictionaries in content.
- New player-facing fruit variants in the bundled nursery pool; the capability is proven with isolated Editor test data first.
- Long-term progression, economy, activities, platform SDK configuration, or a redesign of the runtime UI visual system.

## Decisions

### One root manifest selects exported immutable modules

Add a versioned `GameContentManifest` authoring asset and deterministic manifest JSON. The manifest pins the battle catalog resource path, content catalog ID/version, level catalog ID, presentation catalog ID, and default nursery profile. Bootstrap loads the manifest and battle JSON, validates their identities together, compiles read-only indexes, and passes the result into existing level resolution.

This is preferred over expanding `RuntimeConfigV1`: runtime deployment/channel policy and gameplay content have different ownership and validation. It is also preferred over loading many runtime ScriptableObjects because current content specs require portable deterministic data and no authoring-asset dependency in simulation.

### Domain arrays remain flattened in the runtime catalog

The first implementation keeps plants, enemies, equipment, abilities, projectiles, statuses, waves, upgrade profiles, and nursery profiles as separate typed collections inside one portable catalog DTO. Authoring and validation are modular by collection, but export produces one canonical JSON document.

This avoids a premature per-definition asset graph while removing the production code factory as authority. The root manifest allows later physical asset splitting without changing simulation consumers.

### Fruit identity and presentation identity are separate

Each plant, enemy, equipment, and projectile definition carries a required stable `presentationId`. Rendering resolves the definition and then the finite presentation archetype from that field. A second plant can therefore reuse `visual.plant.pea` without being treated as `plant.pea` by gameplay.

Unknown or missing presentation IDs fail bundled validation. There is no generic production fallback. Finite render archetypes remain code because they are supported rendering mechanisms, not balance content.

### Upgrade profiles replace the global star table

Each plant references one `upgradeProfileId`. An upgrade profile owns an ordered contiguous tier list; each tier contains damage, attack-speed, and range multipliers. Merge eligibility remains exact-definition-ID plus equal tier, and the profile determines whether a next tier exists.

This is simpler and less ambiguous than base-definition inheritance or cross-variant merge families. Cross-variant merging would require an explicit result-definition contract and is not introduced here.

### Nursery profiles own draw policy

Each level rule set references one nursery profile. The profile owns weighted plant entries, pot chance, the first-refresh guaranteed tag/count, and one per-refresh tag cap. The deterministic random generator performs all weighted selection and shuffling.

Plant definitions do not own global draw weights because the same plant may have different availability or weight in different modes later. The initial schema uses one guarantee and one cap instead of a general constraint language.

### Configuration is frozen for a battle

Manifest identity, battle catalog identity/version, and resolved level identity are validated before launch. Resolved upgrade/nursery data are included in the gameplay source identity used by deterministic checks. A running battle never observes asset or JSON changes.

## Risks / Trade-offs

- [Serialized schema replacement can invalidate the existing authored asset] → Bump the catalog schema/content version and rebuild the committed asset and JSON once from current values.
- [Factory removal can break many tests that use it as a fixture] → Keep test fixture construction under `Assets/Editor/Tests/Fixtures` or explicit test helpers; production runtime and exporter must not call it.
- [Weighted nursery selection can change random consumption] → Reproduce the current bundled profile ordering and add fixed-seed parity tests before replacing the old loop.
- [Presentation IDs add a second identifier] → Validate every production definition and centralize finite visual-ID resolution; definitions never derive visuals from gameplay ID text.
- [Immediate-mode UI still has fixed slot/card geometry] → This phase only removes duplicated refresh-cost and plant visual assumptions. Dynamic equipment and lobby collections remain a later scoped change.

## Migration Plan

1. Extend schema DTOs, canonicalization, compiler indexes, validation, and deterministic source identity.
2. Populate current values in the authored battle asset and export schema-versioned JSON.
3. Add and export the root manifest, then switch bundled level compilation to the manifest loader.
4. Convert simulation upgrade, merge, nursery, movement cooldown, and refresh UI consumers.
5. Convert presentation lookup to definition-owned presentation IDs.
6. Remove obsolete production factory/default formula paths and update tests to explicit fixtures.
7. Run catalog validation, aggregate Editor smoke, deterministic snapshot tests, and ordinary WebGL build validation.

There is no in-product compatibility or fallback path. Rollback is performed only by reverting the complete change and rebuilding the previous release artifact.

## Open Questions

- Whether a later design should allow two variants to merge across definition IDs and what definition results from that merge.
- Whether nursery profiles should eventually be selected by level rules only or by a future pre-battle loadout; this phase uses level rules only.

