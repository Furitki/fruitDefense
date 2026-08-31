## 1. Production terrain assets

- [x] 1.1 Publish byte-identical approved 64×64 grass and soil textures under a production-owned first-level terrain folder with deterministic Unity import settings.
- [x] 1.2 Create and validate `palette.orchard-01.square-grid` using the production base textures and the complete existing production binding set.

## 2. First-level catalog integration

- [x] 2.1 Add an explicit canonical single-route visual-composition choice and build `orchard-01` as 35 base-only grass cells plus 21 base-only soil cells.
- [x] 2.2 Bind only `theme.orchard-01.day` to the first-level square palette and keep later bundled themes on `palette.orchard.default`.
- [x] 2.3 Register both production palettes through deterministic project setup and the serialized Battle scene without release dependencies on trial content.

## 3. Validation and evidence

- [x] 3.1 Add focused editor validation for asset ownership, palette registry, per-level theme isolation, exact first-level visual-cell counts, and preserved gameplay topology.
- [x] 3.2 Run the focused first-level validation and aggregate `FruitDefense.Editor.ProjectSetup.SmokeValidate` suite.
- [x] 3.3 Build ordinary WebGL and capture the real `orchard-01` Battle canvas at the portrait acceptance viewport.
- [x] 3.4 Validate the OpenSpec change strictly and record final evidence paths and any platform limitations.
