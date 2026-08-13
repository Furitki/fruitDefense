## 1. Release Terrain Binding

- [x] 1.1 Make editor project setup bind the third registered grass-on-soil Runtime64 dirt and composite grass outputs into the release palette while preserving other registered bindings.
- [x] 1.2 Serialize the same exact third-choice brush bindings into the release orchard palette used by the Battle scene.
- [x] 1.3 Extend terrain smoke validation to assert the exact release assets and their runtime import/render contracts.

## 2. Battlefield Presentation

- [x] 2.1 Remove idle planting-cell outlines, fills, and markers while preserving active pot placement and drag feedback on the existing hit rectangles.
- [x] 2.2 Remove enemy square backplates, enlarge projection-scaled enemy sprites, and retain centered health and combat-status feedback.
- [x] 2.3 Change bundled and recommended monster-route visual cells to base-only dirt without changing route gameplay.

## 3. Unity Verification

- [x] 3.1 Validate the OpenSpec change and run `FruitDefense.Editor.ProjectSetup.SmokeValidate` successfully.
- [x] 3.2 Confirm the scoped changes do not alter bundled map semantics, combat simulation, persistence, or release scene flow.

## 4. WebGL Package and Visual Acceptance

- [x] 4.1 Build a fresh normal WebGL package through `FruitDefense.Editor.WebBuild.Build`.
- [x] 4.2 Run live 402 by 874 WebGL acceptance and inspect an active wave for cartoon terrain, no idle grid, frameless enlarged enemies, readable statuses, and intact controls.
- [x] 4.3 Deliver the validated package and evidence paths without publishing or changing mini-game platform readiness claims.

## 5. Clarification Rebuild

- [x] 5.1 Re-run strict OpenSpec validation and aggregate Unity P0 validation against the clarified third-brush and dirt-route result.
- [x] 5.2 Rebuild WebGL and inspect live portrait evidence for the exact Runtime64 grass/soil appearance, dirt monster path, no idle grid, and enlarged frameless enemies.
- [x] 5.3 Replace the prior package with the clarified accepted build and retain the final evidence.
