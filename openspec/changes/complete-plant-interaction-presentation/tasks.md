## 1. Formation Interaction

- [x] 1.1 Add the `Swap` drop action and update pot/nursery legality so incompatible occupied destinations are legal swaps while compatible pairs still merge.
- [x] 1.2 Exchange complete plant locations through one simulation helper, preserve identity/equipment/star state, and apply active-wave cooldown/reset rules to both moved plants.
- [x] 1.3 Update drag selection, highlight, and feedback behavior for swap destinations.

## 2. Battle Presentation

- [x] 2.1 Restore a subtle always-visible plantable-cell outline grid and preserve stronger active flowerpot-placement feedback.
- [x] 2.2 Route board, nursery, and drag-ghost plant rendering through the existing full-size equipment evolution resources and remove the obsolete tiny badge path.
- [x] 2.3 Remove the temporary procedural pea trail and retain atlas-backed projectile/effect rendering.
- [x] 2.4 Upgrade the immutable attack-range texture to 1024 pixels without changing projected center or radius.

## 3. Validation and Acceptance

- [x] 3.1 Add deterministic editor smoke coverage for merge priority, cross-location swaps, cooldown rejection, evolution resource mapping, and range texture resolution.
- [x] 3.2 Run `openspec validate`, the aggregate `FruitDefense.Editor.ProjectSetup.SmokeValidate`, and focused automated tests.
- [x] 3.3 Build ordinary WebGL and capture live portrait evidence for idle grid, equipped evolution forms, sprite-backed attack effects, crisp range overlay, and direct swaps.
