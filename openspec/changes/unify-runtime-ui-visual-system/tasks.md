## 1. 优先级 A — 视觉盘点与方向冻结

- [x] 1.1 Capture the current Bootstrap, Lobby, Battle HUD/detail/modal, and Settlement release states at the 402-by-874 reference viewport and record a component-by-component inconsistency audit without changing runtime code.
- [x] 1.2 Produce one orchard-cartoon UI style board covering the semantic palette, Chinese typography hierarchy, spacing rhythm, panel/card/button families, resource metrics, selection/disabled/error states, modal/result treatment, icon canvas, outlines, shallow-depth rules, and at least two interchangeable art-set treatments using the same semantic slots.
- [x] 1.3 Review the style board against existing battlefield content and obtain explicit visual approval before creating production UI assets or converting a release surface.
- [x] 1.4 Create `docs/ui/ui-visual-system.md` with the approved component anatomy, state matrix, art source/export/slicing/naming/import rules, and visual review checklist, keeping exact runtime values owned by the release theme asset.

## 2. 优先级 A — 单一主题与美术资产底座

- [x] 2.1 Add the dedicated editable-source and `Assets/UI/Art/Runtime` production hierarchies, stable semantic asset naming, and an approved reference-board location without placing raw or review assets in release `Resources`.
- [x] 2.2 Implement the single `RuntimeUiTheme` asset contract for semantic colors, typography, metrics, opacity/motion, packaged Chinese font, and exactly one active `RuntimeUiArtSet` reference.
- [x] 2.3 Implement `RuntimeUiArtSet` with stable set ID/revision, complete finite semantic slots, texture/sprite references, and slicing metadata, rejecting inheritance, duplicate slots, and missing-slot fallback.
- [x] 2.4 Implement the minimal shared immediate-mode style/component layer so all artwork resolves only through semantic slots from the active art set.
- [x] 2.5 Create the approved production nine-slice surfaces, buttons, state indicators, and common icons with owned lossless masters and optimized runtime exports assembled into at least one complete release art set.
- [x] 2.6 Add the stable `Fruit Defense/UI/Visual System` editor workflow under `Assets/Editor/Tools` to discover sets, validate and preview a component/state gallery plus representative route chrome, atomically activate one valid set, and support Undo without editing scenes or code.
- [x] 2.7 Add editor test fixtures and smoke coverage for complete/incomplete sets, preview isolation, atomic activation, Undo, in-place reimport behavior, unchanged scene references, and exclusion of fixtures from the release theme.
- [x] 2.8 Bind the one release theme through the persistent Bootstrap composition root, pass it explicitly to route presenters and Battle initialization, and make project configuration reproduce the binding.
- [x] 2.9 Add editor validation for theme/art-set completeness, stable identity/revision, semantic token constraints, essential text contrast/size, component-state coverage, production asset ownership, alpha/color-space/import settings, padding, slicing, filter/wrap/mipmap/compression, and release references.

## 3. 优先级 A — 首个端到端样板：Bootstrap 与 Lobby

- [x] 3.1 Convert Bootstrap initializing, blocking error, recoverable error, and retry presentation to the shared theme and component hierarchy, preserving initialization and recovery behavior.
- [x] 3.2 Convert Lobby title, three level cards, selected state, Start action, transition-disabled state, and status feedback to the shared visual system while retaining current layout and hit rectangles.
- [x] 3.3 Delete the coordinator default-skin presentation, migrated Shell style code, reserved-card compatibility helpers, and legacy Lobby compatibility validation made obsolete by the converted slice.
- [x] 3.4 Preview both approved interchangeable art treatments through the editor workflow, confirm that switching requires no code/scene/layout changes, then activate the selected release set.
- [x] 3.5 Validate Bootstrap and Lobby at every supported full/inset portrait case and capture real WebGL default, alternate-selection, transition, and error evidence for style-board comparison.

## 4. 优先级 B — Battle 高频界面统一

- [x] 4.1 Extract Battle UI-chrome rectangles and state presentation into dedicated layout/style collaborators without moving simulation rules or creating alternate draw/hit geometry.
- [x] 4.2 Convert the Battle header, resource metrics, pause/speed controls, battlefield status strip, and wave action to the shared surface, metric, and action components.
- [x] 4.3 Convert weapon/pot tools, nursery slots, refresh action, selected/disabled states, legal/illegal drag feedback, and transient status treatment while preserving all click and drag behavior.
- [x] 4.4 Convert plant detail, pause modal, victory/defeat overlay, and terminal actions to the shared detail/modal/result families with consistent non-color state cues.
- [x] 4.5 Delete Battle-local `BuildStyles`/`Style`/duplicated panel-button helpers and the standalone compatibility UI path after all Battle chrome uses the shared theme.
- [x] 4.6 Run portrait geometry, interaction, restart, deterministic simulation, and presentation-boundary smoke checks, then capture real WebGL ready, active-wave, selected-tool, plant-detail, legal/illegal interaction, paused, and terminal evidence.

## 5. 优先级 B — Settlement 与完整路由闭环

- [x] 5.1 Convert Settlement title, outcome, completed-level and result metrics, Retry/Return hierarchy, transition-disabled state, and recoverable status to the shared visual system.
- [x] 5.2 Remove the remaining obsolete `ShellStyleSet` and `ShellGui` implementation once Lobby and Settlement no longer reference them.
- [x] 5.3 Validate victory, defeat, retry, return, missing-result recovery, and selected-level preservation, then capture real WebGL Settlement and return/retry evidence.

## 6. 优先级 B — 汇总验收与稳定文档

- [x] 6.1 Add the visual-system, theme/art-set, semantic-slot, editor activation, art-import, scene-binding, shared-state, and supported-viewport checks to `FruitDefense.Editor.ProjectSetup.SmokeValidate` and the aggregate release gate without adding scattered daily smoke menus.
- [x] 6.2 Run strict OpenSpec validation, Unity compilation, the aggregate editor smoke, deterministic/shell/session validations, and `FruitDefense.Editor.WebBuild.Build`.
- [x] 6.3 Execute the cross-route real WebGL visual matrix at the 402-by-874 full safe area and a representative top/bottom inset, recording the active theme/art-set identity and failing any default-skin, legacy-style, mixed-set, clipping, stretch, overlap, contrast, or input-drift defect.
- [x] 6.4 Finalize only accepted stable rules in `docs/ui/ui-visual-system.md`, link it once from `README.md`, and keep build/evidence status in its existing baseline or acceptance owner rather than duplicating it in the guide or game-design overview.

## 7. 优先级 C — 统一性精修与交付收口

- [x] 7.1 Tune restrained shared pressed, selection, warning, and transition feedback durations using unscaled time, honoring reduced visual noise and preserving command timing.
- [x] 7.2 Inspect texture memory, GUI allocations, draw-call impact, Chinese glyph coverage, and WebGL clarity; optimize production assets or style caching without adding a second visual path.
- [x] 7.3 Remove rejected UI candidates, obsolete assets, source experiments, temporary review helpers, one-shot capture code, and unused compatibility references while preserving the active production set, intentional iteration candidates, required masters, validators, and final evidence ownership.
- [x] 7.4 Re-run the complete editor and WebGL acceptance sequence after cleanup and record the final handoff with no gameplay, persistence, platform-support, or design-overview claim.
