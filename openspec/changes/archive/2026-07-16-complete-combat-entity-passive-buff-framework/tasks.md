## 1. Runtime contracts

- [x] 1.1 Add the serializable combat-entity base, faction/capability contract, passive runtime state, and generalized status ownership
- [x] 1.2 Add finite combat attributes, modifier operations, polarity/tags, effective-attribute resolution, and deterministic status removal

## 2. Content and compilation

- [x] 2.1 Extend catalog DTOs and canonicalization with passive definitions, passive grants, status modifiers, and periodic metadata
- [x] 2.2 Compile and validate every new trigger, owner role, target, attribute, modifier, polarity, and reference before Battle starts
- [x] 2.3 Migrate ice/chili on-hit and producer-opening equipment behavior from granted skills to first-class passives

## 3. Simulation integration

- [x] 3.1 Resolve plant damage/range/attack interval and enemy movement through the shared attribute pipeline
- [x] 3.2 Implement fixed-step generalized status apply, stack, periodic tick, expiry, control, and legacy-view synchronization for both factions
- [x] 3.3 Implement ordered passive event dispatch, cooldowns, target selection, shared effects, root-event loop protection, and activation budgets

## 4. Persistence and deterministic state

- [x] 4.1 Extend resolved-level snapshot V2 with atomic entity-runtime sidecar export/restore while keeping snapshot V1 compatible
- [x] 4.2 Include generalized entity statuses, passive runtime state, tick progress, and event sequence in deterministic checksums

## 5. Validation

- [x] 5.1 Add a framework smoke covering shared entity lookup, positive plant buffs, expiry, stacking, removal filters, enemy passives, and loop protection
- [x] 5.2 Extend composable-skill and snapshot V1/V2 fixtures to prove current ice/chili/projectile outcomes and restored continuation
- [x] 5.3 Run strict OpenSpec validation, the focused framework smoke, P0 and P1 Unity gates, and a WebGL build regression
- [x] 5.4 Keep the stable game-design overview unchanged unless the user explicitly authorizes synchronization, and record the final documentation status
