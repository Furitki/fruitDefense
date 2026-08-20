# Closed severity-ranked defect inventory

Status: **closed**. The baseline tables preserve what failed in the frozen
50-point payload; the closure ledger records the final implementation and
canonical proof. All 1 Blocker, 10 High, and 4 Medium items pass the
[normative checklist](runtime-ui-quality-checklist.md). The later 5.1
alignment redline is also closed below against the final 5.9 evidence.

## Severity and ownership

- **Blocker:** required player copy/control is obscured, overlapping, missing,
  or semantically ambiguous; publishing is blocked.
- **High:** the required quality standard fails in a supported state/geometry;
  it must be corrected and rerun before canonical acceptance.
- **Medium:** visible polish defect with no current loss of copy/input; close it
  in the route polish pass or explicitly retain it as a non-blocking observation.
- **Foundation owner:** C# layout/style/copy/state/draw-hit authority.
- **Art owner:** reviewed master/export/alpha/optical weight/import metadata.
- Shared defects list both owners; neither side may relax the standard alone.

All proposed geometry is in the 402×874 design space. The same layout is scaled
through the existing safe-area transform at 360/375/402/430 full/inset. Exact
before/after values below are an audit-approved implementation target; if the
owner proves an equivalent simpler layout, it must meet the same measurable
pass criteria and update this row before closure.

## Baseline Blocker and High defects

| ID | Severity | State / evidence | Current authority and defect | Owner | Verifiable correction and pass criteria | Baseline status |
| --- | --- | --- | --- | --- | --- | --- |
| B-01 | **Blocker** | Battle terminal [full][terminal-full], [inset][terminal-inset] | `BattleUiLayout`: `Modal=(36,300,330,244)`, `ModalResultBanner=(152,384,194,64)`, `ModalTerminalMessage=(160,390,146,52)`, `ModalResultIndicator=(314,398,24,24)`. Message, banner center/tails, leaf ornament, and indicator occupy the same pixels; terminal copy is visibly crossed. | Foundation | Use a terminal-specific non-overlapping composition: `Modal=(28,270,346,320)`, `ModalTitle=(48,292,306,56)`, `ModalResultBanner=(70,352,262,64)`, `ModalOrchardVista=(56,424,112,63)`, `ModalTerminalMessage=(180,420,142,64)`, `ModalResultIndicator=(328,438,24,24)`, single `ModalAction=(90,510,222,52)`. The message is explicit ≤2 lines; indicator has ≥6 gap; vista is 16:9; banner text uses its safe center; restart hit center updates to the same layout. Pause may keep its existing modal. All eight transforms have zero overlap/clip and real terminal restart input passes. | Failed at baseline |
| H-01 | **High** | Battle ready/active/between/dense/restart [ready][battle-ready], [between][battle-between] | `SunMetric=(16,38,88,24)`, `LivesMetric=(104,38,82,24)`, `WaveMetric=(186,38,76,24)` call non-compact `DrawMetric`, which draws a Metric value and Supplemental label below it. A 24-high Rect leaves the label a zero/near-zero region; screenshots show tiny icon/value bars without readable labels. | Foundation | Keep `Header=(8,8,386,60)` and field geometry unchanged. Use compact single-baseline anatomy: `HeaderTitle=(16,12,96,20)`; metric Rects `(16,36,82,26)`, `(106,36,76,26)`, `(190,36,72,26)` with dividers `(98,41,8,16)` and `(182,41,8,16)`. Each metric reserves icon ≥16, 4 gap, finite label, 2–4 gap, value; all Rects non-zero, Supplemental ≥16, peer baselines/centers differ ≤1, actual text contrast ≥4.5, and pause/speed hit Rects remain unchanged. | Failed at baseline |
| H-02 | **High** | Battle all non-modal states, especially [selected tool][battle-selected] and [detail][battle-detail] | `ToolTrayTitle=(16,580,180,18)` and `NurseryTrayTitle=(16,656,180,18)` begin on the panel top stroke. The label baseline visually cuts through the outline; current slots start at tray `y+18`. | Foundation | Keep external trays `(8,580,386,68)` / `(8,656,386,80)`. Move titles to `y+4`, height 16; move tool slots to `y+22`, height 44; nursery slots to `y+22`, height 54. Preserve ≥4 title/stroke and ≥2 title/slot visual gaps, touch ≥44, and derive all slot hit Rects from the new layout. Real selected tool and nursery inputs must remain mapped at all eight geometries. | Failed at baseline |
| H-03 | **High** | Bootstrap Loading/error; Lobby Loading/disabled; any disabled action | Theme feedback applies `LoadingOpacity=.72` and `DisabledOpacity=.58` to text through `DrawTextCore`. Against `baseSurface #FFF6E0`, primary text composites to approximately `2.9934:1` and `2.3377:1`. [Lobby Loading][lobby-loading] and [Bootstrap Loading][bootstrap-loading] visibly fade essential copy. | Foundation | Apply opacity to surface/decorative art only, or use a separately validated semantic text opacity/color. Loading text must meet 4.5:1, Disabled informational text 3.0:1, and essential state icons 3.0:1 in actual pixels. Loading retains spinner+copy and no hit; Disabled retains label+disabled glyph and no hit. Normal/Pressed/Selected behavior and primary CTA ≥3.0 remain unchanged. | Failed at baseline |
| H-04 | **High** | Battle legal/illegal drag [full pair][legal-full] [illegal][illegal-full], [inset pair][legal-inset] [illegal inset][illegal-inset] | `BattleUiLayout.CueBadge` is 24×24. `indicator.drag-legal` has alpha bbox only 52×70 on a 96 canvas, so its current visible short edge is about 13 logical points and is obscured by plant art. Hash difference is not sufficient perceptual proof. | Art + Foundation | Art: bring legal/illegal family alpha short edge to ≥64 source px, centroid ≤4 px/axis, and ≥2-logical critical stroke while preserving 96 canvas/GUID. Foundation: use a contained cue Rect whose rendered alpha box is ≥16×18 logical (up to a 28–32 badge without changing target hit geometry), and keep cue above dragged content. Full/inset screenshots must be distinguishable in grayscale without zoom and keep separate finite legal/illegal copy. | Failed at baseline |
| H-05 | **High** | Header speed; warning/error badges across Bootstrap/Battle/Settlement | Active inventory measures `icon.control-speed` centroid x `-5.725` source px, `indicator.warning` y `+4.749`, and `indicator.error` y `+4.411`, exceeding the 4-source-pixel/2-logical tolerance. The speed glyph appears left-heavy; warning/error sit low relative to peer badges. | Art | Deterministic in-place export on 96×96 canvas, safe inset 12, same paths/meta/GUIDs. Each centroid ≤4 source px/axis; common major bbox 60–72; runtime short edge ≥16 and stroke ≥2. Resource validator plus smallest-size full/inset screenshot comparison must pass. | Failed at baseline |
| H-06 | **High** | Settlement victory/defeat [full victory][settlement-victory], [inset defeat][settlement-defeat] | `OrchardVista=(x+18, result.y+82,100,168)` receives 256×144 (16:9) art via aspect-fit. Actual image is about 100×56 with roughly 112 logical points of unused destination height, weakening the v2 result hierarchy. | Foundation | Refine reference layout: `Title=(x,y+12,w,56)`, `ResultCard=(x,y+86,w,340)`, `ResultBanner=(x+51,y+98,268,72)`, `Outcome=(x+103,y+108,164,52)`, `OrchardVista=(x+16,y+194,144,81)`, metrics `(x+172,y+186,182,56)`, `(x+172,y+254,182,56)`, `(x+172,y+322,182,56)`. Vista aspect error ≤1%, displayed ≥128×72, bars ≤8; metric baselines align ≤1 and stay separate from banner/indicator. | Failed at baseline |
| H-07 | **High** | Lobby default/selected/Loading at all sizes [402 full][lobby-full], [360 inset][lobby-min] | Current reference offsets are title `+17/48`, cards `+92,+190,+288` at height 82, Start `+396/64`; essential content ends around y478 at 402 full, leaving about 378 px undifferentiated paper. Thumbnails remain only 84×54 and CTA exposes `orchard-*`. | Foundation | Reference offsets: title `+12/56`; cards `+80,+228,+376`, each height 136; Start `+544/64`; Status `+624/58`. At 402 full this yields `(16,30,370,56)`, cards `(16,98/246/394,370,136)`, Start `(16,562,370,64)`. Card frame target 144×92 with aspect-fit thumbnail ≥138×86 destination; title/body remain finite one-line and clear the 48 marker/28 transient cue. Replace internal ID with localized selected-level display name. Update card/Start hit Rects and acceptance control centers from the same layout; all eight real-input runs show no drift and lower unused safe area ≤30%. | Failed at baseline |
| H-08 | **High** | Settlement victory/defeat/Return/Retry [victory][settlement-victory], [defeat][settlement-defeat] | Current `ResultCard` auto-draws a state indicator and `ResultIndicator` draws another; their visual ownership is ambiguous. Main visible controls end around y539, leaving weak lower composition, and `完成关卡 orchard-01` exposes an internal ID. | Foundation | Use the H-06 340-high result card; draw its container with neutral surface state and exactly one explicit result badge in banner region `(x+287,y+118,28,28)`. Set Retry `(x,y+470,w,64)`, Return `(x,y+550,w,58)`, Status `(x,y+624,w,58)`; at 402 full these are y488/568/642. Use localized level display name. Update Return/Retry hit Rects and all expected control centers from the same layout; full/inset victory/defeat have one cue, ≤30% undifferentiated lower space, and both real inputs pass. | Failed at baseline |
| H-09 | **High** | Bootstrap initializing/error [loading][bootstrap-loading], [full error][bootstrap-error], [inset error][bootstrap-error-inset] | `Modal` is always 360×190 while no retry is available in the formal invalid-level case; title/status occupy only the upper portion. `DrawBlockingModal(Error)` adds an outer state badge and `DrawStatus(Error)` adds another. Status uses Standard wrapping rather than an explicit finite policy. | Foundation | State-specific layout: no-action Loading/error modal `(21,262,360,142)` at 402 full, Title `(41,278,320,34)`, Status `(41,318,320,45)`; initialization failure with Retry may keep 190 height and `(41,367,320,52)` action. Draw modal surface neutral and exactly one status badge. Error/loading copy is one explicit line (or approved 2-line split), meets H-03 contrast, remains within safe area at all eight geometries, and Retry hit mapping is unchanged where present. | Failed at baseline |
| H-10 | **High** | Battle plant detail/transient status [full][battle-detail], [inset][battle-detail-inset] | Detail body and compact transient copy use primary/state brown over amber/cream textures near the small-text threshold; the screenshot hierarchy is visually flat, and the selected-tool two-line status crowds its 48-high surface. | Foundation | Measure each finite transient/detail line from the catalog after raster composition: Supplemental/Body text ≥4.5, indicator ≥3, line Rects reconstruct full copy, and title/body/close have ≥8 gaps. Keep `Detail=(8,796,386,70)` and status/wave hit geometry unless a failing measurement proves otherwise. Full/inset plant detail must show two complete lines without border/action overlap. | Failed at baseline |

### Geometry scaling and hit acceptance for H-06/H-07/H-08

The proposed reference Rects are transformed by the existing
`scale=min(safeWidth/402,safeHeight/874)` and centered content frame. Acceptance
must compute, not hard-code, expected device Rects for:

| Viewport | Full | Inset |
| --- | --- | --- |
| 360×800 | 0/0 | top 32 / bottom 24 |
| 375×812 | 0/0 | top 40 / bottom 21 |
| 402×874 | 0/0 | top 44 / bottom 34 |
| 430×932 | 0/0 | top 50 / bottom 36 |

For each geometry: every draw Rect is contained by the GUI safe area; every
interactive center hits exactly the drawn component; 2 px outside each edge
does not hit it; Loading/transition disables the same Rect; no route/session or
command ordering changes. Battlefield target coordinates are byte-for-byte or
numerically unchanged.

## Baseline Medium defects and retained passes

| ID | Severity | Affected state | Finding | Owner / pass criterion | Baseline status |
| --- | --- | --- | --- | --- | --- |
| M-01 | Medium | Lobby default/selected | Card title/body color and stroke weight are close, so primary/secondary reading order is weaker than v2 even though copy fits. | Foundation: ensure title uses ControlLabel/Primary and body Supplemental/Secondary with measured 4.5; preserve selected marker clearance. | Failed at baseline |
| M-02 | Medium | Battle pause | The pause modal is contained but visually flat; outer warning badge is remote from the message. | Foundation: one intentional badge adjacent to message/title, ≥8 gap, no duplicate container cue; preserve Continue/Restart hits. | Failed at baseline |
| M-03 | Medium | Battle normal/dense | Tool/nursery and paper panels use many similar thin outlines, giving weak depth separation from the approved board. | Art + Foundation: shared surface hierarchy must remain distinguishable in grayscale without adding route-only fallback textures. | Failed at baseline |
| M-04 | Medium | Lobby/Settlement route titles | 32-point title is readable, but painted ribbon/text weight is flatter than the approved leaf-banner hierarchy. | Art: review ribbon center/edge weight; Foundation: keep safe-center text and do not add baked text. | Failed at baseline |
| P-01 | Pass | All canonical full/inset states | No known black/transparent background, four-side seam, default skin, legacy chrome, or mixed active ArtSet. | Preserve exact gate; regression is High or Blocker depending on affected content. | Guard |
| P-02 | Pass | Lobby CTA, tool selection, Return/Retry | Existing real hit/hash evidence passes. | Any geometry polish must rerun and produce the same semantic input outcomes. | Guard |

## Baseline required-state coverage

This table ensures every state named by the acceptance contract has an explicit
baseline disposition rather than inheriting a page-level verdict.

| Required state | Baseline disposition | Defect IDs / guard |
| --- | --- | --- |
| Bootstrap initializing | Fail | H-03, H-09 |
| Bootstrap blocking error | Fail | H-03, H-05, H-09 |
| Lobby default | Fail | H-07, M-01, M-04 |
| Lobby alternate selection | Fail | H-07, M-01; selected marker containment itself passes |
| Lobby Loading transition | Fail | H-03, H-07 |
| Battle ready | Fail | H-01, H-02, M-03 |
| Battle active wave | Fail | H-01, H-02, M-03 |
| Battle between-wave | Fail | H-01, H-02; finite one-line action/countdown remains a guard |
| Battle paused | Fail | M-02; no overlap or hit drift currently observed |
| Battle tool selected | Fail | H-02, H-10; real click/hash distinction remains a guard |
| Battle legal drag | Fail | H-04 |
| Battle illegal drag | Fail | H-04 |
| Battle plant detail | Fail | H-02, H-10 |
| Battle dense board | Fail | H-01, H-02, M-03 |
| Battle terminal victory/defeat preview | **Blocker** | B-01 |
| Battle restart / terminal restart | Fail | H-01, H-02; command and route behavior remain guards |
| Settlement victory | Fail | H-05, H-06, H-08, M-04 |
| Settlement defeat | Fail | H-05, H-06, H-08, M-04 |
| Settlement Return | Fail visual / pass input | H-08; real return hit/session evidence is a guard |
| Settlement Retry | Fail visual / pass input | H-08; real retry hit/session evidence is a guard |

## Final closure ledger

Each closure below records:

1. exact source/layout/art change and preserved GUID/hit authority;
2. before and after original-resolution screenshot links for full and inset;
3. packaged-font/resource/contrast gate name and measured values;
4. affected 360/375/402/430 full/inset results;
5. real input result where a hit Rect changed;
6. final canonical payload identity.

The exact payload identity, pass/fail categories, Unity markers, scope limits,
and resource-workflow proof are consolidated in the
[final quality audit](final-quality-audit.md). The earlier 4.2/4.3 evidence is
retained as pre-alignment-rework history. Current closure authority is the
[5.9 final acceptance](5.9-final-acceptance/README.md), its
[before/after index](5.9-final-acceptance/before-after.md), and the
[original-resolution manual audit](5.9-final-acceptance/manual-audit.md).

| ID | Final correction and measured result | Canonical after evidence | Final status |
| --- | --- | --- | --- |
| B-01 | Terminal-only layout owns non-overlapping title/banner/vista/two-line message/indicator/action Rects inside `Modal=(28,270,346,320)`. The banner carries finite `胜利`/`失败` copy; restart hit and draw use one action Rect and preserve command order. | [victory](5.9-final-acceptance/canonical/cross-route/full-402x874-battle/17-battle-terminal-victory.png), [defeat inset](5.9-final-acceptance/canonical/cross-route/inset-402x874-44-34-battle/17-battle-terminal-defeat.png), [manual review](5.9-final-acceptance/manual-audit.md#5-9-original-resolution-manual-audit) | **Closed** |
| H-01 | Header uses compact single-baseline metrics with non-zero icon/label/value Rects and two dividers; peer baseline/center tolerance is ≤1 logical point and icon ink stays inside each row. Pause/speed hit Rects and battlefield geometry remain protected. | [ready](5.9-final-acceptance/canonical/cross-route/full-402x874-battle/01-ready.png), [between-wave inset](5.9-final-acceptance/canonical/cross-route/inset-402x874-44-34-battle/03-between-wave.png) | **Closed** |
| H-02 | Tray titles have explicit stroke/slot clearance; tool/nursery slot draw and hit Rects derive from the same revised layout and retain ≥44 logical touch size. All supported geometries and real selected/nursery inputs pass. | [selected tool](5.9-final-acceptance/canonical/cross-route/full-402x874-battle/09-selected-tool.png), [detail inset](5.9-final-acceptance/canonical/cross-route/inset-402x874-44-34-battle/14-plant-detail.png) | **Closed** |
| H-03 | Loading/Disabled opacity affects decorative surfaces, not essential copy. Packaged-font text meets the finite state thresholds; Loading remains non-interactive with spinner, Disabled remains non-interactive with an independent glyph. | [Bootstrap Loading](5.9-final-acceptance/canonical/shell-visual/full-430x932/00-bootstrap-initializing.png), [Lobby Loading](5.9-final-acceptance/canonical/shell-visual/full-402x874/03-lobby-transition.png) | **Closed** |
| H-04 | Battle cue badge is 28 logical points without changing target hit geometry. Both production candidates satisfy legal/illegal alpha short-edge ≥64 source px, centroid ≤4 px/axis, and distinct shape/copy cues. | [legal](5.9-final-acceptance/canonical/cross-route/full-402x874-battle/11-legal-drag-cue.png), [illegal](5.9-final-acceptance/canonical/cross-route/full-402x874-battle/12-illegal-drag-cue.png), [resource proof](resource-polish/README.md) | **Closed** |
| H-05 | Painted speed/warning/error centroids are `(-3.909,+0.122)`, `(+0.049,+3.850)`, and `(+0.010,+3.520)` source px; the inactive production candidate also passes the same ≤4 px contract. Paths, metas and GUIDs are preserved. | [painted audit](resource-polish/post-fix-audit.json), [alternate audit](old-set-resource-polish/README.md) | **Closed** |
| H-06 | Settlement vista now uses the final `338×190` destination, stays within 1% of 16:9, and has enough visual weight beside three aligned `338×48` metric rows without competing with the result banner. | [victory](5.9-final-acceptance/canonical/cross-route/full-402x874-flow-victory/03-settlement-victory.png), [defeat inset](5.9-final-acceptance/canonical/cross-route/inset-402x874-44-34-flow-defeat/03-settlement-defeat.png) | **Closed** |
| H-07 | Lobby uses the final 56-title/three 176-card/72-action/58-status rhythm. Each card has a `164×104` illustration frame and a packaged-font-verified Chinese title/body split; card/Start draw-hit authority plus real clicks pass all supported geometries. | [default](5.9-final-acceptance/canonical/shell-visual/full-402x874/01-lobby-default.png), [selected inset](5.9-final-acceptance/canonical/shell-visual/inset-402x874-44-34/02-lobby-alternate-selection.png) | **Closed** |
| H-08 | Settlement has one explicit result badge on a neutral card, localized level copy, separated banner/vista/full-width metric rows, and lower Retry/Return/status rhythm. Revised draw/hit Rects are single-source; both real actions preserve route/session semantics. | [victory flow](5.9-final-acceptance/canonical/cross-route/full-402x874-flow-victory/flow-acceptance.json), [defeat inset flow](5.9-final-acceptance/canonical/cross-route/inset-402x874-44-34-flow-defeat/flow-acceptance.json) | **Closed** |
| H-09 | Bootstrap selects compact no-action or action modal geometry, draws one status cue, and uses finite one/two-line catalog copy. All supported safe-area cases fit; Retry keeps its authoritative hit mapping when present. | [error full](5.9-final-acceptance/canonical/cross-route/full-402x874-shell-error/00-bootstrap-blocking-error.png), [error inset](5.9-final-acceptance/canonical/cross-route/inset-402x874-44-34-shell-error/00-bootstrap-blocking-error.png), [Loading](5.9-final-acceptance/canonical/shell-visual/inset-402x874-44-34/00-bootstrap-initializing.png) | **Closed** |
| H-10 | The finite copy catalog measures every Battle status/detail string with packaged Noto. Short state text stays explicit single-line; long transient/detail copy uses at most two controlled lines and reconstructs the full sentence without action/indicator overlap. | [plant detail](5.9-final-acceptance/canonical/cross-route/full-402x874-battle/14-plant-detail.png), [plant detail inset](5.9-final-acceptance/canonical/cross-route/inset-402x874-44-34-battle/14-plant-detail.png) | **Closed** |
| M-01 | Lobby card title/body use ControlLabel/Primary and Supplemental/Secondary, retain marker/cue clearance, and pass rendered text contrast and packaged-font fit without truncating any of the three titles. | [default](5.9-final-acceptance/canonical/shell-visual/full-402x874/01-lobby-default.png), [selected](5.9-final-acceptance/canonical/shell-visual/full-402x874/02-lobby-alternate-selection.png) | **Closed** |
| M-02 | Pause modal uses one warning cue adjacent to its message with ≥8 logical gap; neutral surface and finite copy improve hierarchy while Continue/Restart draw-hit authority and command order remain unchanged. | [paused](5.9-final-acceptance/canonical/cross-route/full-402x874-battle/05-paused.png), [continued](5.9-final-acceptance/canonical/cross-route/full-402x874-battle/06-continued.png) | **Closed** |
| M-03 | Standard/raised/card/action surface roles are consistent across route chrome; deterministic resource validation and native-resolution review find no legacy/default/mixed surface or collapsed grayscale hierarchy. | [dense Battle](5.9-final-acceptance/canonical/cross-route/full-402x874-battle/13-dense-board.png), [manual review](5.9-final-acceptance/manual-audit.md) | **Closed** |
| M-04 | Lobby/Settlement titles use the refined leaf ribbon with a declared safe center; runtime Chinese remains separate text and no ornament crosses glyphs. | [Lobby](5.9-final-acceptance/canonical/shell-visual/full-402x874/01-lobby-default.png), [Settlement](5.9-final-acceptance/canonical/cross-route/full-402x874-flow-victory/03-settlement-victory.png) | **Closed** |

### Reopened alignment-redline closure

The 5.1 browser-host review reopened the visual conclusion after the original
1/10/4 ledger had closed. These redline items are additional closure records,
not a replacement score and not a relaxation of the original quality profile.

| ID | Final correction and proof | Final status |
| --- | --- | --- |
| R-01 | The project-owned host uniformly contains and centers the complete portrait canvas with no page scroll; all three desktop matrices preserve input mapping and one payload. See [host acceptance](5.3-webgl-host/README.md). | **Closed** |
| R-02 | Shared action anatomy centers icon+label rendered groups within 2 logical points; repeated metrics align peer baselines/centers within 1 and keep icon ink at least 8 inside rows. See [manual audit](5.9-final-acceptance/manual-audit.md). | **Closed** |
| R-03 | Lobby uses the final full-page rhythm and balanced thumbnail/copy anatomy; all three Chinese titles fit in the desktop host and portrait matrix without shrinking or truncation. See [before/after](5.9-final-acceptance/before-after.md). | **Closed** |
| R-04 | Settlement uses the enlarged vista, aligned full-width metric rows, centered actions, and intentional occupied-content bounds in full/inset states. See [before/after](5.9-final-acceptance/before-after.md). | **Closed** |
| R-05 | The terminal result banner now carries finite `胜利`/`失败` copy instead of empty decoration. | **Closed** |
| R-06 | Battlefield opposite gutters remain within 1 logical point and the same projection continues to drive drawing and hit testing. | **Guard preserved** |

## Final required-state coverage

All required Bootstrap initializing/error, Lobby default/selected/Loading,
Battle ready/active/between/paused/selected/legal/illegal/detail/dense/terminal/
restart, and Settlement victory/defeat/Return/Retry states pass. The canonical
matrix reports one theme and active ArtSet identity, no clip/stretch/overlap/
seam/mixed-set/input defect, and real route/session outcomes for Start, pause,
restart, Return and Retry. There are **0 open Blocker, 0 open High, and 0 open
Medium** product defects in this inventory. The retained transition-race
capture is classified as acceptance infrastructure evidence, not a product
defect.

[terminal-full]: ../../unify-runtime-ui-visual-system/evidence/sunny-orchard-painted-acceptance/49-slot-candidate/cross-route/full-402x874-battle/17-battle-terminal-victory.png
[terminal-inset]: ../../unify-runtime-ui-visual-system/evidence/sunny-orchard-painted-acceptance/49-slot-candidate/cross-route/inset-402x874-44-34-battle/17-battle-terminal-victory.png
[battle-ready]: ../../unify-runtime-ui-visual-system/evidence/sunny-orchard-painted-acceptance/49-slot-candidate/cross-route/full-402x874-battle/01-ready.png
[battle-between]: ../../unify-runtime-ui-visual-system/evidence/sunny-orchard-painted-acceptance/49-slot-candidate/cross-route/full-402x874-battle/03-between-wave.png
[battle-selected]: ../../unify-runtime-ui-visual-system/evidence/sunny-orchard-painted-acceptance/49-slot-candidate/cross-route/full-402x874-battle/09-selected-tool.png
[battle-detail]: ../../unify-runtime-ui-visual-system/evidence/sunny-orchard-painted-acceptance/49-slot-candidate/cross-route/full-402x874-battle/14-plant-detail.png
[battle-detail-inset]: ../../unify-runtime-ui-visual-system/evidence/sunny-orchard-painted-acceptance/49-slot-candidate/cross-route/inset-402x874-44-34-battle/14-plant-detail.png
[legal-full]: ../../unify-runtime-ui-visual-system/evidence/sunny-orchard-painted-acceptance/49-slot-candidate/cross-route/full-402x874-battle/11-legal-drag-cue.png
[illegal-full]: ../../unify-runtime-ui-visual-system/evidence/sunny-orchard-painted-acceptance/49-slot-candidate/cross-route/full-402x874-battle/12-illegal-drag-cue.png
[legal-inset]: ../../unify-runtime-ui-visual-system/evidence/sunny-orchard-painted-acceptance/49-slot-candidate/cross-route/inset-402x874-44-34-battle/11-legal-drag-cue.png
[illegal-inset]: ../../unify-runtime-ui-visual-system/evidence/sunny-orchard-painted-acceptance/49-slot-candidate/cross-route/inset-402x874-44-34-battle/12-illegal-drag-cue.png
[lobby-loading]: ../../unify-runtime-ui-visual-system/evidence/sunny-orchard-painted-acceptance/49-slot-candidate/shell-visual/full-402x874/03-lobby-transition.png
[bootstrap-loading]: ../../unify-runtime-ui-visual-system/evidence/sunny-orchard-painted-acceptance/49-slot-candidate/timing-regression/full-402x874-attempt-01/00-bootstrap-initializing.png
[settlement-victory]: ../../unify-runtime-ui-visual-system/evidence/sunny-orchard-painted-acceptance/49-slot-candidate/cross-route/full-402x874-flow-victory/03-settlement-victory.png
[settlement-defeat]: ../../unify-runtime-ui-visual-system/evidence/sunny-orchard-painted-acceptance/49-slot-candidate/cross-route/inset-402x874-44-34-flow-defeat/03-settlement-defeat.png
[lobby-full]: ../../unify-runtime-ui-visual-system/evidence/sunny-orchard-painted-acceptance/49-slot-candidate/shell-visual/full-402x874/01-lobby-default.png
[lobby-min]: ../../unify-runtime-ui-visual-system/evidence/sunny-orchard-painted-acceptance/49-slot-candidate/shell-visual/inset-360x800-32-24/01-lobby-default.png
[bootstrap-error]: ../../unify-runtime-ui-visual-system/evidence/sunny-orchard-painted-acceptance/49-slot-candidate/cross-route/full-402x874-shell-error/00-bootstrap-blocking-error.png
[bootstrap-error-inset]: ../../unify-runtime-ui-visual-system/evidence/sunny-orchard-painted-acceptance/49-slot-candidate/cross-route/inset-402x874-44-34-shell-error/00-bootstrap-blocking-error.png
