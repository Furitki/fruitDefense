# Line-Free Nursery Carrier Correction Evidence

> Superseded on 2026-08-28 after PC rejection. This revision removed the visible rail but combined an RGB checkerboard plate with legacy slot alpha geometry. The resulting neutral/dark and orange semi-transparent edge fringe could still reconstruct as four perimeter lines at selected PC scales. See `pc-alpha-fringe-correction.md` for the replacement.

Date: 2026-08-28

## Result

The nursery section keeps its authored outer `NurseryTray` frame and each slot keeps a warm cream rounded-paper carrier with shallow depth. The `slot.nursery` master no longer contains an orange solid rim or inner dashed rail. Empty-slot interaction remains marker-free and uses the existing short motion response.

The original resolution-dependent symptom was caused by those one-pixel rails being baked into the slot raster and then sampled at fractional player scale. At some resolutions the rail pixels landed strongly on output pixels; at others filtering blended them into the cream face. Removing the authored rails from the master makes the result independent of that sampling accident.

## ImageGen provenance

Mode: built-in ImageGen image edit.

Final edit prompt:

> Remove only the orange/yellow outer solid rim and inner dashed rail while preserving the warm cream rounded paper face, gentle painted texture, soft tonal edge, shallow depth, transparent exterior intent, and text/icon-free ownership.

Final background-extraction prompt:

> Preserve the line-free cream rounded paper tile and replace only the baked checkerboard exterior with genuine transparent alpha; add no outline, dash, dot, border, marker, text, icon, or black pixel.

The attempted transparent output still arrived as an RGB checkerboard plate, so it was not accepted as a standalone alpha asset. Production uses only its reviewed line-free material pixels plus the separately hash-locked, asset-specific geometry alpha mask. The exporter does not paint over, recolor, blur, or procedurally erase rails.

| Artifact | SHA-256 |
|---|---|
| `evidence/direct-replacement-v2/imagegen/slot-nursery.png` | `995C3C6188886D6D75801204569C62C3768C9DF031334695AB48A9B2C810038D` |
| `evidence/direct-replacement-v2/imagegen/slot-nursery-geometry-mask.png` | `9739CB264370D075E67AB9E924688E1E8B9EA455A4C9D4D2A3E5C067C2CC8B7E` |
| `evidence/rejected-slot-nursery-with-rails.png` | `130E845511C06FCDD9816AA113B6C12E2683F05E1C41AECD2A738E863ACDDEEF` |

## Deterministic export

The revision-7 exporter was run twice against the same direct asset and mask. All four checked artifacts retained identical hashes:

| Artifact | SHA-256 |
|---|---|
| source `surfaces/slot-nursery.png` | `51BA3647EBB3672E5EFB3B6CB2A4B6446586548F9076F3927F0B216530BE4E22` |
| runtime `surfaces/slot-nursery.png` | `C52ECD5541DEBB42928AAF76364B8AC68144E4866D1EAF3C208812BD348E7B53` |
| `art_manifest.json` | `0D974BD657D71755CB765234DA7FEA354983B40B20BC552A89F13584CF6A4D73` |
| `SunnyOrchardPaintedRuntimeUiArtSet.asset` | `7E3DE2AECE1AC6588639AEC1DDCB3FDDC512B8FA498EAF71F7F538DEFF9014D7` |

The significant-alpha pixel scan found zero rail-orange pixels and zero black pixels in both outputs:

- source 256x256: `visible=56000`, `railOrange=0`, `black=0`
- runtime 128x128: `visible=14020`, `railOrange=0`, `black=0`

## Validation and builds

- `openspec validate polish-sky-paper-ui-eight-point --strict`: passed.
- `CompactControlAcceptanceSmoke.Run`: passed with `COMPACT_CONTROL_ACCEPTANCE_SMOKE_OK` in `Logs/line-free-nursery-compact.log`.
- `RuntimeUiVisualSystemSmoke.Run`: its nursery assertions compile, but the aggregate is blocked by the pre-existing `scripts/webgl-acceptance/image-analysis.ps1 exceeds the 900-line module boundary` source-authority gate. This change does not add lines to that script.
- Windows x64 player: `Build Finished, Result: Success` in `Logs/line-free-nursery-windows-build.log`; current data assembly SHA-256 is `BFA48E40F0E6CDF480EFA953FB19C9B3750FBA66F969BC0CACCD04A6294A7D6E`.
- The rebuilt Windows player launched successfully as the `水果塔防` window, but two attempts to capture that Unity window through Windows Graphics Capture failed with the host response `SetIsBorderRequired failed: 不支持此接口 (0x80004002)`. The acceptance window was then closed. No WebGL screenshot is labeled as PC evidence.
- Ordinary WebGL release: `FRUIT_DEFENSE_WEB_BUILD_OK` in `Logs/line-free-nursery-webgl-build.log`; data SHA-256 is `E01AC4DB17F2A3D8B5F7C8FFFA268F86CC04A56D64190ACA9AC664BFB7916701` and wasm SHA-256 is `172D5782392C5B56B7849E23E030D03D2B87D0F48135DF52835F8C08ABE264D5`.
- Unity's post-build preview proxy reported port 35020 already in use because an earlier local preview server was still running. The build itself completed successfully with exit code 0 and was accepted through that existing server.

## Real WebGL canvas review

| State / viewport | Evidence | Review |
|---|---|---|
| normal, 1280x720 | `line-free-nursery-webgl-ready.png` | outer section frame retained; five rounded-paper carriers retained; no solid/dashed slot rails |
| clicked, 1280x720 | `line-free-nursery-webgl-click.png` | no selection marker or four-line cue; click response remains motion-only |
| normal, 1024x768 | `line-free-nursery-webgl-1024x768.png` | no rail at a different fractional canvas scale |
| normal, 1366x768 | `line-free-nursery-webgl-1366x768.png` | no rail at a second fractional canvas scale |

This WebGL capture is evidence for the shared visual result, not a claim that WebGL substitutes for PC package acceptance. The rebuilt Windows package carries the same revision-7 runtime PNG and is ready for explicit PC resolution review.
