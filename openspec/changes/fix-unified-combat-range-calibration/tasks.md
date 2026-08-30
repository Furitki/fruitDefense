## 1. Unified calibration implementation

- [x] 1.1 Replace route-length-derived legacy distance scaling with the canonical per-cell calibration in `BattlefieldMapDefinition`.
- [x] 1.2 Remove the GM-only combat-distance override so GM uses the same map-construction path as normal battle maps.

## 2. Regression coverage

- [x] 2.1 Extend level-map validation to assert identical legacy-distance cell coverage on orchard-01, orchard-02, and the short-route orchard-03 map.
- [x] 2.2 Extend GM validation to assert shared construction-path parity, including effective-range overlay and inside/outside combat-boundary behavior on a short-route normal map.

## 3. Validation and runtime evidence

- [x] 3.1 Run the focused Editor smoke tests and the aggregate `FruitDefense.Editor.ProjectSetup.SmokeValidate` suite.
- [x] 3.2 Build the normal WebGL player and capture runtime evidence that an inspected plant's short-route range overlay matches its combat targeting boundary.
