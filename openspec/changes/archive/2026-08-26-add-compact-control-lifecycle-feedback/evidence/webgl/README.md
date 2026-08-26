# Semantic action container/content WebGL evidence

This directory records the accepted WebGL evidence for the semantic action-button system. The current reference is:

- `semantic-action-v4-full-402x874-battle`
- `semantic-action-v4-inset-402x874-battle`

All earlier `full-*`, `inset-*`, `refined-*`, and `v3-full-surface-*` directories are retained only as rejected regression history. In particular, the `v3` two-surface cross-fade is not the current solution and must not be restored.

The accepted implementation selects one complete semantic surface per frame. Container and content color are resolved as one pair for `Primary`, `Secondary`, `Quiet`, `Danger`, `ModeActive`, and `Disabled`; glyph, label, and multiplier share that content color. Persistent mode changes select another complete endpoint instead of blending or overlaying a second button. Hover or keyboard focus adds one contained four-segment inner cue, while press feedback changes geometry without introducing another surface.

Battle mappings exercised by this evidence are:

- start wave: `Primary`
- refresh five fruits: `Secondary`
- pause and speed: `Quiet`, with persistent active endpoints
- close: `Quiet`, instant command

## Build identity

- loader `2ff7e34b04c4`
- data `b128cbb27ef7`
- framework `b12d22343ced`
- wasm `075137a24bb9`
- total payload `10,266,792` bytes
- theme / ArtSet `ui.sunny-orchard@2 / sunny-orchard-painted@1`

## Accepted checkpoints

- [Full ready](semantic-action-v4-full-402x874-battle/01-ready.png)
- [Full start-wave focus](semantic-action-v4-full-402x874-battle/01a-wave-action-hover.png)
- [Full start-wave press](semantic-action-v4-full-402x874-battle/02a-wave-action-pressed.png)
- [Full speed focus](semantic-action-v4-full-402x874-battle/01c-speed-hover-1x.png)
- [Full speed active](semantic-action-v4-full-402x874-battle/01f-speed-active-2x.png)
- [Full paused](semantic-action-v4-full-402x874-battle/05-paused.png)
- [Full close focus](semantic-action-v4-full-402x874-battle/14a-detail-close-hover.png)
- [Full close press](semantic-action-v4-full-402x874-battle/14b-detail-close-pressed.png)
- [Inset ready](semantic-action-v4-inset-402x874-battle/01-ready.png)
- [Inset start-wave focus](semantic-action-v4-inset-402x874-battle/01a-wave-action-hover.png)
- [Inset speed active](semantic-action-v4-inset-402x874-battle/01f-speed-active-2x.png)
- [Inset paused](semantic-action-v4-inset-402x874-battle/05-paused.png)

The machine-readable [full manifest](semantic-action-v4-full-402x874-battle/acceptance.json) and [inset manifest](semantic-action-v4-inset-402x874-battle/acceptance.json) record viewport, safe area, build identity, theme, coordinates, state sequence, contrast, focus, press, and containment metrics. Both report `accepted=true`, `compactControlLifecycle=pass`, `compactControlInstantClose=pass`, and `actionFocusCue=pass`.

## Final-pixel results

| Check | Full | Inset | Gate |
|---|---:|---:|---:|
| Primary start-wave content contrast | `5.699:1` | `5.699:1` | `>= 4.5:1` |
| Secondary refresh content contrast | `4.914:1` | `4.880:1` | `>= 4.5:1` |
| Start-wave contained focus pixels | `720` | `668` | material change, no owner-edge spill |
| Speed contained focus pixels | `224` | `208` | material change, no owner-edge spill |

`Disabled` has no stable production action in this battle flow. Its final-pixel contrast and alpha-mask behavior are therefore covered deterministically by the Editor acceptance suite; no fake runtime state or acceptance-only production path is introduced.

Manual review of the full and inset rasters confirmed that the play glyph remains distinct from its deep-leaf surface, the refresh action reads as lower emphasis without losing contrast, compact controls have one silhouette, focus cues stay contained, active modes remain legible, and no control clips or overlaps neighboring content.

## Automated results

- Aggregate Editor smoke: `FRUIT_DEFENSE_SMOKE_OK`, Unity return code `0`.
- Compact lifecycle smoke: `COMPACT_CONTROL_LIFECYCLE_SMOKE_OK`.
- Compact final-composite smoke: `COMPACT_CONTROL_ACCEPTANCE_SMOKE_OK`.
- Runtime visual-system smoke: `RUNTIME_UI_VISUAL_SYSTEM_SMOKE_OK`.
- Ordinary WebGL build: `FRUIT_DEFENSE_WEB_BUILD_OK`, Unity return code `0`.
- Full real-canvas acceptance: `FRUIT_DEFENSE_VISUAL_ACCEPTANCE_OK`.
- Inset real-canvas acceptance: `FRUIT_DEFENSE_VISUAL_ACCEPTANCE_OK`.

## Reproduction commands

```powershell
Unity.exe -batchmode -nographics -quit -projectPath . -executeMethod FruitDefense.Editor.ProjectSetup.SmokeValidate
Unity.exe -batchmode -nographics -quit -projectPath . -executeMethod FruitDefense.Editor.WebBuild.Build
scripts/accept-webgl-portrait.ps1 -ServeLocal -InteractionPolishEvidence -CompactControlEvidence -Width 402 -Height 874 -SafeTop 0 -SafeBottom 0 -OutputDirectory openspec/changes/add-compact-control-lifecycle-feedback/evidence/webgl/semantic-action-v4-full-402x874-battle
scripts/accept-webgl-portrait.ps1 -ServeLocal -InteractionPolishEvidence -CompactControlEvidence -Width 402 -Height 874 -SafeTop 40 -SafeBottom 24 -OutputDirectory openspec/changes/add-compact-control-lifecycle-feedback/evidence/webgl/semantic-action-v4-inset-402x874-battle
```
