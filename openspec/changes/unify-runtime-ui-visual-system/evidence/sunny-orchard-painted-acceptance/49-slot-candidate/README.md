# 49-slot Sunny Orchard Painted WebGL acceptance

Status: **ACCEPTED** for the scoped ordinary WebGL runtime UI visual/theme/art-set contract.

The candidate uses one release identity across every captured route:

- Theme: `ui.sunny-orchard@1`
- ArtSet: `sunny-orchard-painted@1`
- Active ArtSet GUID: `91aa538ae02449cba8c971ffe4d427eb`
- Required semantic slots: 49

Payload SHA-256:

| Artifact | SHA-256 |
|---|---|
| loader | `677D7B5431FA1EBFB0076D55F5B09E121EB81792E785F2A0CD0F89116E88E5C5` |
| data | `24A7191BDECD4CB3B25E194A927045AE3CAC1356FFFCD9C6BC5F63211075AB35` |
| framework | `377D39D2C40E2BBD6F8DCC8DAFC7B64D30E3AB9F94D598D82752E81F6F1146EE` |
| wasm | `41A4189136E53C1F2703283234812A48248C8574447EB648F9613FE546D2AC3A` |

## Matrix

All 16 canonical manifests are accepted and record the same identity/GUID. The 10-run cross-route count shares the two 402 ShellVisual runs with the eight-size Shell matrix; it does not duplicate them as new captures.

### ShellVisual — 8 runs

| Viewport | Full | Inset |
|---|---|---|
| 360×800 | [manifest](shell-visual/full-360x800/shell-visual-evidence.json) | [32/24 manifest](shell-visual/inset-360x800-32-24/shell-visual-evidence.json) |
| 375×812 | [manifest](shell-visual/full-375x812/shell-visual-evidence.json) | [40/21 manifest](shell-visual/inset-375x812-40-21/shell-visual-evidence.json) |
| 402×874 | [manifest](shell-visual/full-402x874/shell-visual-evidence.json) | [44/34 manifest](shell-visual/inset-402x874-44-34/shell-visual-evidence.json) |
| 430×932 | [manifest](shell-visual/full-430x932/shell-visual-evidence.json) | [50/36 manifest](shell-visual/inset-430x932-50-36/shell-visual-evidence.json) |

Each run contains real default, alternate-selection, and route-transition screenshots. The minimum measured primary-action contrast is 5.7477:1; all default/alternate captures required three consecutive stable frames. Canonical Shell frames have zero black and invalid fractions.

### 402×874 cross-route — 10 runs

| Geometry | ShellVisual | ShellError | Direct Battle | Flow victory | Flow defeat |
|---|---|---|---|---|---|
| full | [manifest](shell-visual/full-402x874/shell-visual-evidence.json) | [manifest](cross-route/full-402x874-shell-error/shell-error-evidence.json) | [manifest](cross-route/full-402x874-battle/acceptance.json) | [manifest](cross-route/full-402x874-flow-victory/flow-acceptance.json) | [manifest](cross-route/full-402x874-flow-defeat/flow-acceptance.json) |
| inset 44/34 | [manifest](shell-visual/inset-402x874-44-34/shell-visual-evidence.json) | [manifest](cross-route/inset-402x874-44-34-shell-error/shell-error-evidence.json) | [manifest](cross-route/inset-402x874-44-34-battle/acceptance.json) | [manifest](cross-route/inset-402x874-44-34-flow-victory/flow-acceptance.json) | [manifest](cross-route/inset-402x874-44-34-flow-defeat/flow-acceptance.json) |

## Manual visual QA

The 49-slot hierarchy materially closes the rejected 40-slot gap:

- Lobby retains recognizable orchard structure with copy hidden: a leaf title ribbon, three distinct fixed-aspect level thumbnails, a large amber selected medallion, and the primary CTA remain identifiable at every supported full/inset size.
- Thumbnail frames remain aspect-correct and leaves stay in protected corner/fixed-aspect art. No leaf enters a nine-slice stretch center; no one-pixel seam, transparent underdraw, clipped border, or stretched ornament was visible on any four-sided full/inset comparison.
- The selected marker does not collide with either Chinese line or the transition indicator. The Start icon plus label reads as one optically centered group at 360 through 430 widths.
- Chinese title, card, status, between-wave, two-line transient, modal, result, and action copy remains complete. Full and inset terminal/result illustration, banner, message, and state indicator occupy separate readable regions.
- Direct Battle real input evidence distinguishes available and selected tool screenshots by SHA-256, and separately captures legal/illegal drag cues, plant detail, pause, terminal preview, and restart without route submission.
- Settlement victory/defeat, Return, and Retry use the painted banner/vista/metric hierarchy without clipping or mixed-set chrome. ShellError shows finite user copy plus a non-color error cue.
- No default Unity skin, legacy style, alternate ArtSet, black/transparent frame, or hit-target drift was observed.

Representative visual links:

- Lobby minimum inset: [360×800 inset selected](shell-visual/inset-360x800-32-24/02-lobby-alternate-selection.png)
- Lobby maximum inset: [430×932 inset transition](shell-visual/inset-430x932-50-36/03-lobby-transition.png)
- Battle full: [selected tool](cross-route/full-402x874-battle/09-selected-tool.png), [plant detail](cross-route/full-402x874-battle/14-plant-detail.png), [terminal](cross-route/full-402x874-battle/17-battle-terminal-victory.png)
- Battle inset: [between wave](cross-route/inset-402x874-44-34-battle/03-between-wave.png), [pause](cross-route/inset-402x874-44-34-battle/05-paused.png), [illegal cue](cross-route/inset-402x874-44-34-battle/12-illegal-drag-cue.png)
- Settlement: [full victory](cross-route/full-402x874-flow-victory/03-settlement-victory.png), [inset defeat](cross-route/inset-402x874-44-34-flow-defeat/03-settlement-defeat.png)

## Acceptance-infrastructure regressions

These are retained as regression history and are not canonical product failures:

- [`timing-regression/full-402x874-attempt-01/`](timing-regression/full-402x874-attempt-01/) captured the initial default during Bootstrap fade and then missed the short transition frame. Shell default capture now requires three consecutive passing frames whose average-luma delta is at most 0.006.
- [`timing-regression/full-430x932-attempt-01/`](timing-regression/full-430x932-attempt-01/) missed the short transition on its first attempt; one bounded serial rerun passed without changing runtime timing.
- [`acceptance-guard-regression/`](acceptance-guard-regression/) showed that the old orange/red sentinel matched only the painted Refresh icon (`x=27..34`, `y=762..768`, 20 pixels) rather than a legacy action row. The guard still requires the pixel threshold and now also requires a horizontal signature wider than 20% of the sampled band.

The capture script resolves the release ArtSet by the Theme's serialized GUID with a unique production asset/meta match; it has no painted filename special case or fallback. All runs used `-ServeLocal` with independent random ports/profiles. The user-visible server on port 4173 was not stopped or overwritten.

## Validation

- Acceptance script self-check: `FRUIT_DEFENSE_ACCEPTANCE_SELF_CHECK_OK`
- ShellVisual: 8/8 accepted
- 402 cross-route: 10/10 accepted, including the two shared ShellVisual runs
- OpenSpec strict validation: passed at final evidence close
- JSON/link/hash/diff checks: passed at final evidence close

Structured summary: [`final-audit.json`](final-audit.json)

Task 3.4 is checked after combining this WebGL acceptance with the final Unity editor workflow evidence in `Logs/runtime-ui-49-direct-v3.log`: both approved 49-slot treatments were previewed, the alternate was atomically activated with Undo/Redo, theme/scene/code/layout bytes were restored, and the selected `sunny-orchard-painted@1` set remained active. The WebGL run alone does not substitute for that editor evidence.
