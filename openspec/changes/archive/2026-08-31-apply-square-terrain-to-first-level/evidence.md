# Acceptance Evidence

Validated on 2026-08-31 with Unity `6000.3.19f1`.

## Runtime assets

- Production grass: `Assets/Battlefield/Terrain/Orchard01SquareGrid/GrassSquareBase.png`
  - SHA-256: `CF2EAC649F4F92F86B4999CF9D7447272A1233220D2ABC2395E3458BDBE4F321`
- Production soil: `Assets/Battlefield/Terrain/Orchard01SquareGrid/SoilSquareBase.png`
  - SHA-256: `2B3794E23E55C5BFE3643C5AF833A3E473BAB19D08DA09AC10741D7FA54554F4`
- Both hashes match their approved normalized v2 trial exports byte-for-byte. The release palette and Battle scene have no dependency under `Assets/LayeredTerrain/Trials/`.

## Editor validation

- Focused first-level smoke: `Builds/Logs/first-level-square-smoke.log`
  - Marker: `FRUIT_DEFENSE_FIRST_LEVEL_SQUARE_TERRAIN_OK`
- Aggregate project smoke: `Builds/Logs/apply-square-terrain-aggregate-smoke.log`
  - Marker: `FRUIT_DEFENSE_SMOKE_OK`
- OpenSpec strict validation: `Change 'apply-square-terrain-to-first-level' is valid`

## Ordinary WebGL

- Build output: `Builds/WebGL`
- Build log: `Builds/Logs/apply-square-terrain-webgl-build.log`
  - Marker: `FRUIT_DEFENSE_WEB_BUILD_OK`
  - Output: 7 files, 12,557,542 bytes
- Real `Bootstrap → Lobby → Battle` first-level portrait capture at 402×874:
  - `Builds/Evidence/apply-square-terrain-to-first-level/orchard-01-square-terrain-webgl.png`
  - SHA-256: `53EF3EAEF0229C7C4FB48B48F849AA55ABAB98D304F439C512F6940A6DE7DCF1`
  - Observed: 35 grass squares inside the 21-cell soil U-frame, eight initial pots, readable controls, and no missing-palette diagnostic.

## Platform boundary

The in-app desktop browser reports that `screen.orientation.lock()` is unavailable. This is a browser/device capability warning and did not affect the portrait canvas or terrain presentation. Ordinary WebGL acceptance does not authorize Douyin or WeChat support.
