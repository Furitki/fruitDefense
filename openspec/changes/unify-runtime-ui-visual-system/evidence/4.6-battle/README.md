# Battle runtime UI acceptance evidence (task 4.6)

## Verdict

Task 4.6 is accepted on one rebuilt ordinary-WebGL payload. Four real Battle
runs cover the 402×874 full safe area and representative top-44/bottom-34 inset,
with victory and defeat terminal previews in each geometry. All runs use
`ui.sunny-orchard@1 / sunny-orchard@1`, contain 18 screenshots, report
`accepted=true`, and have no failed manifest check.

The accepted matrix contains only real WebGL canvas captures. Rejected earlier
payloads are retained separately under `regressions/` and are not release
evidence.

## Accepted payload

- loader: `81b6c1f7f0cf`
- data: `489a222d6a70`
- framework: `74a8df0275f8`
- wasm: `76bb8318d749`
- build log: [unity-webgl-build-between-wave-single-line-final.log](webgl-build/unity-webgl-build-between-wave-single-line-final.log)
- exact hashes and pixel probes: [visual-probes.json](visual-probes.json)

## Real WebGL matrix

| Geometry | Outcome | Manifest | Terminal frame |
| --- | --- | --- | --- |
| 402×874 full | victory | [manifest](accepted-full-402x874-victory/acceptance.json) | [victory](accepted-full-402x874-victory/17-battle-terminal-victory.png) |
| 402×874 full | defeat | [manifest](accepted-full-402x874-defeat/acceptance.json) | [defeat](accepted-full-402x874-defeat/17-battle-terminal-defeat.png) |
| 402×874 inset 44/34 | victory | [manifest](accepted-inset-402x874-44-34-victory/acceptance.json) | [victory](accepted-inset-402x874-44-34-victory/17-battle-terminal-victory.png) |
| 402×874 inset 44/34 | defeat | [manifest](accepted-inset-402x874-44-34-defeat/acceptance.json) | [defeat](accepted-inset-402x874-44-34-defeat/17-battle-terminal-defeat.png) |

Every directory records the same state sequence:

1. ready;
2. active wave;
3. between-wave countdown and immediate-next-wave action;
4. immediate next wave;
5. paused;
6. continued;
7. restarted;
8. Gatling available;
9. Gatling selected by a real click;
10. adjacent pots;
11. legal drag cue;
12. illegal drag cue;
13. dense board;
14. plant detail;
15. destination click without movement;
16. completed drag relocation;
17. Battle terminal victory or defeat;
18. terminal-preview restart back to Ready.

## Interaction and behavior evidence

- The URL-guarded `selected-tool` acceptance state supplies one Gatling, then
  the script clicks the production tool hit rectangle. Available and selected
  captures have different SHA-256 values in all four runs and visibly change to
  the amber/check selected treatment. No unguarded runtime path can enter this
  acceptance state.
- Legal and illegal drag captures differ across the board/status region and use
  both text/glyph and color. At the full reference sample `(10,166)`, legal is
  `#6EBF4B` and illegal is `#D95147`; the inset sample is also distinct.
- Click-only destination input leaves the source plant in place; the subsequent
  real drag relocates it. The manifest records both checks as passing.
- Pause/Continue retains the run. Pause/Restart returns to a clean Ready state.
  Terminal-preview Restart dismisses the result card, keeps route `Battle`, and
  retains the same session without submitting a result.
- Stable terminal preview states are reachable only through the existing
  `acceptance=1` bridge. Normal terminal result submission is not suppressed;
  actual Battle→Settlement submission and route behavior are covered by task
  5.3 rather than simulated here.

## Visual review

- The rebuilt `03-between-wave.png` in all four runs renders
  `下一波倒计时 9 秒` and `立即开始下一波` as complete single lines. There is no
  wrap, clipping, overlap, or change to the original draw/hit rectangles.
- Ready, active, dense, detail, paused, terminal, and restarted frames retain the
  approved warm cream/leaf green/amber/soil brown hierarchy and packaged Chinese
  font. No Unity default skin, legacy chrome, mixed ArtSet, or fallback surface
  is visible.
- The single identity-space background is continuous in full and inset frames.
  The former right-side crescent and top-left black ellipse are absent. Inset
  probes across both former defect regions remain warm opaque background colors.
- Original-resolution review of panel, tool-card, status/action, detail-card,
  pause-modal, and terminal-card edges found no isolated background column/row,
  missing outer pixel, nine-slice seam, or stretched corner.
- All 72 accepted screenshots are 402×874. Across each run, the largest sampled
  black, near-black, and invalid fractions are at most `0.0005425`, `0.0016916`,
  and `0.0005425`, below the script limits `0.01`, `0.05`, and `0.05`.
- The aggregate visual-system gate reports primary/loading contrast `3.2156:1`,
  semantic status contrast at least `3.6334:1`, modal title `5.2719:1`, and both
  production screen backgrounds fully opaque.

## Editor and deterministic evidence

The final shared-text validation logs are copied into [editor-smoke](editor-smoke/):

- [layout/font-fit](editor-smoke/between-wave-single-line-layout.log): packaged
  Noto font, exact 9/10-second status copy, complete action copy, 402 full/inset
  `CalcSize`, single-line fit, and unchanged interaction rectangles;
- [presentation boundary](editor-smoke/between-wave-single-line-presentation.log);
- [session/restart](editor-smoke/between-wave-single-line-session.log);
- [deterministic simulation](editor-smoke/between-wave-single-line-deterministic.log);
- [aggregate P0](editor-smoke/between-wave-single-line-p0.log): visual validator
  valid with zero warnings, opaque backgrounds, contrast markers, and
  `FRUIT_DEFENSE_P0_RELEASE_GATE_OK`.

## Rejected regression history

- [terminal transition before stable preview](regressions/terminal-transition-pre-preview-761196808a41/README.md)
- [duplicate inset background](regressions/double-background-pre-fix-4cf167fdd44d/README.md)
- [transparent background export](regressions/transparent-screen-background-pre-fix-4cf167fdd44d/README.md)
- [unavailable selected tool](regressions/selected-tool-unavailable-pre-fix-7a3ae99510ad/README.md)
- [between-wave text clipping](regressions/between-wave-text-clip-pre-fix-f7a3c8279624/README.md)

## Reproduction

Build with Unity `6000.3.19f1`:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' `
  -batchmode -nographics -quit `
  -projectPath 'E:\project\unity\furitDefense' `
  -executeMethod FruitDefense.Editor.WebBuild.Build `
  -logFile '<evidence>/webgl-build/unity-webgl-build-between-wave-single-line-final.log'
```

Representative inset victory run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass `
  -File scripts/accept-webgl-portrait.ps1 `
  -ServeLocal -Width 402 -Height 874 -SafeTop 44 -SafeBottom 34 `
  -LevelId orchard-01 -BattleTerminalOutcome victory -TimeoutSeconds 60 `
  -OutputDirectory '<evidence>/accepted-inset-402x874-44-34-victory'
```

Use safe insets `0/0` for the full case and terminal outcome `defeat` for the
second outcome. Each run uses its own server/profile and cleans both up before
exit. The task-local payload is preserved in `Builds/WebGL`; later cross-route
acceptance owns any final all-route payload consolidation.
