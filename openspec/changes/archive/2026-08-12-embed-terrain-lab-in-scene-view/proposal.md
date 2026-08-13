## Why

The current terrain laboratory separates brush selection into its own editor window while painting still happens in the Scene view, so a simple material experiment requires managing two Unity windows. The laboratory should return to a single-workspace flow without losing the semantic presets, validation, Undo, or exact directed-edge behavior added by the current painter.

## What Changes

- Replace the ordinary standalone terrain-painter window with a terrain-laboratory panel embedded inside the active Scene view.
- Make the existing `Fruit Defense/地图工具/地貌素材实验室` entry focus the Scene view and activate the embedded laboratory against the selected or sole valid target.
- Keep target selection, contour selection, four semantic brush presets, contextual edge refinement, explicit erasure, active-brush feedback, and start/stop controls together in the embedded panel.
- Allow the panel to collapse without ending the paint session and provide an explicit close action that releases Scene input.
- Keep the former window API as a compatibility redirect so existing Inspector and acceptance entry points activate the embedded workflow instead of opening another window.
- Preserve the authoring component, runtime terrain data, gameplay, persistence, release scenes, and ordinary WebGL behavior.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `layered-terrain-painter-workflow`: Change the required ordinary authoring surface from a dedicated painter window plus Scene view to a single Scene-view workspace with an embedded laboratory panel and bounded activation lifecycle.

## Impact

- Affects the editor-only terrain painter UI, Scene input controller integration, custom Inspector launch behavior, and focused editor acceptance under `Assets/Editor/Tools` and `Assets/Editor/Tests`.
- Does not change `LayeredTerrainTilemap` serialization, generated terrain outputs, runtime rendering, player-visible flow, gameplay rules, saves, builds, or platform support.
- Validation is editor compilation plus focused and aggregate editor smoke; no new runtime/WebGL surface is introduced by this editor-only workflow change.
