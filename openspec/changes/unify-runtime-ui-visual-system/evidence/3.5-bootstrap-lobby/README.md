# 3.5 Bootstrap / Lobby WebGL visual evidence

## Verdict

Task 3.5 is **accepted** on one rebuilt ordinary-WebGL payload. All eight
full/inset Lobby cases and both formal Bootstrap-error cases pass on the same
payload and runtime identity `ui.sunny-orchard@1 / sunny-orchard@1`. The complete
device-pixel nine-slice partition fix was verified on all four sides, including
the former 360×800 inset selected-card leak at `x=35`.

No temporary runtime hook, scene mutation, loader failure, or default-skin
substitute was used. The error cases use the production acceptance route with the
formal invalid level input `__missing-ui-acceptance__`.

## Accepted payload

- loader: `23dda1fa00d8`
- data: `761196808a41`
- framework: `74a8df0275f8`
- wasm: `577ad40e2527`
- build log: [unity-webgl-build-final-complete-partition.log](webgl-build/unity-webgl-build-final-complete-partition.log)
- style reference: [approved A Sunny Orchard board](../style-board/artset-a-sunny-orchard-style-board.png)

Every final manifest below records the same payload versions and the same release
Theme/ArtSet identity.

## Lobby matrix

Each case captures the real default Lobby, alternate selection of `orchard-02`,
and the pre-navigation `正在进入…` transition. The route/identity sequence is
`Lobby orchard-01 / no session` → `Lobby orchard-02 / no session` →
`Battle orchard-02 / fresh session`, proving that draw and hit geometry did not
drift.

| Case | Safe top/bottom | Default/alternate/loading contrast | Bootstrap initializing | Manifest |
| --- | ---: | ---: | --- | --- |
| 360×800 full | 0 / 0 | 3.21556 / 3.21556 / 3.21556 | short frame not captured | [manifest](full-360x800/shell-visual-evidence.json) |
| 360×800 inset | 32 / 24 | 3.21556 / 3.21556 / 3.21556 | short frame not captured | [manifest](inset-360x800-32-24/shell-visual-evidence.json) |
| 375×812 full | 0 / 0 | 3.21556 / 3.21556 / 3.21556 | short frame not captured | [manifest](full-375x812/shell-visual-evidence.json) |
| 375×812 inset | 40 / 21 | 3.21556 / 3.21556 / 3.21556 | [captured](inset-375x812-40-21/00-bootstrap-initializing.png) | [manifest](inset-375x812-40-21/shell-visual-evidence.json) |
| 402×874 full | 0 / 0 | 3.21556 / 3.21556 / 3.21556 | short frame not captured | [manifest](full-402x874/shell-visual-evidence.json) |
| 402×874 inset | 44 / 34 | 3.21556 / 3.21556 / 3.21556 | short frame not captured | [manifest](inset-402x874-44-34/shell-visual-evidence.json) |
| 430×932 full | 0 / 0 | 3.21556 / 3.21556 / 3.21556 | short frame not captured | [manifest](full-430x932/shell-visual-evidence.json) |
| 430×932 inset | 50 / 36 | 3.21556 / 3.21556 / 3.21556 | short frame not captured | [manifest](inset-430x932-50-36/shell-visual-evidence.json) |

The 375×812 inset transition needed one bounded recapture after the
post-screenshot route assertion rejected a one-attempt race. The accepted image
was captured while the route was still Lobby. No runtime transition duration was
changed. The application-owned initializing modal was captured on the first
bounded 375×812 inset recap after the complete matrix.

## Formal Bootstrap error

| Case | Route ready | Visible finite copy | Non-color cue | Evidence |
| --- | --- | --- | --- | --- |
| 402×874 full | no (`appRoute=-1`) | `启动失败：所选关卡不可用` | error × icon and blocking modal | [image](error-full-402x874/00-bootstrap-blocking-error.png), [manifest](error-full-402x874/shell-error-evidence.json) |
| 402×874 inset 44/34 | no (`appRoute=-1`) | `启动失败：所选关卡不可用` | error × icon and blocking modal | [image](error-inset-402x874-44-34/00-bootstrap-blocking-error.png), [manifest](error-inset-402x874-44-34/shell-error-evidence.json) |

Manual original-resolution review confirms that the finite user copy is fully
visible in both images with no wrap, clipping, or overlap. Raw diagnostic detail
is intentionally not rendered into the fixed single-line status slot; direct P0
coverage verifies that formatting does not mutate the raw blocking-error state.

## Visual review against approved A

- Warm cream base, amber selected card, leaf-green primary action, soil-brown
  outlines, rounded surfaces, and restrained shallow depth match the approved
  Sunny Orchard direction.
- Chinese hierarchy uses the packaged release font. Selection has amber plus a
  checkmark; initializing/loading use text plus spinner; error uses finite copy,
  a modal, and × indicators. Feedback is not color-only.
- No default Unity skin, legacy chrome, black/transparent accepted frame, text
  overlap, component clipping, or input drift was observed.
- Full/inset safe-area samples pass the approved `#FFF6E0` base and `#F5DDAE`
  edge tolerances. Default, alternate, and loading layouts remain aligned.
- The actual primary-action label/background measurement is `3.21556:1` in all
  24 Lobby screenshots. The shared semantic-state direct test reports error
  status text at `5.0910:1` and modal primary text at `5.2719:1`; the final error
  screenshots were also reviewed at original resolution.

## Stretch/seam regression check

The former fractional nine-slice seams are absent in the accepted payload. Exact
samples are recorded in [seam-probes.json](seam-probes.json):

- 360×800 inset selected card left inner boundary: `x=31..35` and `x=37..38`
  are `#FFCB44`; `x=36` is the intended soil-colored art edge. The former
  underlying-base leak at `x=35` is gone;
- the same card's right interior remains `#FFCB44` through `x=331`, followed by
  the intended antialiased outer edge at `x=332..335`;
- its center column remains filled through the top and bottom nine-slice
  partitions (`y=215..270=#FFCB44`) before the intended outer edge, and the
  primary button remains filled through `y=390..430=#559A39`;
- 360×800 full `surface.safe-area`: `x=344/345/346` are all `#FFF6E0` at
  `y=50/450/700/780`;
- 375×812 inset selected card: `x=331/332/333` are all `#FFCB44` at
  `y=155/190`; its primary action is all `#559A39` at `y=415`;
- 430×932 inset selected card: `x=377/378/379` are all `#FFCB44` at
  `y=170/205`; its primary action is all `#559A39` at `y=470`.

Rejected captures remain as regression history and are not release evidence:

- [pre-fix left inner partition leak](regressions/left-seam-pre-fix-839a3a4c5fee/README.md)
- [pre-fix error contrast](regressions/error-contrast-pre-fix-ad29883fcac7/README.md)
- [pre-fix raw error-copy clipping](regressions/error-copy-clipping-pre-fix-1147491fa6d2/README.md)

## Reproduction

Build with Unity `6000.3.19f1`:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' `
  -batchmode -nographics -quit `
  -projectPath 'E:\project\unity\furitDefense' `
  -executeMethod FruitDefense.Editor.WebBuild.Build `
  -logFile '<evidence>/webgl-build/unity-webgl-build-final-complete-partition.log'
```

Representative Lobby capture:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass `
  -File scripts/accept-webgl-portrait.ps1 `
  -ServeLocal -ShellVisual -LevelId orchard-02 `
  -Width 402 -Height 874 -SafeTop 44 -SafeBottom 34 `
  -BootstrapCpuThrottlingRate 20 -TimeoutSeconds 90 `
  -OutputDirectory '<evidence>/inset-402x874-44-34'
```

Representative formal error capture replaces `-ShellVisual -LevelId orchard-02`
with `-ShellError`. The script supplies the fixed invalid-level acceptance input
and rejects route-ready, Unity splash/black frames, wrong dimensions, and missing
application presentation.
