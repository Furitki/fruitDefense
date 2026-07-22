## 1. Skill Runtime

- [x] 1.1 Add validated Trigger, Target, Effect, projectile-mode, status-instance, skill-runtime, and tag-modifier types
- [x] 1.2 Compile authored seconds to fixed ticks and reject unknown mechanisms, invalid references, and ambiguous modifier matches
- [x] 1.3 Implement explicit target selection and Effect executors with deterministic ordering

## 2. Current Mechanic Migration

- [x] 2.1 Migrate pea, watermelon, banana, durian, and sunflower base skills
- [x] 2.2 Migrate Tracking, TimedArc, and LinearReturn projectile behavior
- [x] 2.3 Migrate slow, stun, ice-count/freeze, and capped independent burn instances
- [x] 2.4 Migrate machine-gun, ice, pepper, and producer equipment combinations through grants/tags
- [x] 2.5 Migrate wave milestone rewards and remove the corresponding hard-coded wave branches
- [x] 2.6 Remove migrated plant/equipment special-case branches after parity tests pass

## 3. Compatibility and Validation

- [x] 3.1 Adapt current UI/presentation lookups through stable IDs, visual IDs, and cue IDs
- [x] 3.2 Add parity checks for all current plants, equipment, projectiles, statuses, and milestone rewards
- [x] 3.3 Add a data-only test plant proving existing mechanisms compose without simulation-code changes
- [x] 3.4 Run OpenSpec validation, deterministic smoke, Unity project smoke, WebGL build, and existing 13-state acceptance
