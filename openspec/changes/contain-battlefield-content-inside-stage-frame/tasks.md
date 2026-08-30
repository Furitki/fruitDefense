## 1. Runtime Containment

- [x] 1.1 Wrap all battlefield-owned drawing and board hit targets in one absolute-coordinate `BattleStage` IMGUI clip with guaranteed cleanup.
- [x] 1.2 Move the gameplay-stage frame to the final stage-occlusion pass and clip board-target frame/cue/ghost pixels without masking the cross-region connector.

## 2. Automated Validation

- [x] 2.1 Extend focused Battle editor smoke to enforce the shared clip, final frame ordering, board-target containment, connector exception, and full/inset/1280×720 projection contract.
- [x] 2.2 Add final-pixel WebGL analysis for protected-rail and outside-stage contamination in an edge board-target drag state.

## 3. Acceptance

- [x] 3.1 Run strict OpenSpec validation, focused Unity checks, and `FruitDefense.Editor.ProjectSetup.SmokeValidate`, fixing containment regressions. The containment checks pass; aggregate smoke reaches an unrelated pre-existing `CombatFeedbackSdfRenderSmoke` role-route failure after the UI/layout checks.
- [x] 3.2 Build ordinary WebGL and capture full/inset/1280×720 edge-drag evidence proving the stage rail remains intact.

## 4. Frame-opening Fit Correction

- [x] 4.1 Replace the over-conservative ArtSet safe-inset clip with one gameplay-stage opening mask at the component's explicit 8pt visible boundary, preserving the outer stage clip and unchanged hit geometry.
- [x] 4.2 Update structural and WebGL pixel gates to protect the 8pt visible rail and require edge feedback to reach the opening band without crossing it.
- [x] 4.3 Rebuild and recapture 402×874 full/inset and 1280×720 evidence proving both containment and frame-opening fit.

## 5. Vertical Gutter Fit Correction

- [x] 5.1 Paint the palette base terrain through the full stage mask before the unchanged square grid so the top and bottom aspect-ratio gutters meet the frame opening.
- [x] 5.2 Add structural and final-pixel gates for top/bottom terrain coverage without changing tile size, projection, or hit geometry.
- [x] 5.3 Rebuild and recapture all three viewport profiles, then deliver the updated same-state comparison.
