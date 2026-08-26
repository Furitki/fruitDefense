## Context

The battle currently advances authoritative state in fixed 0.05-second ticks and emits transient cue/text events. That separation is sound, but `GameSimulation` still decides presentation kinds and lifetimes, while `FruitDefenseGame` renders entity and projectile state without interpolation. Attack animation fields and visual IDs also remain in authoritative model objects.

The content model has separate Skill and Passive DTO/compiler/runtime paths even though both execute the same finite effects. Event trigger values remain in the Skill enum without a scheduler, area effect radii are not consumed by the damage executor, and resolved plant/equipment combinations are cloned and sorted in fixed-step hot paths. The change must preserve deterministic ordering and snapshot continuation while deliberately changing two current rules: ordinary direct hits no longer stop movement, and delayed attacks resolve at their authored release tick.

## Goals / Non-Goals

**Goals:**

- Provide smooth, bounded, presentation-only combat feedback on the shared WebGL baseline.
- Keep every outcome-changing timer, target, projectile, effect, status, and reactive activation deterministic.
- Replace Skill and Passive execution with one finite Ability model and one runtime path.
- Align visible contact with authoritative resolution and make authored area radii effective.
- Remove presentation-only state from authoritative entities and recurring content-resolution allocation from hot loops.
- Keep the immediate-mode battle surface, portrait safe area, and existing input geometry intact.

**Non-Goals:**

- Critical hits, gameplay knockback, new plants, new equipment, enemy attacks, arbitrary scripts, nested behavior graphs, ECS, Cinemachine, Addressables, or platform haptics.
- Increasing the authoritative simulation frequency above 20 Hz.
- Preserving old Skill/Passive catalog or runtime-state compatibility after migration.
- Allowing missing production feedback profiles to fall back silently.

## Decisions

### 1. One finite Ability definition replaces Skill and Passive definitions

An ability contains a stable ID, activation definition, owner-role filter, deterministic priority/cooldown, fixed-tick windup/release/recovery timing, burst data, tags, and one or more deliveries. A delivery selects targets, optionally travels through a supported projectile mode, and carries a flat payload of supported effects.

Cooldown/periodic activations are polled in stable entity/ability order. Combat-event activations use the existing monotonic root event, owner ordering, priority ordering, activation-key guard, and finite activation budget. This retains bounded behavior without two executors.

Alternative considered: keep Skill and Passive DTOs and share only `ExecuteEffects`. Rejected because it retains duplicated validation, grant, snapshot, and scheduling semantics and leaves dead trigger values in the public schema.

All migrated definitions receive canonical `ability.*` IDs. Base attacks use `ability.plant.<plant>.attack`, production uses `ability.plant.sunflower.produce`, and equipment reactions use `ability.equipment.<equipment>.<behavior>`. The former `skill.*` and `passive.*` IDs are removed rather than aliased. Presentation profiles and fixtures reference the canonical Ability IDs.

### 2. Delivery owns target/radius/projectile; payload owns outcome effects

`Damage`, `ApplyStatus`, and `GrantResource` remain the finite payload effects. Presentation cue emission is not an outcome effect. Instant delivery resolves its payload at the release tick. Projectile delivery resolves payload at an actual collision or stored impact point. Area selection reads the delivery radius; it never substitutes plant range for an authored radius.

Alternative considered: add more optional fields to `SkillEffectDefinitionDto`. Rejected because projectile, target, radius, cue, and effect identity already have multiple competing sources of truth.

### 3. Semantic presentation events contain results, not rendering policy

The simulation emits transient events such as ability started/released, projectile launched, damage resolved, status applied/procced, entity defeated, resource granted, and wave feedback. Events contain stable gameplay identities, tick/sequence, source/target IDs, position/direction, resolved magnitude, and result flags as applicable.

The simulation does not emit `CombatEffectKind`, asset IDs, colors, durations, shake values, audio clips, or floating-text lifetimes. A presentation-owned catalog maps semantic keys such as ability/release, projectile/impact, status/proc, and enemy/defeated to feedback profiles. Production validation requires either an explicit profile or an explicit `None` policy.

Alternative considered: keep cue IDs and move only the switch. Rejected because cue IDs duplicate gameplay identities and make start/release/impact/status phases difficult to validate consistently.

### 4. Presentation feedback is layered, interpolated, and budgeted

The battle host retains previous/current render samples for moving enemies and projectiles and interpolates them using a read-only accumulator fraction. Presentation reactions then add non-authoritative offsets/tint/scale on top of interpolated positions. Battlefield shake offsets only the battlefield rendering transform; HUD layout and hit-test geometry remain unchanged.

Feedback profiles independently configure attacker motion, target reaction, VFX, floating text, audio routing, battlefield motion, priority, merge window, minimum interval, and maximum concurrent instances. Event ordering and event sequence drive deterministic visual variation without consuming the battle RNG.

Combat feedback follows battle pause and speed policy; shell/UI feedback continues to use unscaled time. At 2x, rate limits and merging prevent doubled event frequency from doubling clutter.

### 5. Ordinary hit reaction is not gameplay control

`DamageEntity` no longer automatically applies `status.combat.hit-stun`. Target flash, squash, and short directional displacement are owned by presentation. Freeze and other explicitly authored control statuses continue to block movement through deterministic status rules.

This is an intentional balance change, not a visual refactor. Deterministic fixtures and bundled balance expectations are updated accordingly.

### 6. Release timing is authoritative

Windup and release are stored in fixed ticks. Targets are revalidated at release; front-target abilities deterministically retarget using the current ordering if the original target is invalid. Durian damage and its heavy impact event resolve on the same release tick. Cooldown-start policy is uniform and documented in the Ability runtime.

Cooldown starts when an activation is accepted and windup begins, preserving authored attack cadence; recovery is an independent activation gate. Durian uses a 0.4-second windup and 0.3-second recovery inside its existing 0.7-second action, so its authoritative area payload resolves at the visible landing without extending the authored 1.8-second cadence.

Alternative considered: play anticipation after immediate damage. Rejected because a visible landing after HP loss cannot produce credible contact feedback.

### 7. Compiled loadouts are immutable and cached

The compiled catalog caches resolved ability arrays by stable `(plantId, equipmentId)` key. Equipment modifiers are compiled once using finite typed attributes and validated match cardinality. Entity runtime state stores only outcome-relevant ability IDs/ticks/burst progress; presentation fields are excluded.

Hot fixed-step paths use stable arrays and reusable buffers instead of cloning definitions and constructing LINQ collections every tick. Deterministic ordering is explicit and covered by tests.

### 8. Migration replaces rather than adapts

Bundled DTOs, compilation, validation, runtime, snapshot fields, tests, and editor authoring migrate in one change. Once all bundled abilities run through the new path, old Skill/Passive DTOs, compilers, runtime states, legacy view mirrors, and cue mappings are deleted. No old-schema loader or dual execution flag is added.

Outcome-relevant pending Ability state includes phase, remaining windup/recovery/cooldown/burst ticks, pending source/target IDs, event magnitude and root sequence where applicable. Projectile state stores canonical Ability ID and delivery index so impact can recover its pinned compiled payload. These fields enter every constructor path's checksum and current resolved-level snapshot; presentation samples and event delivery remain excluded.

The obsolete pre-Ability legacy snapshot migration is removed instead of translating old Skill/Passive arrays into Ability state. Exact content pinning remains the supported boundary: an old content identity is unavailable to the new runtime rather than silently upgraded.

## Risks / Trade-offs

- **[Snapshot and checksum regressions]** → Update the current snapshot schema and deterministic fixtures in the same migration; compare uninterrupted/restored runs for active cooldown, delayed release, burst, projectile, and event activation.
- **[Reactive ability loops]** → Retain root-event activation keys, stable priority ordering, and the existing finite budget.
- **[Visual feedback changes gameplay accidentally]** → Keep reaction state exclusively in the presentation buffer and assert that consuming/advancing/clearing feedback cannot change checksum or random state.
- **[Delayed release changes current balance]** → Treat release ticks and removal of automatic micro-stun as explicit bundled-content changes and synchronize the design overview.
- **[High-density feedback overload]** → Merge repeated numbers, rate-limit light audio/impacts, cap each channel, and retain heavy/control/defeat priorities.
- **[Immediate-mode renderer becomes a bottleneck]** → Reuse existing sprite drawing in the first slice, pool transient records, avoid per-frame allocations, and defer a renderer replacement until evidence requires it.
- **[Concurrent edits overlap existing UI work]** → Keep draw/hit geometry unchanged, isolate battlefield presentation helpers, and inspect existing diffs before integration.

## Migration Plan

1. Add semantic event contracts, feedback catalog/buffer, interpolation exposure, and a complete pea vertical slice without changing outcome state.
2. Introduce Ability DTO/compiler/runtime and migrate bundled plants/equipment/enemy passives plus snapshots and deterministic tests.
3. Switch projectile and instant damage to delivery payload resolution; apply radius and release timing; remove automatic hit-stun.
4. Migrate all battle feedback profiles and remove simulation-owned visual mappings and authoritative animation mirrors.
5. Delete the old Skill/Passive definitions, compilers, runtime paths, and obsolete content fields.
6. Synchronize stable design documentation, run OpenSpec/Editor/WebGL validation, and capture the portrait battle surface.

Rollback is source-level only: revert the complete change. Runtime compatibility with partially migrated or old catalogs is intentionally unsupported.

## Open Questions

- Concrete audio assets are not currently present; the runtime routing/profile contract can be accepted with silent entries until approved clips are supplied.
- Final per-profile motion amplitudes and concurrency budgets should be tuned from the real WebGL portrait capture rather than inferred only from Editor rendering.
