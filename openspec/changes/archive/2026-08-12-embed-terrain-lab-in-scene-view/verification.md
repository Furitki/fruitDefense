# Verification

Verified on 2026-07-24 with Unity `6000.3.19f1`.

## Automated acceptance

- Final editor script compilation completed with `Tundra build success`; no new compile errors were introduced. The log still contains the two pre-existing obsolete-API warnings in `CanonicalBattlefieldMapEditorWindow.cs` and `ProjectSetup.cs`.
- Focused layered-terrain painter smoke passed with `FRUIT_DEFENSE_LAYERED_TERRAIN_PAINTER_OK` and `CODEX_TERRAIN_LAB_EMBEDDED_FOCUSED_OK`.
- Aggregate editor smoke passed with `FRUIT_DEFENSE_SMOKE_OK` and `CODEX_TERRAIN_LAB_EMBEDDED_AGGREGATE_OK`.
- `openspec validate embed-terrain-lab-in-scene-view --strict` passed.
- `openspec validate --all --strict` passed all 38 items.

## Visual acceptance

- Evidence: `Builds/Evidence/terrain-material-laboratory/unity-terrain-material-laboratory.png`
- Capture: 1024 x 688 PNG, SHA-256 `F728DDF87633ECEC382C499E7D5873A81041F0E7694AE96EFA526BC0E1A608B4`.
- The inspected image shows one maximized Scene workspace with the terrain target, contour, four semantic brush presets, contextual edge mode, active brush state, paint controls, and the visible Scene canvas together. No standalone terrain-painter window is present, and the panel clears the Scene tool strip.
- Evidence setup opens an additive copy of the diagnostic scene and normalizes legacy edge data only in that copy; it does not modify the scene asset.

## Scope review

- Removed the temporary one-shot validation runner and its Unity metadata after acceptance.
- No runtime script or product-direction document was changed by this editor-workflow change.
