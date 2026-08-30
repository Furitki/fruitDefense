## 1. Schema and compilation

- [x] 1.1 Add manifest DTO/asset contracts and deterministic manifest validation/serialization.
- [x] 1.2 Replace global star tiers with plant-referenced upgrade profiles and add nursery profiles plus presentation identities to the battle schema.
- [x] 1.3 Extend canonicalization, structured validation, compiled indexes, and resolved gameplay identity for the new definitions.

## 2. Authoring and runtime source

- [x] 2.1 Convert the bundled battle authoring asset and JSON to the new schema while preserving current active gameplay values.
- [x] 2.2 Add the bundled game-content manifest asset/JSON and Editor export/validation workflow.
- [x] 2.3 Load manifest-selected bundled JSON for production level compilation and remove the production factory/default-config source path.

## 3. Gameplay and presentation consumers

- [x] 3.1 Resolve plant statistics, attack timing, maximum tier, and merge legality through each plant's upgrade profile.
- [x] 3.2 Resolve deterministic nursery generation, pot chance, tag guarantees/caps, relocation cooldown, and refresh cost from active profiles/rules.
- [x] 3.3 Resolve plant, enemy, equipment, and projectile visuals through configured presentation identities without gameplay-definition-ID branches.
- [x] 3.4 Make player-facing refresh cost and plant inspection consume the same resolved gameplay configuration as simulation.

## 4. Validation and acceptance

- [x] 4.1 Add Editor tests for manifest mismatches, invalid profile references, deterministic nursery replay, configured cooldown/tier limits, and a shared-visual fruit variant.
- [x] 4.2 Update existing content/snapshot/interaction tests and fixtures for the schema replacement and removal of exact-roster assumptions.
- [ ] 4.3 Run OpenSpec validation, content validation, aggregate Unity Editor smoke, deterministic tests, and the ordinary WebGL build gate.
  - OpenSpec strict validation, modular config/content/determinism gate, and ordinary WebGL release build pass.
  - Aggregate P0 smoke currently stops at the unrelated `CombatFeedbackSdfRenderSmoke` role-route label-admission assertion.
