# Acceptance Evidence

Date: 2026-07-22

## Automated validation

- Unity `6000.3.19f1` batch smoke completed successfully. The final log is `Logs/layered-smoke-final.log` and contains the layered-map, Dual-Grid terrain, catalog, multi-level simulation, snapshot V2, shell, and aggregate smoke success markers.
- The permanent pre-migration regression fixture covers all three bundled maps. Exact outcome checksums remain:
  - `orchard-01`: `4d94f64cad0a67b56090bb22fd6d922c0aab28e44668f5fc6e3abbcc442d26aa`
  - `orchard-02`: `a7f66482933378e1c9863bca20f1f591d28b7858d7690856d27d3aacf2853bb1`
  - `orchard-03`: `781ab86ead73e1a1e6e67b0cc4492c904b77be87249f5059ec0612cef41ed4f7`
- Presentation-only surface and palette changes preserve both the gameplay fingerprint and deterministic outcome. Capability, collision, route, and gameplay-marker changes alter the gameplay fingerprint and are rejected by snapshot restoration when incompatible.
- Snapshot V2 round-trip continuation passes with the gameplay fingerprint. Previously supported V2 payloads without that optional field still restore.

## Ordinary WebGL build

- `FruitDefense.Editor.WebBuild.Build` completed successfully. The final log is `Logs/layered-web-build.log`.
- Output: `Builds/WebGL`; compression: `BrotliFallback`; stripping: `High`; build version: `be2854e5c618`; total size: `18,942,416` bytes.
- The release scene order remains `Bootstrap -> Lobby -> Battle -> Settlement`.
- This result is the shared ordinary-WebGL baseline only. It does not authorize Douyin or WeChat support.

## Real-canvas visual matrix

- Root evidence directory: `Logs/layered-visual-acceptance/`.
- All three bundled maps were exercised at `360x800`, `375x812`, `402x874`, and `430x932`, both with the full viewport and with `top=24/bottom=34` safe-area insets. The `402x874` viewport also covered `top=47/bottom=0` and `top=0/bottom=34` extremes.
- Result: `30/30` expected acceptance manifests present. Every completed run emitted `FRUIT_DEFENSE_VISUAL_ACCEPTANCE_OK` and captured 13 states, including ready and dense-battle scenes.
- Manual inspection confirmed terrain parity, route/core/pot/entity readability, layer clipping, marker alignment, hit targets, and safe-area controls across representative narrow, wide, standard-inset, and extreme-inset captures.

## Migration cleanup

- Duplicate live exclusive-role authoring and validation were removed; layered source data is canonical for all bundled maps.
- The old point/route constructor remains only for supported legacy custom-map and snapshot compatibility, not as live catalog authoring.
- The pre-migration JSON is retained deliberately as a permanent regression fixture and the capture utility has been converted into `BattlefieldLayeredMapSmoke`.
