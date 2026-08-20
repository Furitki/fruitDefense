# Cross-route WebGL visual matrix (task 6.3)

## Verdict

Task 6.3 is accepted. Ten ordinary-WebGL runs cover 402×874 full and
top-44/bottom-34 inset geometry across ShellVisual, formal ShellError, direct
Battle, Flow victory, and Flow defeat. The 64 canonical screenshots and all ten
manifests use the exact task 6.2 payload and the same release UI identity:

`ui.sunny-orchard@1 / sunny-orchard@1`

No runtime, scene, art, or acceptance-script source was changed for this task.

## Fixed payload

| File | SHA-256 |
| --- | --- |
| loader | `e0fd8292100517f441ce1e5ab1eb7a8219aa40919e46f125667090633a9b9388` |
| data | `cc0a2b18a0c194bd7aafaff1e0b6b49247162799d54671ca892045612e938a60` |
| framework | `628ded04a7af9570e4032627edcf6f973928ea5e8aa51c10f5ed97150760fc77` |
| wasm | `868b0585638663a2ad55c2d2ba62dac4caee134ba340e01ebc53c841b25e084a` |

The full build and aggregate validation are owned by
[task 6.2](../6.2-aggregate-build/README.md). Every manifest below independently
records these same four content hashes, theme/art-set revisions, active ArtSet
GUID `12cc0c638d174040bb0384d7bf17ea92`, viewport, safe area, and route/session
metadata. The consolidated machine-readable review is
[matrix-audit.json](matrix-audit.json).

## Matrix

| Geometry | Mode | Captures | Evidence |
| --- | --- | ---: | --- |
| full | ShellVisual | 3 | [manifest](full-402x874-shell-visual/shell-visual-evidence.json) |
| full | ShellError | 1 | [manifest](full-402x874-shell-error/shell-error-evidence.json) |
| full | direct Battle | 18 | [manifest](full-402x874-battle/acceptance.json) |
| full | Flow victory | 5 | [manifest](full-402x874-flow-victory/flow-acceptance.json) |
| full | Flow defeat | 5 | [manifest](full-402x874-flow-defeat/flow-acceptance.json) |
| inset 44/34 | ShellVisual | 3 | [manifest](inset-402x874-44-34-shell-visual/shell-visual-evidence.json) |
| inset 44/34 | ShellError | 1 | [manifest](inset-402x874-44-34-shell-error/shell-error-evidence.json) |
| inset 44/34 | direct Battle | 18 | [manifest](inset-402x874-44-34-battle/acceptance.json) |
| inset 44/34 | Flow victory | 5 | [manifest](inset-402x874-44-34-flow-victory/flow-acceptance.json) |
| inset 44/34 | Flow defeat | 5 | [manifest](inset-402x874-44-34-flow-defeat/flow-acceptance.json) |

All ten commands ended with their mode-specific success marker. The two
ShellVisual manifests mark the optional, extremely short Bootstrap-initializing
frame as not captured and point error review to a separate formal capture. Both
required Lobby Loading transition frames are present, and both dedicated
ShellError runs provide the missing blocking-error evidence. The ShellError
manual-review marker is satisfied by the original-resolution review below.

## Route, session, and input review

- ShellVisual preserves no-session Lobby identity across default and alternate
  selection, maps the real selected-card and Start hit targets, then creates a
  non-empty Battle session for `orchard-02`.
- ShellError uses the production invalid-level acceptance input, publishes no
  route-ready state (`appRoute=-1`), and renders the finite
  `启动失败：所选关卡不可用` copy with modal plus error glyph.
- Direct Battle preserves route `Battle`, a non-empty acceptance session, and
  deterministic seed through Ready, active/between wave, pause/continue,
  restart, tool selection, legal/illegal drag, detail, terminal preview, and
  terminal-preview restart. Available→real click→selected image hashes differ
  in both geometries.
- Each Flow run preserves the first session from Battle to Settlement, clears it
  on Return, creates a distinct second Battle/Settlement session, and creates a
  third distinct session for Retry. Victory and defeat use the expected result
  data and both Return/Retry hit targets pass without geometry drift.

## Original-resolution visual review

- Lobby default and alternate cards retain the approved hierarchy. The 7.1
  Loading emphasis is clear through `正在进入…`, per-card spinner glyphs, and the
  primary action spinner; full/inset action contrast is `3.21556:1`.
- Formal error copy is complete, single-line, and accompanied by a distinct
  error glyph and blocking modal. It is not color-only.
- Battle `下一波倒计时 9 秒` and `立即开始下一波` remain complete single lines in
  full and inset captures. Selected-tool amber/check treatment, legal/illegal
  text and target cues, plant detail, pause, dense state, and terminal result
  remain visually distinct.
- The Battle screen background is continuous and opaque. No former right-side
  crescent, top-left transparent ellipse, missing outer pixel, or isolated
  nine-slice row/column appears on header, status/action, tool/slot, detail,
  pause, or terminal surfaces.
- Settlement victory/defeat wording and success/error glyphs are distinct.
  Result metrics, Retry, and Return fit without clipping in both geometries;
  returned Lobby selection and retry Battle state are visibly correct.
- Across Bootstrap, Lobby, Battle, Settlement, and their modals, no Unity default
  skin, legacy chrome, mixed ArtSet, clipping, stretching, overlap, contrast
  failure, or input drift was observed. The largest sampled black, near-black,
  and invalid fractions are `0.0006329`, `0.0015371`, and `0.0006329`, well below
  the acceptance limits `0.01`, `0.05`, and `0.05`.

## Reproduction

Representative full commands:

```powershell
& .\scripts\accept-webgl-portrait.ps1 -ServeLocal -ShellVisual `
  -LevelId orchard-02 -Width 402 -Height 874 -SafeTop 0 -SafeBottom 0 `
  -BootstrapCpuThrottlingRate 20 -TimeoutSeconds 90 -OutputDirectory '<shell-visual>'

& .\scripts\accept-webgl-portrait.ps1 -ServeLocal -ShellError `
  -Width 402 -Height 874 -SafeTop 0 -SafeBottom 0 -TimeoutSeconds 90 `
  -OutputDirectory '<shell-error>'

& .\scripts\accept-webgl-portrait.ps1 -ServeLocal -LevelId orchard-01 `
  -BattleTerminalOutcome victory -Width 402 -Height 874 `
  -SafeTop 0 -SafeBottom 0 -TimeoutSeconds 90 -OutputDirectory '<battle>'

& .\scripts\accept-webgl-portrait.ps1 -ServeLocal -Flow -LevelId orchard-01 `
  -SettlementOutcome victory -Width 402 -Height 874 `
  -SafeTop 0 -SafeBottom 0 -TimeoutSeconds 90 -OutputDirectory '<flow-victory>'
```

Repeat Flow with `-SettlementOutcome defeat`, and replace safe insets with
`-SafeTop 44 -SafeBottom 34` for the representative inset matrix. Runs are
serialized so each owns its local server and Chrome profile.

## Rejected host-concurrency attempt

The first five-way parallel launch exceeded the acceptance host's CDP/WebGL
resource envelope. Partial WebSocket/warm-reload failures and the one result
captured under that load are retained only in
[regressions/parallel-overload-attempt](regressions/parallel-overload-attempt/README.md).
All ten canonical directories were produced serially on the unchanged payload.
