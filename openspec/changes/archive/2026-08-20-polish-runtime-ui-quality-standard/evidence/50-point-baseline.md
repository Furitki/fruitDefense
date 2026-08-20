# 50-point painted runtime UI baseline

## Frozen baseline

This audit freezes the already captured `sunny-orchard-painted@1` ordinary-WebGL
payload as the starting point for this change. It does **not** reclassify the
previous visual-system acceptance as a quality-standard pass. The earlier run
proved resource identity, route coverage, safe-area behavior, input mapping,
and broad rendering integrity; the stricter review below scores typography,
optical alignment, rendered contrast, component spacing, and route composition.

- Theme: `ui.sunny-orchard@1`
- ArtSet: `sunny-orchard-painted@1`
- Active ArtSet GUID: `91aa538ae02449cba8c971ffe4d427eb`
- Semantic slots: 49
- Baseline score: **50 / 100**
- Baseline status for this change: **fails; Blocker/High defects remain open**

| Payload artifact | SHA-256 |
| --- | --- |
| loader | `677D7B5431FA1EBFB0076D55F5B09E121EB81792E785F2A0CD0F89116E88E5C5` |
| data | `24A7191BDECD4CB3B25E194A927045AE3CAC1356FFFCD9C6BC5F63211075AB35` |
| framework | `377D39D2C40E2BBD6F8DCC8DAFC7B64D30E3AB9F94D598D82752E81F6F1146EE` |
| wasm | `41A4189136E53C1F2703283234812A48248C8574447EB648F9613FE546D2AC3A` |

The payload and the complete 8-run Shell plus 10-run 402 cross-route matrix are
recorded in the [previous canonical acceptance][previous-canonical]. No Unity
or browser process was started for this baseline audit, and the user-visible
server on port 4173 was not stopped or overwritten.

## Visual authorities

1. [Approved Sunny Orchard style board][approved-board] — direction, route
   hierarchy, typography relationships, orchard material, and state language.
2. [Painted component proof v2][painted-v2] — optical treatment for the
   selectable card, primary action, three-metric group, state badges, result
   banner, and plant detail.
3. [Active 49-slot painted resource inventory][resource-inventory] — current
   runtime ownership and measurable resource geometry.
4. Existing supported portrait canvases and authoritative layout code — real
   geometry wins over literal mockup reproduction, but it does not excuse a
   failed hierarchy or unreadable compact adaptation.

## Representative full/inset evidence

| Route/state | Full screenshot | Inset screenshot | Baseline observation |
| --- | --- | --- | --- |
| Bootstrap initializing | [402 full initializing][bootstrap-loading] | Not stably captured in the previous canonical payload | Finite cue exists; modal is visually sparse and text is dimmed by Loading opacity. |
| Bootstrap error | [402 full error][bootstrap-error-full] | [402 inset error][bootstrap-error-inset] | Copy is finite, but the modal/status show duplicated error cues and weak hierarchy. |
| Lobby default | [402 full][lobby-full] | [402 inset 44/34][lobby-inset] | Painted cards and thumbnails are recognizable; essential content ends near mid-screen. |
| Lobby selected | [402 full][lobby-selected-full] | [360 inset 32/24][lobby-selected-min] | Selection marker and amber surface read; CTA exposes internal `orchard-*` ID. |
| Lobby Loading | [402 full][lobby-loading-full] | [402 inset 44/34][lobby-loading-inset] | Spinner is non-color feedback; opacity lowers player-copy contrast. |
| Battle between-wave | [402 full][battle-between-full] | [402 inset][battle-between-inset] | Finite single lines fit, but compact status and header hierarchy are visually weak. |
| Battle selected tool | [402 full][battle-selected-full] | [402 inset][battle-selected-inset] | Click changes the selected state, while status/tray text presses against component edges. |
| Battle legal/illegal cue | [full legal][battle-legal-full] / [full illegal][battle-illegal-full] | [inset legal][battle-legal-inset] / [inset illegal][battle-illegal-inset] | Hashes differ, but the 24-point cue's visible legal glyph is too small to be a strong perceptual distinction. |
| Battle plant detail | [402 full][battle-detail-full] | [402 inset][battle-detail-inset] | Copy is finite; detail surface and body hierarchy remain low-emphasis and dense. |
| Battle paused | [402 full][battle-paused-full] | [402 inset][battle-paused-inset] | Modal is contained, but title/message/action relationships are flatter than the reference. |
| Battle terminal | [402 full][battle-terminal-full] | [402 inset][battle-terminal-inset] | Result banner, message, leaf ornament, and indicator overlap. This is a Blocker. |
| Settlement victory | [402 full][settlement-victory-full] | [402 inset][settlement-victory-inset] | Correct route hierarchy exists, but a 16:9 vista is placed in a portrait destination and appears very small. |
| Settlement defeat | [402 full][settlement-defeat-full] | [402 inset][settlement-defeat-inset] | Victory/defeat are distinguishable; duplicate state cues and large unused lower space remain. |
| Return / Retry | [returned Lobby][settlement-return] | [retry Battle][settlement-retry] | Real inputs passed and route/session behavior is protected. |

## Scored audit

The weighting deliberately favors player-visible hierarchy and readability over
implementation completeness. A category receives credit only for what the
frozen screenshots and static resource/layout facts prove.

| Category | Weight | Score | Evidence-based rationale |
| --- | ---: | ---: | --- |
| Typography, copy bounds, and baseline alignment | 15 | **5** | Lobby copy fits, but Battle header metric labels have no usable vertical region; tray labels touch borders; several statuses use non-finite Standard wrapping. |
| Route composition and approved hierarchy | 20 | **6** | Lobby thumbnails and Settlement result anatomy exist, but Lobby/Settlement occupy only the upper portion of the safe area, Battle remains engineering-dense, and terminal composition overlaps. |
| Component spacing and optical alignment | 10 | **5** | CTA grouping and selected marker are sound; tray titles, terminal content, repeated header metrics, and several indicator/icon centroids are not. |
| Semantic color and rendered-state contrast | 10 | **5** | Normal text tokens and the measured primary CTA pass; Loading/Disabled opacity produces approximately `2.99:1` / `2.34:1` for primary text over the base token. |
| Icon weight and non-color state cues | 10 | **5** | Icons are painted and semantic, but legal-drag alpha width is only 52/96 and speed/warning/error optical centers exceed the proposed tolerance. |
| Resource identity, illustration export, and nine-slice integrity | 10 | **8** | One active 49-slot set, stable GUIDs, opaque background, and four-sided seam evidence pass. Settlement's aspect-mismatched vista destination and compact use of ornaments lose credit. |
| Supported portrait, safe area, touch, and input stability | 15 | **9** | 360/375/402/430 full/inset evidence and real hit mapping pass; proposed Lobby/Settlement geometry changes must re-prove these contracts. |
| Required state completeness and cross-route consistency | 10 | **7** | All required states are represented with one theme/ArtSet; several are technically distinct but not yet visually strong enough for the new standard. |
| **Total** | **100** | **50** | **Baseline only; not publishable under this change.** |

## What the baseline already proves

- No Unity default skin, legacy chrome, mixed ArtSet, black/transparent frame,
  or known four-sided nine-slice seam is present in the canonical captures.
- Selected-tool available/clicked hashes differ; Return, Retry, pause, restart,
  and route/session checks are real inputs rather than synthetic screenshots.
- Lobby thumbnail aspect-fit, selected marker containment, and CTA hit mapping
  work at every supported full/inset size.
- The baseline primary-action screenshot measurement is `5.7477:1`.

These passes remain regression constraints. They do not offset any open
Blocker/High item in the [severity-ranked defect inventory][defects].

[approved-board]: ../../../../Assets/UI/Art/Sources/ReferenceBoards/Approved/sunny-orchard-style-board.png
[painted-v2]: ../../unify-runtime-ui-visual-system/evidence/sunny-orchard-redo-proof/sunny-orchard-core-components-v2.png
[previous-canonical]: ../../unify-runtime-ui-visual-system/evidence/sunny-orchard-painted-acceptance/49-slot-candidate/README.md
[resource-inventory]: resource-inventory/resource-inventory.json
[defects]: severity-ranked-defects.md
[bootstrap-loading]: ../../unify-runtime-ui-visual-system/evidence/sunny-orchard-painted-acceptance/49-slot-candidate/timing-regression/full-402x874-attempt-01/00-bootstrap-initializing.png
[bootstrap-error-full]: ../../unify-runtime-ui-visual-system/evidence/sunny-orchard-painted-acceptance/49-slot-candidate/cross-route/full-402x874-shell-error/00-bootstrap-blocking-error.png
[bootstrap-error-inset]: ../../unify-runtime-ui-visual-system/evidence/sunny-orchard-painted-acceptance/49-slot-candidate/cross-route/inset-402x874-44-34-shell-error/00-bootstrap-blocking-error.png
[lobby-full]: ../../unify-runtime-ui-visual-system/evidence/sunny-orchard-painted-acceptance/49-slot-candidate/shell-visual/full-402x874/01-lobby-default.png
[lobby-inset]: ../../unify-runtime-ui-visual-system/evidence/sunny-orchard-painted-acceptance/49-slot-candidate/shell-visual/inset-402x874-44-34/01-lobby-default.png
[lobby-selected-full]: ../../unify-runtime-ui-visual-system/evidence/sunny-orchard-painted-acceptance/49-slot-candidate/shell-visual/full-402x874/02-lobby-alternate-selection.png
[lobby-selected-min]: ../../unify-runtime-ui-visual-system/evidence/sunny-orchard-painted-acceptance/49-slot-candidate/shell-visual/inset-360x800-32-24/02-lobby-alternate-selection.png
[lobby-loading-full]: ../../unify-runtime-ui-visual-system/evidence/sunny-orchard-painted-acceptance/49-slot-candidate/shell-visual/full-402x874/03-lobby-transition.png
[lobby-loading-inset]: ../../unify-runtime-ui-visual-system/evidence/sunny-orchard-painted-acceptance/49-slot-candidate/shell-visual/inset-402x874-44-34/03-lobby-transition.png
[battle-between-full]: ../../unify-runtime-ui-visual-system/evidence/sunny-orchard-painted-acceptance/49-slot-candidate/cross-route/full-402x874-battle/03-between-wave.png
[battle-between-inset]: ../../unify-runtime-ui-visual-system/evidence/sunny-orchard-painted-acceptance/49-slot-candidate/cross-route/inset-402x874-44-34-battle/03-between-wave.png
[battle-selected-full]: ../../unify-runtime-ui-visual-system/evidence/sunny-orchard-painted-acceptance/49-slot-candidate/cross-route/full-402x874-battle/09-selected-tool.png
[battle-selected-inset]: ../../unify-runtime-ui-visual-system/evidence/sunny-orchard-painted-acceptance/49-slot-candidate/cross-route/inset-402x874-44-34-battle/09-selected-tool.png
[battle-legal-full]: ../../unify-runtime-ui-visual-system/evidence/sunny-orchard-painted-acceptance/49-slot-candidate/cross-route/full-402x874-battle/11-legal-drag-cue.png
[battle-illegal-full]: ../../unify-runtime-ui-visual-system/evidence/sunny-orchard-painted-acceptance/49-slot-candidate/cross-route/full-402x874-battle/12-illegal-drag-cue.png
[battle-legal-inset]: ../../unify-runtime-ui-visual-system/evidence/sunny-orchard-painted-acceptance/49-slot-candidate/cross-route/inset-402x874-44-34-battle/11-legal-drag-cue.png
[battle-illegal-inset]: ../../unify-runtime-ui-visual-system/evidence/sunny-orchard-painted-acceptance/49-slot-candidate/cross-route/inset-402x874-44-34-battle/12-illegal-drag-cue.png
[battle-detail-full]: ../../unify-runtime-ui-visual-system/evidence/sunny-orchard-painted-acceptance/49-slot-candidate/cross-route/full-402x874-battle/14-plant-detail.png
[battle-detail-inset]: ../../unify-runtime-ui-visual-system/evidence/sunny-orchard-painted-acceptance/49-slot-candidate/cross-route/inset-402x874-44-34-battle/14-plant-detail.png
[battle-paused-full]: ../../unify-runtime-ui-visual-system/evidence/sunny-orchard-painted-acceptance/49-slot-candidate/cross-route/full-402x874-battle/05-paused.png
[battle-paused-inset]: ../../unify-runtime-ui-visual-system/evidence/sunny-orchard-painted-acceptance/49-slot-candidate/cross-route/inset-402x874-44-34-battle/05-paused.png
[battle-terminal-full]: ../../unify-runtime-ui-visual-system/evidence/sunny-orchard-painted-acceptance/49-slot-candidate/cross-route/full-402x874-battle/17-battle-terminal-victory.png
[battle-terminal-inset]: ../../unify-runtime-ui-visual-system/evidence/sunny-orchard-painted-acceptance/49-slot-candidate/cross-route/inset-402x874-44-34-battle/17-battle-terminal-victory.png
[settlement-victory-full]: ../../unify-runtime-ui-visual-system/evidence/sunny-orchard-painted-acceptance/49-slot-candidate/cross-route/full-402x874-flow-victory/03-settlement-victory.png
[settlement-victory-inset]: ../../unify-runtime-ui-visual-system/evidence/sunny-orchard-painted-acceptance/49-slot-candidate/cross-route/inset-402x874-44-34-flow-victory/03-settlement-victory.png
[settlement-defeat-full]: ../../unify-runtime-ui-visual-system/evidence/sunny-orchard-painted-acceptance/49-slot-candidate/cross-route/full-402x874-flow-defeat/03-settlement-defeat.png
[settlement-defeat-inset]: ../../unify-runtime-ui-visual-system/evidence/sunny-orchard-painted-acceptance/49-slot-candidate/cross-route/inset-402x874-44-34-flow-defeat/03-settlement-defeat.png
[settlement-return]: ../../unify-runtime-ui-visual-system/evidence/sunny-orchard-painted-acceptance/49-slot-candidate/cross-route/full-402x874-flow-victory/04-returned-lobby.png
[settlement-retry]: ../../unify-runtime-ui-visual-system/evidence/sunny-orchard-painted-acceptance/49-slot-candidate/cross-route/full-402x874-flow-victory/05-retry-battle.png
