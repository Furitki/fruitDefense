## Context

`BattlefieldMapDefinition` currently derives `LegacyToMapScale` from the first route's total length. This incorrectly couples all legacy combat quantities to route topology. The GM factory overrides that default with the standard-map scale, which avoids the GM symptom but leaves normal short-route maps incorrect and creates two calibration paths.

## Goals / Non-Goals

**Goals:**

- Define one calibration rule for every `BattlefieldMapDefinition`: legacy distance is converted through the active map's cell pitch relative to the canonical pitch.
- Use that one rule for normal and GM maps without a GM-only override.
- Preserve the default map's current calibration and preserve the existing single value shared by targeting, projectile logic, overlay, and inspection text.
- Prove parity across normal long/short maps and GM, including range overlay projection.

**Non-Goals:**

- Changing authored plant, enemy, projectile, star-tier, equipment, or status values.
- Changing route geometry, route travel duration, range-overlay art, UI layout, or input geometry.
- Adding per-level range modifiers or a compatibility fallback.

## Decisions

### Calibrate from cell pitch, not route length

`LegacyToMapScale` SHALL be derived as `MapUnitsPerCell / LegacyReferenceMapUnitsPerCell * LegacyReferenceDistanceScale`. This preserves the canonical map's scale, keeps a legacy distance's covered-cell count invariant for all maps with the same cell pitch, and supports future maps with another valid pitch.

The rejected alternative is retaining the route-derived default and passing an override for every map factory. It is error-prone for authored/published maps and reintroduces divergent map construction paths.

### Remove the GM-only calibration argument

GM shall construct `BattlefieldMapDefinition` through the same ordinary constructor as normal maps. Its shared canonical pitch naturally resolves to the same calibration; there is no separate GM combat-distance implementation to synchronize.

### Regression at the calibration boundary and the combat/presentation boundary

Editor tests shall compare map-cell coverage for the same legacy distance on orchard-01, orchard-02, orchard-03, and GM. They shall also retain a deterministic inside/outside targeting assertion and verify the inspection overlay uses the same effective range, so map conversion cannot drift independently in the simulation and presentation.

## Risks / Trade-offs

- [Risk] Existing route-derived movement quantities have the same conversion helper and could be unintentionally changed. → Keep route progress/length logic untouched; assert only legacy-unit quantities change conversion source.
- [Risk] The default map's visual baseline could change due to float arithmetic. → Derive through the canonical constants and assert its existing scale within float tolerance.
- [Risk] GM could retain an obsolete special path. → Remove the override and make parity tests construct GM through the common map constructor.

## Migration Plan

1. Replace the route-derived scale with the shared per-cell calculation.
2. Remove the GM explicit scale argument.
3. Add focused parity and boundary regressions, then run the aggregate editor smoke.
4. No content, save-data, or released artifact migration is required.

## Open Questions

None.
