# Runtime UI final acceptance

## Verdict and scope

Task 7.4 is accepted for the release runtime UI visual system on ordinary
WebGL. The evidence covers the injected release theme, active ArtSet, portrait
geometry, UI state presentation, and the existing acceptance-route input and
navigation observations. It does not claim gameplay correctness, persistence,
mini-game platform support, or a design-overview change.

All canonical captures below use `ui.sunny-orchard@1 / sunny-orchard@1` and
active ArtSet GUID `12cc0c638d174040bb0384d7bf17ea92`. Candidate B remains
preserved, pending, and non-active; task 3.4 remains unchecked.

The preceding sentence records the state at the signed acceptance run. The user
subsequently rejected B, and the task-7.3 follow-up removed its production
source/runtime/ArtSet assets while retaining the historical review evidence.
This post-acceptance repository cleanup does not change the A-only payload or
the acceptance facts below; task 3.4 remains unchecked.

## Final payload

The independent Unity `6000.3.19f1` WebBuild completed successfully and left
`Builds/WebGL` intact.

| File | SHA-256 |
|---|---|
| `WebGL.loader.js` | `851fcdde61eef1484651f2967dacaa331ac377b0b8466d78c7557aaf1e0a507e` |
| `WebGL.data.unityweb` | `f6ef5e73ae2c98afee676d099f7b3a0eda2dcd946ef76362e456aaf759f24044` |
| `WebGL.framework.js.unityweb` | `628ded04a7af9570e4032627edcf6f973928ea5e8aa51c10f5ed97150760fc77` |
| `WebGL.wasm.unityweb` | `1e2ae62ab967aba634bef0d8aa3e5726eab40cbf20acbc7893fc4a43d55c7015` |
| `index.html` | `b9da71958fb5c463f1e146dc9904fcaf034ed47841f1d926dd6b652e22776091` |

The four build payloads total `8,885,317` bytes; the build marker reports
`8,905,120` bytes for the complete output. See the [WebBuild log](webgl-build/webgl-build.log).

## Editor gates

The single final aggregate P0 run completed with return code 0. The
[aggregate log](editor/p0-aggregate.log) contains all required markers:

- deterministic simulation and battle session host validation;
- `FRUIT_DEFENSE_SHELL_OK`, `RUNTIME_UI_FEEDBACK_TIMING_OK`, and
  `RUNTIME_UI_GLYPH_COVERAGE_OK`;
- binding-cache, performance, visual-system, project smoke, and
  `FRUIT_DEFENSE_P0_RELEASE_GATE_OK`;
- Battle UI layout/presentation coverage using packaged Noto Sans SC, including
  the compact two-line transient status contract.

No C# compiler failure, batch abort, or nonzero return marker is present.

## Supported portrait Shell matrix

All eight ordinary-WebGL ShellVisual manifests are accepted. Each directory
contains default, alternate selection, and Loading transition captures; three
runs also captured the short Bootstrap initializing frame.

| Geometry | Manifest |
|---|---|
| 360x800 full | [manifest](shell-visual/full-360x800/shell-visual-evidence.json) |
| 360x800 inset 32/24 | [manifest](shell-visual/inset-360x800-32-24/shell-visual-evidence.json) |
| 375x812 full | [manifest](shell-visual/full-375x812/shell-visual-evidence.json) |
| 375x812 inset 40/21 | [manifest](shell-visual/inset-375x812-40-21/shell-visual-evidence.json) |
| 402x874 full | [manifest](shell-visual/full-402x874/shell-visual-evidence.json) |
| 402x874 inset 44/34 | [manifest](shell-visual/inset-402x874-44-34/shell-visual-evidence.json) |
| 430x932 full | [manifest](shell-visual/full-430x932/shell-visual-evidence.json) |
| 430x932 inset 50/36 | [manifest](shell-visual/inset-430x932-50-36/shell-visual-evidence.json) |

All 24 Lobby action measurements report `3.21555998483371:1`. Loading remains
recognizable through both text and spinner cues; selection is not color-only.

## 402x874 cross-route matrix

The 402x874 full and inset ShellVisual runs above are shared once as the Shell
entry for this matrix. The following eight additional runs make the matrix
10/10 without duplicating those two manifests.

| Geometry | State path | Manifest |
|---|---|---|
| full | Bootstrap blocking error | [manifest](cross-route/full-402x874-shell-error/shell-error-evidence.json) |
| full | direct Battle | [manifest](cross-route/full-402x874-battle/acceptance.json) |
| full | victory flow | [manifest](cross-route/full-402x874-flow-victory/flow-acceptance.json) |
| full | defeat flow | [manifest](cross-route/full-402x874-flow-defeat/flow-acceptance.json) |
| inset 44/34 | Bootstrap blocking error | [manifest](cross-route/inset-402x874-44-34-shell-error/shell-error-evidence.json) |
| inset 44/34 | direct Battle | [manifest](cross-route/inset-402x874-44-34-battle/acceptance.json) |
| inset 44/34 | victory flow | [manifest](cross-route/inset-402x874-44-34-flow-victory/flow-acceptance.json) |
| inset 44/34 | defeat flow | [manifest](cross-route/inset-402x874-44-34-flow-defeat/flow-acceptance.json) |

All 16 canonical manifests are `accepted=true`, use the exact payload above,
and agree on the release theme, ArtSet revision/GUID, requested viewport, safe
area, route identity, session identity, and scripted input checks. The largest
sampled black, near-black, and invalid fractions are respectively
`0.0006329400`, `0.0014467200`, and `0.0006329400`.

## Original-resolution review

- The full and inset [plant-detail](cross-route/full-402x874-battle/14-plant-detail.png)
  captures now show `正在查看豌豆；` and `拖动可移动或合成` as complete controlled
  lines. The independent indicator does not overlap either line, and the status
  card does not overlap the WaveAction button. The inset counterpart is
  [here](cross-route/inset-402x874-44-34-battle/14-plant-detail.png).
- Both [full](cross-route/full-402x874-battle/03-between-wave.png) and
  [inset](cross-route/inset-402x874-44-34-battle/03-between-wave.png)
  between-wave captures preserve the countdown and action labels as complete
  single lines.
- Tool available-to-selected captures are hash-distinct in both geometries and
  visibly use the amber/check selection treatment. Legal and illegal drag cue
  captures are also hash-distinct and retain non-color visual cues.
- Ready, active, paused, dense, detail, terminal, restart, victory/defeat,
  Return, and Retry captures show no Unity default skin, legacy chrome, mixed
  ArtSet, clipped control, stretched corner, overlap, contrast failure, or
  input-coordinate drift.
- Backgrounds are warm and opaque. No former right-side crescent, top-left
  transparent ellipse, missing outer pixel, or isolated horizontal/vertical
  nine-slice seam was observed.

The machine-readable summary is [final-audit.json](final-audit.json).

## Rejected attempts retained

- The first complete 7.4 payload is retained as a rejected
  [plant-detail text-clipping regression](regressions/plant-detail-status-clip-pre-fix-e0fd82921005/README.md).
- One post-fix 375 inset Shell attempt lost the short transition frame and is
  retained as an [acceptance timing regression](regressions/transition-race-final-attempt-1-inset-375x812-40-21/README.md).
- The earlier transition-race record remains under
  `regressions/transition-race-attempt-1/`.

None of these directories is canonical acceptance evidence.

## Final cleanup

All runtime `server.stdout.log` and `server.stderr.log` files were removed from
the change evidence after capture. `GeneratedInvalid`, the removed
`RuntimeUiWarmProfile` helper/source, and acceptance-owned Unity, Chrome, and
server processes are absent. The final WebGL build remains intact for review.

## Reproduction

```powershell
openspec validate unify-runtime-ui-visual-system --strict

& 'C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' `
  -batchmode -nographics -quit `
  -projectPath 'E:\project\unity\furitDefense' `
  -executeMethod FruitDefense.Editor.P0ValidationSuite.Run `
  -logFile '<evidence>\editor\p0-aggregate.log'

& 'C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' `
  -batchmode -nographics -quit `
  -projectPath 'E:\project\unity\furitDefense' `
  -executeMethod FruitDefense.Editor.WebBuild.Build `
  -logFile '<evidence>\webgl-build\webgl-build.log'
```

The accepted browser runs use `scripts/accept-webgl-portrait.ps1` with
`-ServeLocal` and explicit width, height, safe-area, mode, level/outcome, and
output-directory arguments matching the tables above.
