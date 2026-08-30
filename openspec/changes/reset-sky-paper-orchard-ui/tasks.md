## 1. Baseline and Reference Contract

- [x] 1.1 Record the current 402×874 Battle ready/active/paused/selected-detail screenshots, layout measurements, release theme/ArtSet/font identities, and open blocking visual defects as the before baseline.
- [x] 1.2 Store the supplied full-page image and concise interpretation notes only under this change's evidence area, mark generated copy/gameplay imagery/coordinates as non-authoritative, and verify no production asset or scene depends on it.
- [x] 1.3 Inventory every current Battle Wave draw/hit/acceptance reference and every legacy single-font access so removal can be verified without adding compatibility or fallback paths.
- [x] 1.4 Add or update the focused Editor inspection catalog for the six-track Battle composition and finite cross-route copy/state matrix using fixtures outside production `Resources`.

## 2. Role-Level Packaged Typography

- [x] 2.1 Select the rounded high-weight Chinese display/action face and reading face, add the static font assets plus complete license/source records, and verify required Chinese, numeric, punctuation, and boundary-copy glyph coverage.
- [x] 2.2 Move the packaged `Font` reference into every `RuntimeUiTypographyStyle`, serialize explicit release-theme bindings for all seven roles, and remove the legacy `packagedChineseFont` field and any single-font migration/fallback logic.
- [x] 2.3 Update shared `RuntimeUiGui` measurement, line-box, and `GUIStyle` caching so each role uses the same assigned font for bounds calculation and drawing without Presenter-local font or optical-offset choices.
- [x] 2.4 Extend theme/font validation and Editor tests to reject null, host, out-of-project, unlicensed, missing-glyph, legacy-field, synthesized-style, and measure/render font mismatches.
- [x] 2.5 Measure the finite copy catalog with the packaged role faces at 360×800, 375×812, 402×874, and 430×932 full and representative inset layouts and resolve every overflow without truncation, implicit shrinking, or hidden clipping.

## 3. Authoritative Battle Layout Reset

- [x] 3.1 Rebuild `BattleUiLayout` at 402×874 into Header, gameplay stage, `PhaseWaveRow`, ContextTray/detail, NurseryTray, and RefreshAction tracks with four-point gaps, 44-point minimum actions, 8–40-point lower closeout, and a stage height within the specified 38–43% safe-content band.
- [x] 3.2 Construct the one `BattlefieldProjection` from the new stage owner, preserve map/entity legibility and symmetric gutters, and keep renderer, hit tests, drag/drop, range overlays, transient feedback, and acceptance coordinates on that same projection.
- [x] 3.3 Add authoritative phase-status and Wave-action child rectangles to `BattleUiLayout`, then remove the in-stage Wave/control-strip rectangle and every duplicate draw, hit, compatibility, and fallback path from `BattlefieldProjection` and presentation code.
- [x] 3.4 Render the persistent phase/Wave row so ready shows `开始波次`, countdown shows `立即开始下一波`, active shows non-action wave/enemy progress, and terminal exposes no Wave command while keeping lower track positions stable.
- [x] 3.5 Preserve pause, continue, speed, restart, refresh, tool, nursery, detail, click-versus-drag, merge, range, and session command semantics and update acceptance hit-target metadata to the new authoritative rectangles.
- [x] 3.6 Extend `BattleUiLayoutSmoke` and focused tests for track order, non-overlap, stage-height band, full/inset safe containment, target size, projection round trips, draw/hit identity, phase lifecycle, and absence of the removed in-stage Wave target across the supported portrait matrix.

## 4. Sky-Paper Theme and Deterministic Art

- [x] 4.1 Reset release theme tokens to the approved clear-sky edge, warm-paper surfaces, soil-brown stage/text, leaf-green primary, sunlight-yellow phase/selection, restrained fruit-red danger, shallow shadow, outline, spacing, and motion hierarchy.
- [x] 4.2 Produce reviewed text-free masters for the existing semantic surfaces, actions, slots, resource/control icons, state cues, route illustrations, and restrained corner ornament without redrawing gameplay content art or adding new semantic slots.
- [x] 4.3 Update the owned deterministic exporter and manifest records as needed, export into the existing runtime destinations, preserve all destination `.meta` GUIDs, and record nine-slice borders, safe/optical insets, source/runtime hashes, licenses/prompts, and the incremented ArtSet/theme revisions.
- [x] 4.4 Delete superseded masters, exports, generators, alternate theme/ArtSet paths, placeholders, and one-shot review helpers after their replacements are validated; keep exactly one complete active 56-slot release treatment.
- [x] 4.5 Extend art/theme validators and tests to reject text-bearing rasters, reference-image dependencies, missing/duplicate/inherited slots, unstable GUIDs, stale hashes/revisions, unsafe ornament bounds, mixed assets, importer drift, and runtime fallback drawing.
- [x] 4.6 Run the deterministic exporter twice from a clean unchanged source state and verify byte/hash stability, complete semantic binding, and unchanged GUID/import geometry before visual integration acceptance.

## 5. Battle Vertical-Slice Integration

- [x] 5.1 Update shared immediate-mode components for the compact Header metrics, paper section surfaces, soil gameplay-stage frame, sunlight phase block, leaf-green primary Wave action, ContextTray/detail, NurseryTray, RefreshAction, and quiet pause/speed controls using semantic theme and ArtSet roles only.
- [x] 5.2 Integrate the reset components into Battle ready, active, paused, and selected-detail states without Presenter-local fonts, colors, texture paths, private layout rectangles, baked copy, or changes to simulation/content data.
- [x] 5.3 Run focused typography, art-system, layout, session-control, plant-inspection, drag/drop, and Battle presentation Editor tests and fix all blocking containment, contrast, alignment, state, projection, or input defects.
- [x] 5.4 Run `FruitDefense.Editor.ProjectSetup.SmokeValidate`, build ordinary WebGL through `FruitDefense.Editor.WebBuild.Build`, and capture the same-payload 402×874 full and representative inset ready/active/paused/selected-detail Gate A evidence with theme, ArtSet, font, build, geometry, and pointer identities.
- [x] 5.5 Complete a side-by-side manual Gate A review against the hierarchy notes, close every blocking/high Battle defect, and authorize route-wide rollout only after the shorter stage, independent phase row, packaged typography, lower closeout, and removed in-stage target are proven live.

## 6. Bootstrap, Lobby, and Settlement Rollout

- [x] 6.1 Recompose Bootstrap loading, blocking error, and retry surfaces with the accepted sky-paper theme, packaged role fonts, shared status/modal/action anatomy, and existing initialization/recovery behavior.
- [x] 6.2 Recompose Lobby title, selectable level cards, unavailable/reserved states, primary Start action, status feedback, illustrations, and occupied-content balance through `PortraitShellLayout` without changing launch requests or selection semantics.
- [x] 6.3 Recompose Settlement victory/defeat result hierarchy, metrics, Retry, Return, status/error states, illustrations, and occupied-content balance through `PortraitShellLayout` without changing result values, retry identity, or navigation behavior.
- [x] 6.4 Add or update shell layout, finite-copy, state, action-role, draw/hit, and safe-area tests for Bootstrap, Lobby, and Settlement at the supported full/inset portrait matrix and real wide-host fit geometry.
- [x] 6.5 Audit all four route presenters and release scenes to prove they consume the same release theme, role-font bindings, complete ArtSet, and shared component layer with no local visual theme, system font, direct texture path, or compatibility branch.

## 7. Full Ordinary-WebGL Acceptance

- [x] 7.1 Run the UI-focused Editor suites and aggregate `FruitDefense.Editor.ProjectSetup.SmokeValidate`, then produce one release payload through `FruitDefense.Editor.WebBuild.Build` for all Gate B evidence.
- [x] 7.2 Capture Bootstrap, Lobby, Battle, Settlement, return, retry, and recoverable failure states at 360×800, 375×812, 402×874, and 430×932 full plus representative inset safe areas, recording build/theme/ArtSet/font identity and text/structural bounds.
- [x] 7.3 Exercise live Start, Wave, pause/continue, speed, tool, nursery, refresh, inspection close, drag/drop, Settlement Retry/Return, and recovery targets and verify each recorded canvas-relative point resolves to the authoritative visible control without pointer drift or duplicate command.
- [x] 7.4 Capture the same payload in a representative wide desktop host including 1280×720 and verify complete centered portrait fit, uniform scaling, letterboxing, no scrolling/clipping/black frame, current payload delivery, and correct pointer mapping.
- [x] 7.5 Audit the built payload and asset manifests for one theme, one complete ArtSet revision, packaged role fonts, deterministic text-free assets, stable GUID/hash records, and no concept image, stale source/runtime pair, legacy treatment, fallback, or platform-readiness overclaim.
- [x] 7.6 Close all blocking/high issues in the severity-ranked visual defect inventory and produce a final evidence index distinguishing ordinary-WebGL acceptance from unproven Douyin/WeChat conversion.

## 8. Documentation and Cleanup

- [x] 8.1 Synchronize the accepted sky-paper hierarchy, role-level packaged typography, independent Battle phase/Wave row, finite 56-slot production rules, and two-gate review standard into `docs/ui/ui-visual-system.md` without copying transient build evidence.
- [x] 8.2 Update the appropriate release/gate evidence owner only for verified current-state claims and leave the stable game-design overview unchanged because gameplay direction and core loops did not change.
- [x] 8.3 Remove temporary importers, debug commands, marker files, disposable acceptance helpers, and production references to evidence fixtures; preserve stable authoring tools under `Assets/Editor/Tools`, validation under `Assets/Editor/Tests`, fixture isolation, and all required `.meta` GUIDs.
- [x] 8.4 Run final OpenSpec strict validation, inspect the scoped working-tree diff for unrelated changes or obsolete implementation paths, and hand off the complete task/evidence/known-observation status without claiming unsupported mini-game platforms.

## 9. User-Rejected Gate A Rework

- [x] 9.1 Record that the first implementation was rejected at 3/10 reference similarity because only the palette related to the reference while button anatomy and page composition did not, and downgrade its manifests to engineering evidence rather than visual acceptance.
- [x] 9.2 Produce a component-by-component reference decomposition with 402×874 relative geometry, semantic-slot ownership, material anatomy, and a delete list for the generic v1 drawing paths.
- [x] 9.3 Produce and review one text-free reference-faithful Battle chrome/style board as non-production evidence, using the supplied image only as a style/composition reference and not as a cropped runtime asset.
- [x] 9.4 Replace the active treatment's paper page, metric capsule, compact yellow control, phase block, primary green action, recipe card, dashed nursery slot, refresh action, and stage-frame masters/exports with independent reference-faithful nine-slice assets; preserve stable semantic ownership while removing superseded v1 masters and paths.
- [x] 9.5 Recompose `BattleUiLayout` and shared drawing into the floating two-row Header plus one rounded page shell containing the inset stage and lower control tracks, with authoritative draw/hit/projection geometry and no compatibility branch.
- [x] 9.6 Rebuild the Header metric groups, recipe/tool cards, nursery slots, bottom refresh anatomy, icon scale, and rounded display typography so the final canvas matches the reference's hierarchy rather than generic flat panels.
- [x] 9.7 Run focused editor tests, aggregate smoke, build one new acceptance WebGL payload, and capture 402×874 full ready/active/paused/selected-detail evidence plus a side-by-side reference comparison.
- [x] 9.8 Stop at the new Battle Gate A and obtain explicit user visual approval before route-wide rollout or final-completion claims.

## 10. ImageGen Action-Material Rework

- [x] 10.1 Record the user-approved production rule that all six action surfaces must originate from ImageGen output (or a crop of project-owned generated preview output), while the supplied reference remains evidence-only; update the proposal, design, and quality contract before changing assets.
- [x] 10.2 Generate and review one text-free, icon-free ImageGen component sheet containing isolated primary, secondary, quiet, danger, compact-normal, and compact-active button materials with reference-faithful rim, outline, highlight, and short shadow anatomy.
- [x] 10.3 Retain the selected generated output and prompt/hash provenance, then deterministically crop, exterior-alpha-clean, pad, and normalize its six components into the existing owned source paths without drawing new material pixels.
- [x] 10.4 Remove the procedural action-surface recipes and fallback path, preserve source/runtime `.meta` GUIDs, bump ArtSet/theme revisions, and update manifest/import metadata for the new fixed-source extraction boundary.
- [x] 10.5 Run the fixed-source extractor/exporter twice and prove byte/hash stability, text-free rasters, valid nine-slice partitions, stable GUIDs, complete 56-slot binding, and absence of legacy action-material code.
- [x] 10.6 Run focused UI tests, aggregate smoke, ordinary WebGL build, and same-payload 402×874 ready/active/paused/selected-detail capture with a side-by-side reference comparison.
- [x] 10.7 Stop at Battle Gate A and obtain explicit user visual approval before route-wide rollout, stable-document synchronization, or final-completion claims.
