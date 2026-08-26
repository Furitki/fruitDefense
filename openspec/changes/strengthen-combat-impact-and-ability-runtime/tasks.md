## 1. Contracts and Migration Baseline

- [x] 1.1 Add unified Ability activation, delivery, payload, compiled definition, and runtime-state contracts
- [x] 1.2 Replace Skill/Passive catalog DTO collections, grants, compilation, and validation with unified Ability definitions
- [x] 1.3 Migrate bundled plants, equipment reactions, enemy fixtures, projectiles, statuses, and typed equipment modifiers to Ability data
- [x] 1.4 Cache immutable resolved plant/equipment Ability loadouts and verify stable ordering and modifier match validation

## 2. Deterministic Ability Runtime

- [x] 2.1 Implement cooldown, periodic, and combat-event Ability activation through one scheduler/dispatcher path
- [x] 2.2 Preserve root-event sequence ordering, owner/priority ordering, cooldown continuation, activation-key loop guards, and finite budgets
- [x] 2.3 Implement fixed-tick windup/release/recovery and deterministic release-time target revalidation
- [x] 2.4 Implement instant and projectile deliveries whose flat payload effects resolve at actual release or impact
- [x] 2.5 Make authored delivery/impact radii govern area selection and align durian damage with its release tick
- [x] 2.6 Remove automatic direct-hit movement blocking while preserving explicitly authored freeze/control behavior
- [x] 2.7 Remove old Skill/Passive executors, DTOs, runtime states, dead trigger values, duplicate effect fields, and legacy compatibility branches

## 3. Snapshot and Determinism

- [x] 3.1 Migrate current battle snapshot serialization, restore validation, and checksum coverage to unified Ability runtime state
- [x] 3.1a Remove the obsolete pre-Ability legacy snapshot migration and its compatibility-only validation path
- [x] 3.2 Add deterministic fixtures for delayed release, retargeting, burst continuation, projectile payload, reactive cooldown, loop protection, corrected radius, and no implicit hit-stun
- [x] 3.3 Prove cached loadout reuse and remove recurring fixed-step definition cloning/sorting/allocation

## 4. Semantic Presentation Boundary

- [x] 4.1 Replace cue/visual rendering payloads with semantic ability, projectile, damage, status, resource, and defeat events
- [x] 4.2 Remove simulation-owned effect kinds, visual IDs, colors, durations, and cue-to-effect mappings
- [x] 4.3 Add a validated presentation feedback catalog with explicit concrete or no-feedback policies for all bundled semantic keys
- [x] 4.4 Extend the local feedback director with bounded reaction, VFX, floating-text merge, audio-routing, battlefield-motion, and priority records
- [x] 4.5 Preserve one-way ordered bounded delivery, snapshot exclusion, and checksum/random independence

## 5. Smooth Battle Presentation

- [x] 5.1 Expose a read-only interpolation fraction and keep previous/current render samples for enemies and projectiles
- [x] 5.2 Interpolate authoritative movement and layer presentation-only attacker/target reactions without mutating combat state
- [x] 5.3 Apply battlefield-only shake/flash while preserving portrait layout, HUD drawing, and hit-test geometry
- [x] 5.4 Provide differentiated profiles for pea, watermelon, banana, durian, sunflower, gatling, ice, chili, freeze proc, burn tick, and defeat
- [x] 5.5 Remove authoritative `Facing`, action timing, visual IDs, and other presentation mirrors after view-local replacements are active
- [x] 5.6 Add pause/1x/2x feedback-clock, merge, rate-limit, and channel-cap checks

## 6. Documentation and Validation

- [x] 6.1 Synchronize the confirmed combat, presentation, and content-production decisions in `docs/design/game-design-overview.md`
- [x] 6.2 Add or update aggregate Editor smoke coverage for Ability compilation/runtime, deterministic snapshots, semantic presentation, interpolation, and feedback budgets
- [x] 6.3 Run strict OpenSpec validation and the focused combat, snapshot, presentation, and deterministic Editor smoke suites
- [x] 6.4 Run `FruitDefense.Editor.ProjectSetup.SmokeValidate` and build WebGL with `FruitDefense.Editor.WebBuild.Build`
- [x] 6.5 Capture the real iPhone 17-aspect WebGL battle at 1x and 2x and record evidence that feedback, safe area, HUD, and hit targets pass
