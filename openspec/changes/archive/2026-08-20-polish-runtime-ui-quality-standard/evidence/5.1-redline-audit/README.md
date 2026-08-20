# 5.1 user-rejected alignment redline audit

Status: **rework required**. This is a read-only audit of the rejected build;
it does not assign a replacement aesthetic score. The prior `100/100`
conclusion is withdrawn because it proved finite rectangles and gates while
missing the actual desktop-host crop and several rendered-group/page-balance
failures.

The machine-readable authority for every current/target rectangle and tolerance
in this audit is [`audit.json`](audit.json). All coordinates below are logical
points in the 402×874 design space unless a row explicitly says CSS pixels.

## First gate: actual 1280×720 host

The live DOM observation is `canvas=402×874` with the desktop container at
`y=-98.4`. The current generated template centers a fixed canvas/container and
also leaves the Unity footer in the transformed box. At 1280×720 the canvas is
therefore not contained: about 98.4 CSS px are above the viewport and 55.6 CSS
px are below it. Route screenshots cannot override this failure.

The release target is uniform contain:

- `scale=min(1280/402,720/874)=0.823798627`;
- target canvas `331.167×720` at `(474.4165,0)`;
- both axes use the same scale within `0.001`;
- all four canvas edges stay inside the viewport within `0.5` CSS px;
- canvas and viewport centers differ by at most `1` CSS px;
- document scroll overflow is at most `0.5` CSS px;
- a canvas-relative pointer round-trip differs by at most `0.5` logical point.

See [`host-1280x720-redline.svg`](host-1280x720-redline.svg). The diagram is a
DOM-geometry redline, not a replacement or reconstructed product screenshot.

## Shared optical group gate

Containment of separate icon and label rectangles is insufficient. For every
Bootstrap, Lobby, Battle, modal, and Settlement action that has both icon and
label, the union of the actual icon alpha bounds and packaged-font glyph bounds
must center in the action within `2` logical points on both axes. Visible
icon-to-glyph space is `4–8`; both remain at least `4` from the visible stroke.

Lobby Start already opts into centered group anatomy. Bootstrap Retry, Battle
Wave/Refresh/modal actions, and Settlement Retry/Return still use a leading
icon plus an independently centered trailing label region and are High until
they use the same centered anatomy. Loading and Disabled use the identical
group geometry; only state presentation and hit availability differ.

Repeated header/result metrics use one anatomy: peer label/value baselines and
icon centers differ by at most `1`, the complete rendered group centers within
`2`, and icon alpha remains at least `8` from the row/card border. These are
raster-aware measurements, not transparent-canvas centers.

## Lobby redline

[Original 402 full capture](../4.2-preflight/canonical/shell-visual-402x874-full/01-lobby-default.png)
and [`lobby-402x874-redline.svg`](lobby-402x874-redline.svg) show the current
meaningful group ending at `y=626`, centered at `y=328`, with 248 logical points
of lower paper. That is only just inside the old 30% threshold and still reads
as unfinished/top-heavy. The three `370×136` cards are also materially flatter
than the approved painted component proof.

Target reference rectangles:

| Component | Target Rect |
| --- | --- |
| Title | `(16,54,370,56)` |
| Cards | `(16,130,370,176)`, `(16,318,370,176)`, `(16,506,370,176)` |
| Start | `(16,702,370,72)` |
| Optional status | `(16,790,370,58)` |

Normal occupied bounds become `y=54..774`, center `414` (23 from the safe
center), leaving 100 logical points below. Within each card, use a
`164×104` thumbnail frame at relative `(12,36)`, 6-point art inset, title
`(196,34,162,44)`, body `(196,90,162,44)`, a contained 48 marker, and a
contained 28 transient cue. The thumbnail+copy rendered union centers within
`2`; the marker may overlap the thumbnail corner visually but may not cover
title/body glyphs. Card and Start hit rectangles must be these same revised
authorities.

## Battle redline

[Original ready capture](../4.2-preflight/canonical/battle-402x874-full/01-ready.png)
and [`battle-402x874-redline.svg`](battle-402x874-redline.svg) separate a
protected projection guard from chrome defects.

The grid is measured relative to `Battlefield.MapViewportRect`, not the whole
`Board` or `BattleSurface`:

- Map viewport `(0,72,402,438)`;
- Grid `(8,122.125,386,337.75)`;
- gutters `L/R=8`, `T/B=50.125`; opposite-pair delta at most `1`.

This geometry passes and must remain the single draw/hit authority. Brown route
tiles on the top and right are grid content, not empty gutter. No plant, pot,
route, drag, or command coordinate may change.

Header metric rectangles remain `(16,36,82,26)`, `(106,36,76,26)`, and
`(190,36,72,26)` with center `y=49`; peer baselines/centers must be within `1`.
Board status `(8,522,386,48)`, compact status `(16,522,186,48)`, Wave action
`(210,526,184,44)`, and Tool tray `(8,580,386,68)` provide a 10-point gap.
Tray title glyphs must remain at least 4 from the panel stroke and 4 from the
first slot. The visible Battle defects are the shared Wave/Refresh/modal action
group imbalance, not a reason to move battlefield interaction cells.

## Settlement redline

[Original victory capture](../4.2-preflight/canonical/flow-victory-402x874-full/03-settlement-victory.png)
and [`settlement-402x874-redline.svg`](settlement-402x874-redline.svg) show three
icons pressing the left of compact metric rows, a small vista competing with
the metrics, independently anchored button icons, and the same `y=626` normal
content endpoint as Lobby.

Target reference rectangles:

| Component | Target Rect |
| --- | --- |
| Title | `(16,54,370,56)` |
| Result card | `(16,130,370,474)` |
| Banner / outcome / indicator | `(58,146,286,72)` / `(98,156,206,52)` / `(308,168,28,28)` |
| Orchard vista | `(32,234,338,190)` |
| Metric rows | `(32,436,338,48)`, `(32,492,338,48)`, `(32,548,338,48)` |
| Retry / Return | `(16,624,370,72)` / `(16,712,370,64)` |
| Optional status | `(16,792,370,58)` |

The `338×190` vista stays within 1% of 16:9. Each metric uses aligned
icon/label/value columns, keeps icon alpha 8 inside the row/card, and centers
the rendered group. Normal occupied bounds end at `y=776`, leaving 98 logical
points. Retry/Return use the shared centered action anatomy and their revised
draw/hit rectangles are one authority.

## Terminal redline

[Original victory terminal](../4.2-preflight/canonical/battle-402x874-full/17-battle-terminal-victory.png)
and [`terminal-402x874-redline.svg`](terminal-402x874-redline.svg) show a
prominent painted result banner with no text or state payload. Empty decoration
between an explicit terminal title and result detail has no semantic role.

Keep the already non-overlapping modal geometry, but give the banner one finite
line: `胜利` or `失败` in `(102,360,198,48)`. Title, banner, vista, message,
indicator, and Restart stay inside `Modal=(28,270,346,320)`; their rendered
union centers within `2`. Restart also uses the centered action anatomy. The
alternative is to remove the banner draw entirely; keeping an empty banner is
the only forbidden outcome. This audit chooses the finite-copy option because
the result-banner semantic slot already exists and matches the approved proof.

## Severity and owners

| ID | Severity | Owner | Failure / required proof |
| --- | --- | --- | --- |
| R-01 | **Blocker** | Host/Foundation | Complete canvas is clipped in 1280×720. Project-owned template must pass contain, scroll, uniform-scale, and pointer mapping. |
| R-02 | **High** | Foundation | Icon+label actions are visibly unbalanced. Shared rendered-group anatomy must pass every caller/state. |
| R-03 | **High** | Foundation | Lobby card proportion and page visual center diverge from the approved proof. Revised draw/hit rectangles must pass all eight portrait transforms. |
| R-04 | **High** | Foundation | Settlement metric icons press edges, vista is underweighted, and route is top-heavy. Revised rows/vista/actions must pass raster and real-input gates. |
| R-05 | **High** | Foundation | Terminal result banner is empty. It must own finite outcome copy or be removed; this audit selects finite copy. |
| R-06 | Guard | Foundation | Battlefield opposite gutters are currently symmetric. Preserve projection/draw/hit identity while surrounding chrome is corrected. |

ArtHost acceptance remains unchanged but becomes raster-significant here:
96×96 icon canvases keep 12-source-pixel safe inset, alpha centroid at most
4 source pixels per axis, and comparable family weight. Nine-slice surfaces
retain zero missing/double-painted device rows/columns; none of the new larger
rectangles authorizes leaves or highlights in the stretch center.

## Scale and acceptance boundary

All target Shell rectangles are transformed through the same existing safe-area
scale, `min(safeWidth/402,safeHeight/874)`, for 360/375/402/430 full and inset.
Draw and hit use one layout source. A 2-point-outside probe must miss, and every
center probe must hit the expected semantic control. The final WebGL gate starts
with a green 1280×720 host before it may repeat the portrait matrix.

