# Verification

## Unity editor validation

- Unity version: `6000.3.19f1`
- Final script compilation completed successfully with no new compiler errors.
- `FruitDefense.Editor.ProjectSetup.SmokeValidate` completed successfully.
- Focused layered-terrain painter validation reported `FRUIT_DEFENSE_LAYERED_TERRAIN_PAINTER_OK`.
- Aggregate validation reported `FRUIT_DEFENSE_SMOKE_OK` together with the existing project smoke markers.
- The acceptance scene contains an author-ready grass/soil presentation profile.

## Editor visual acceptance

The actual Unity terrain painter and Scene view were captured and inspected:

- `Builds/Evidence/layered-terrain-painter/unity-terrain-painter.png`
- `Builds/Evidence/layered-terrain-painter/unity-terrain-painter-scene.png`

The captures show:

- four semantic presets: pure grass, pure soil, grass on soil, and soil on grass;
- contextual base-edge and AI-refined-edge selection for a composed brush;
- the active-brush summary and explicit start/stop state;
- collapsed-by-default advanced tools expanded for acceptance;
- Scene-view painting guidance, persistent active-brush feedback, and the accepted layered sample;
- no raw Tile or TileSet asset selection in the ordinary authoring workflow.

## WebGL release-parity check

- Ordinary WebGL was rebuilt through `FruitDefense.Editor.WebBuild.Build`.
- Build marker: `FRUIT_DEFENSE_WEB_BUILD_OK`
- Build version: `2b6d1498eed6`
- Total output size: `17,458,374` bytes
- Main payloads: data `13,412,369`, framework `68,982`, loader `117,893`, wasm `3,839,327` bytes
- Browser acceptance covered the Lobby-to-Battle transition and a portrait Battle run with the terrain and controls visible.
- Release scene ordering and complete `Bootstrap → Lobby → Battle → Settlement` parity remain covered by aggregate editor validation.
- Browser evidence: `Builds/Evidence/layered-terrain-painter/webgl-release-battle.png`
- Browser console contained only the existing desktop-browser persistent-data sync warning and unavailable orientation-lock message; no new application, asset, or render errors were observed.

Ordinary WebGL is used only as the shared release baseline. This verification does not claim Douyin or WeChat adapter readiness.

## Documentation state

The proposed product workflow remains recorded in `docs/design/pending-design-review.md`. The stable game-design overview was not synchronized and remains pending explicit user approval.
