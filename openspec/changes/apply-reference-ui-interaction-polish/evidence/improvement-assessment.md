# UI interaction-polish improvement assessment

## Outcome

This iteration produces a measurable temporal improvement while deliberately preserving the approved resting layout and art. The Lobby selection response changes 4.120% of viewport pixels relative to the pre-change immediate frame, while both default and selected resting frames remain pixel-identical. Its new short inward impulse differs from rest across 15.808% of the viewport, and 658 of 1,020 sampled visible-outline pixels retreat inside the resting frame. The ordinary WebGL artifact is 11,216 bytes smaller than the isolated baseline, a 0.116% reduction.

No reference-derived raster entered production. The existing Sunny Orchard ArtSet remains authoritative because the protected APK payload has not passed complete decode and visual verification.

## Comparison method

- Before build: detached revision `1c6ad3e8a8f13f45018dd8ae7d719c785ddd8d34`, Unity `6000.3.19f1`.
- After build: working branch `codex/apply-apk-ui-learning`, Unity `6000.3.19f1`.
- Browser evidence: real ordinary WebGL at `402×874`; no Editor-only substitute and no mini-game support claim.
- Pixel metric: RGB pixels whose maximum channel delta is greater than 8, measured over the stated viewport or region.
- The isolated before build, hashes, and source revision are recorded in `webgl/before-402x874-interaction-probe/provenance.md`.

## Measured visual change

| Checkpoint | Measurement | Interpretation |
| --- | ---: | --- |
| Lobby default resting, before → after | 0 / 351,348 pixels; exact SHA-256 match | No resting layout or art regression. |
| Lobby alternate-selected resting, before → after | 0 / 351,348 pixels; exact SHA-256 match | The approved selected end state remains exact. |
| Lobby selection immediate frame, before → after | 14,477 / 351,348 pixels (4.120%) | The new short selected-card impulse is visibly distinct from the old immediate state. |
| Lobby selection impulse → new resting state | 55,540 / 351,348 pixels (15.808%); 658 / 1,020 visible-outline samples retreat inward | The card contracts inside its authoritative frame and returns to rest without outward overshoot. |
| Lobby Start pressed, before → after | 24,922 / 351,348 pixels (7.093%) | The shared press lifecycle produces a changed pressed checkpoint without moving the hit target. |
| Lobby Start pressed → new resting state | 24,575 / 351,348 pixels (6.994%); 960 changed edge-band pixels | The held scale is locally contained to the action region, then release navigation enters Battle. |
| Battle Wave hover → held press | 7,237 / 8,096 pixels (89.390%) in the exact `184×44` action rect; 478 changed edge-band pixels; 96 / 228 sampled edge pixels retreat inward | The comparison isolates the held press from hover color, proves geometric inset while the pointer remains down, and release then starts the wave. |
| Settlement reveal → resting Settlement | 23,172 / 351,348 pixels (6.595%) | Result surface, outcome/indicator, metrics, and actions reveal as one owned hierarchy instead of producing floating children. |

The Battle proof first captures the Wave action under the stationary pointer, then captures the same action while the pointer remains down. Besides color-region change, the verifier samples the original edge against its surrounding background and requires inward retreat; an active-wave screenshot cannot satisfy this check.

## Capability gain

- Added one pure value-only motion evaluator with five semantic patterns: press, pop, strong pop, fade-slide, and stagger.
- Pop lifetime is independent from long-lived status/reward visibility: the geometric impulse completes in `0.10s`, and motion samples reject scale above `1.0`.
- Added one Shell press owner with stable control IDs, valid-release activation, drag-distance suppression, cancellation, and disabled-target handling.
- Applied route-owned reveal and local feedback to Lobby, Battle, and Settlement without creating a second layout or input authority.
- Kept the authoritative hit rectangles fixed while transient scale, alpha, and offset affect only drawn geometry.
- Added a reduced-motion policy that resolves to the same semantic end state without travel, stagger, or overshoot.
- Added repeatable WebGL motion checkpoints to the existing portrait acceptance script; the release click still proves Lobby-to-Battle routing after the pressed frame.

## Build footprint

| Artifact | Before bytes | After bytes | Delta |
| --- | ---: | ---: | ---: |
| Complete WebGL output | 9,653,955 | 9,642,739 | -11,216 (-0.116%) |
| `WebGL.data.unityweb` | 5,575,469 | 5,573,023 | -2,446 |
| `WebGL.framework.js.unityweb` | 69,018 | 69,111 | +93 |
| `WebGL.loader.js` | 117,893 | 117,893 | 0 |
| `WebGL.wasm.unityweb` | 3,881,120 | 3,872,589 | -8,531 |

After-build identity:

```text
FRUIT_DEFENSE_WEB_BUILD_OK path=E:\project\unity\furitDefense\Builds\WebGL compression=BrotliFallback stripping=High template=PROJECT:FruitDefensePortraitContain host=fruit-defense-portrait-contain-v1 size=9642739 payloads=[WebGL.data.unityweb:version=d863a78c5ffc:size=5573023, WebGL.framework.js.unityweb:version=8e822251a31f:size=69111, WebGL.loader.js:version=3005677ef380:size=117893, WebGL.wasm.unityweb:version=682dd0091686:size=3872589]
```

## Acceptance evidence

- `webgl/full-402x874-interaction-polish/`: Lobby rest, selection-motion, selected-rest, and Start-pressed checkpoints plus accepted manifest.
- `webgl/full-402x874-battle-interaction/`: accepted Battle interaction catalog including wave-action motion.
- `webgl/full-402x874-flow-victory-interaction/`: accepted Lobby → Battle → Settlement → Lobby → retry flow including Settlement reveal motion.
- `webgl/desktop-host/`: accepted `1024×640`, `1280×720`, and `1440×900` contain-host evidence.
- `webgl/regressions/legacy-transition-race-attempt-1/` and `attempt-2/`: retained diagnostic captures from the old route-transition screenshot race. `rejected-fake-wave-action-motion.png` retains the rejected click-after-release frame so it cannot be mistaken for accepted motion evidence. The accepted modes use real pointer-down checkpoints followed by verified release actions.

Editor and build verification:

- Focused `FruitDefense.Editor.RuntimeUiInteractionPolishSmoke.Run`: `RUNTIME_UI_INTERACTION_POLISH_OK`.
- Aggregate `FruitDefense.Editor.ProjectSetup.SmokeValidate`: `FRUIT_DEFENSE_SMOKE_OK`, including feedback, performance, visual-system, and interaction-polish checks.
- Ordinary WebGL build: pass.
- Portrait Battle, cross-route flow, interaction checkpoints, and desktop host acceptance: pass.

## Remaining limits

- This change improves interaction and motion, not the approved resting composition; the exact resting-frame matches are intentional.
- APK UI textures, Prefabs, fonts, Spine, and effect assets remain protected inside an unresolved transformed LZMA payload. Their filenames are an inventory, not decoded production resources.
- Reduced motion has an authoritative theme-level policy but no player-facing settings surface yet.
- Captured temporal checkpoints prove visible browser frames and final states; they are not a frame-by-frame perceptual study or device performance benchmark.
