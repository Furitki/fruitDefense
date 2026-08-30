# Validation Summary

## Passed

- `openspec validate add-plant-drag-connector-and-snap-feedback --type change --strict --no-interactive`
- `FruitDefense.Editor.BattleUiLayoutSmoke.Run` with `FRUIT_DEFENSE_BATTLE_UI_LAYOUT_OK` in `Logs/drag-feedback-focused.log`
- Existing runtime UI quality, feedback timing, interaction polish, compact-control, and visual-system smokes completed with their success markers after the drag-feedback runtime code compiled.
- Ordinary release WebGL completed with `Build Finished, Result: Success.` and `FRUIT_DEFENSE_WEB_BUILD_OK` in `Logs/marker-free-slot-webgl-build.log`.
- Acceptance WebGL completed with `Build Finished, Result: Success.` and `FRUIT_DEFENSE_ACCEPTANCE_WEB_BUILD_OK` in `Logs/drag-feedback-acceptance-build.log`.
- Focused live-canvas drag evidence passed at 402×874 full screen and at safe-area insets top 44 / bottom 34. Each manifest records a loaded acceptance build, correct screenshot dimensions, and captured free, legal, and illegal drag states.

## Remaining aggregate blocker

Task 3.2 remains open. `FruitDefense.Editor.ProjectSetup.SmokeValidate` reached the unrelated active floating-text suite and failed at `CombatFeedbackSdfRenderSmoke` with `role-route prepares every admitted label`. The drag-feedback-focused and runtime UI visual-system smokes complete before that point. This change does not alter the floating-text workstream.

## Resource-frame and PC-matrix follow-up

- The primitive four-edge target frame was removed. `DrawDragTargetFrame` now draws the approved transparent-center `surface.illustration-frame` production nine-slice binding with the resolved semantic drag state.
- The connector is projected through the current GUI matrix into device space, then rotated under `Matrix4x4.identity`; this prevents a PC letterbox offset from participating in the dash rotation.
- `FruitDefense.Editor.BattleUiLayoutSmoke.Run` passed with deterministic 1280×720 projection coverage in `Logs/drag-feedback-pc-matrix-focused.log`.
- `FruitDefense.Editor.RuntimeUiVisualSystemSmoke.Run` passed with `RUNTIME_UI_VISUAL_SYSTEM_SMOKE_OK` in `Logs/drag-feedback-resource-frame-visual-system.log`.
- A Windows x64 verification player built successfully at `Builds/Evidence/drag-feedback-pc/FruitDefense.exe`; the desktop capture service could not capture this Unity window, so no unverified coordinate automation was performed.
- The rebuilt acceptance WebGL passed at 1280×720 with design scale `0.823798627` and horizontal offset `474.416476`. Its manifest and free/legal/illegal frames are stored under `evidence/webgl-1280x720-wide-pc-matrix`.
