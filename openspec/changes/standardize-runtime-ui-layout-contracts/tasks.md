## 1. Runtime layout and text anatomy

- [x] 1.1 Make shared single-line styles use semantic line-height and reject undersized owner rectangles instead of silently compressing them.
- [x] 1.2 Rebuild `BattleUiLayout` with one full-width owner track for Header and BattleStage, a two-row header, 4pt-derived section rhythm, and resized Board/Context/Nursery regions while preserving one draw/hit projection.
- [x] 1.3 Keep Header and persistent sections on the light 1–2px structural family, make BattleStage the sole normal-state 3–5px heavy frame, and remove the superseded raised-header path and second lower-page outer frame.
- [x] 1.4 Replace generic wrapped Battle chrome text for tool/pot counts, stars, details, and merge hints with explicit finite single/two-line anatomy.

## 2. Persistent validation

- [x] 2.1 Update Battle geometry smoke to assert shared owner-track alignment, the light-section/heavy-stage hierarchy, absence of a second enclosing frame, named tracks, complete line-height capacity, 4pt rhythm, containment, and draw/hit identity.
- [x] 2.2 Extend text inspection with real semantic action specs and boundary samples for dynamic metrics, costs, counts, stars, statuses, merge hints, and production content names.
- [x] 2.3 Add resolver fit/clamp assertions and projected 360/375/402/430 full+inset text/geometry checks instead of repeating the unprojected 402 layout.
- [x] 2.4 Add panel-family metadata/optical consistency checks and WebGL acceptance fields for live structural edges, 1–2px light-section outlines, the unique 3–5px BattleStage outline, absence of a second heavy outer frame, and text-ink containment.
- [x] 2.5 Synchronize the accepted long-term track, line-height, dynamic-copy, and review rules into `docs/ui/ui-visual-system.md` without copying transient evidence.

## 3. Verification and evidence

- [x] 3.1 Run focused Battle layout, runtime UI quality, visual-system, and copy/content validation suites to green.
- [x] 3.2 Run `FruitDefense.Editor.ProjectSetup.SmokeValidate` to green.
- [x] 3.3 Build ordinary WebGL with `FruitDefense.Editor.WebBuild.Build` and record the current payload/theme/ArtSet identity.
- [x] 3.4 Capture and review live 402×874 full+canonical-inset ready, active/between, detail, paused, and terminal evidence with the shared-owner-track, structural-weight, no-second-frame, and text checks.
