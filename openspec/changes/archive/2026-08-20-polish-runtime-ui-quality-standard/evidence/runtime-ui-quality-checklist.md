# Normative runtime UI quality checklist

This checklist is the task-1.2 human-readable contract. Exact values are also
recorded in [`runtime-ui-quality-profile.json`](runtime-ui-quality-profile.json)
so task 2.1 can move them into one editor-owned profile. Presenters and tests
must consume that single implementation authority rather than copying numbers.

## 1. Geometry matrix and ownership

- Required canvases are 360×800, 375×812, 402×874, and 430×932.
- Every canvas is evaluated full-screen and with the representative top/bottom
  inset pair: 32/24, 40/21, 44/34, and 50/36 respectively.
- All layout values are logical points in the 402×874 design space before the
  existing safe-area transform.
- Draw and hit rectangles for an interactive component come from the same
  layout object. Any approved external Lobby/Settlement/Battle-chrome change
  updates both authorities and reruns input mapping at all eight geometries.
- The Battlefield interaction geometry is protected. Header, tray, detail, and
  modal chrome may be refined; planting cells, route cells, pot/plant targets,
  drag targets, and command order may not drift.

## 2. Typography and copy

The packaged Noto Sans SC font is mandatory. The semantic roles are:

| Role | Size / line | Weight | Typical use |
| --- | --- | --- | --- |
| Display | 40 / 46 | Bold | Rare hero/result treatment only |
| ScreenTitle | 32 / 38 | Bold | Lobby and Settlement route titles |
| SectionTitle | 28 / 34 | Bold | Modal/result/major section title |
| Body | 20 / 28 | Normal | Player-facing explanation or status |
| ControlLabel | 20 / 24 | Bold | Card/action label and Battle header title |
| Metric | 24 / 28 | Bold | Standalone metric value where height permits |
| Supplemental | 16 / 22 | Normal | Card body, compact metric/status, slot count |

Acceptance rules:

- No player-facing font is smaller than 15 logical points at the reference
  viewport. Comparable repeated rows have baseline and center differences of
  at most 1 logical point.
- Screen/section/action/card titles, card bodies, metric labels/values, and
  compact header/status content are declared one-line. Only body messages and
  compact transient status may use a finite two-line split.
- `CalcSize`/`CalcHeight` with the packaged font must fit the authoritative Rect
  at all eight geometries. Ellipsis, implicit font shrinking, hidden overflow,
  unbounded word wrap, and runtime truncation are failures.
- A two-line copy is split into two explicit no-wrap/Clip line rectangles and
  must reconstruct the complete source sentence exactly. An icon/indicator
  reserves its own rectangle before text is measured.
- Text is at least 4 logical points from a component stroke or ornament. Text
  may overlay a ribbon only inside an explicitly declared safe center; leaf,
  knot, highlight, or banner tails may not cross glyphs.
- Internal identifiers such as `orchard-01` are not player copy. Lobby CTA and
  Settlement metrics use the finite localized level display name.

### Required text anatomy by route

| Route/component | Role | Alignment | Lines | Rect rule |
| --- | --- | --- | ---: | --- |
| Bootstrap title | SectionTitle | MiddleLeft | 1 | `Title`; no state glyph in the text Rect |
| Bootstrap status/error | Body | MiddleLeft | 1, or explicit 2 only for approved copy | `Status`; one leading state-glyph Rect |
| Lobby title | ScreenTitle | MiddleCenter | 1 | `Title` ribbon safe center |
| Lobby card title/body | ControlLabel / Supplemental | MiddleLeft | 1 / 1 | Separate Rects after thumbnail; neither intersects marker/transient indicator |
| Lobby primary action | ControlLabel | optical group center | 1 | Icon + 4–8 gap + measured label centered as one group |
| Battle header title | ControlLabel | MiddleLeft | 1 | `HeaderTitle` |
| Battle header metric | Supplemental label/value | shared baseline | 1 | Icon, label, and value each have non-zero contained Rects |
| Battle board status | Supplemental | MiddleLeft | 1 or explicit compact 2 | Separate from `WaveAction` by at least 8 |
| Battle tray title | Supplemental | MiddleLeft | 1 | At least 4 from panel top stroke and 4 from first slot |
| Battle detail | ControlLabel / Supplemental | MiddleLeft | 1 / at most 2 | Separate title/body/close Rects |
| Battle modal | SectionTitle / Body / ControlLabel | centered by region | 1 / at most 2 / 1 | Title, message, vista/banner, state indicator, and actions never overlap |
| Settlement result | SectionTitle | MiddleCenter | 1 | Banner safe center, one state indicator |
| Settlement metric | Supplemental label/value | shared baseline | 1 | Repeated icon/label/value columns align within 1 |
| Settlement actions | ControlLabel | optical group center | 1 | Retry primary before Return quiet |

## 3. Spacing, containment, and touch

- Use the 4-point grid. Standard spacing is 4, 8, 12, 16, 24, or 32;
  route-specific optical compensation is bounded to ±2 logical points and is
  recorded rather than hidden in a texture.
- Icon-to-text gap is 4–8; component-to-component gap is at least 8; normal
  content inset is at least 8; text-to-visible-stroke gap is at least 4.
- The shortest interactive dimension is at least 44 logical points. Pressed
  visual offsets do not move the hit Rect.
- No text, icon, marker, indicator, or illustration escapes its component Rect
  or safe area. Repeated rows/columns have equal rhythm unless the hierarchy
  explicitly declares a larger primary item.
- On Lobby and Settlement, the last visible primary control should leave no
  more than 30% of the safe height as undifferentiated lower background. Larger
  whitespace requires a deliberate orchard illustration/ornament, not empty
  paper.

## 4. Color, contrast, and state semantics

Contrast is measured from the **rendered** foreground/background after texture,
tint, opacity, alpha, anti-aliasing, and scrim composition:

- small/regular player text: at least `4.5:1`;
- large or bold text: at least `3.0:1`;
- disabled informational copy that remains visible: at least `3.0:1`;
- essential icons, state marks, focus/selection boundaries: at least `3.0:1`.

Loading and Disabled are semantic states, not permission to fade all content:

- Loading keeps its label at the applicable text contrast, adds a spinner and
  finite loading copy, disables the hit target, and may dim only the surface or
  nonessential decoration.
- Disabled keeps the control/card identity readable at `3.0:1`, adds the
  disabled glyph/shape, and disables input. Opacity alone is insufficient.
- Selected uses surface/border plus medallion/check; Pressed uses position/depth
  plus surface change; Success/Warning/Error use an independent badge and copy.
- A state remains distinguishable in grayscale and never relies on hue alone.
- Error/Warning/Success tint does not recolor an entire neutral card if doing so
  lowers text contrast. Use a neutral readable surface plus semantic indicator.

The current token-only reference calculations over `baseSurface #FFF6E0` are:
normal primary `5.1833:1`, Loading primary at opacity 0.72 `2.9934:1`, and
Disabled primary at opacity 0.58 `2.3377:1`. Therefore applying state opacity to
text is a known failure; a fix must separate surface/decorative opacity from
text/icon contrast and then measure real WebGL pixels.

## 5. Icon optical box and family weight

- Common icons/markers/indicators use a stable 96×96 source canvas, 12-source-
  pixel safe inset, and 2 source pixels per logical point.
- Nontransparent pixels remain inside the safe inset; fully transparent edge
  pixels have no contaminating visible RGB fringe after bilinear sampling.
- Alpha-weighted centroid offset is at most 4 source pixels per axis (2 logical
  points). Major alpha dimension for the common family is 60–72 source pixels;
  a semantic narrow shape must be explicitly classified and still pass runtime
  legibility.
- At the smallest runtime draw, the visible alpha box has a short edge of at
  least 16 logical points, major edge of at least 18, and critical stroke of at
  least 2. Legal/illegal/success/warning/error badges use comparable optical
  weight.
- Icon + label is centered as one optical group. Raw transparent-canvas center
  is not accepted as proof of visual centering.

## 6. Nine-slice and illustration rules

For current painted nine-slice surfaces:

- source canvas 128×128, slice border 32 source pixels, safe inset 20 source
  pixels, and 2 source pixels per logical point;
- destination width/height is at least 32 logical points before scaling;
- leaves, flowers, wood knots, hard highlights, and asymmetric ornamentation
  remain outside the stretch center;
- snapped outer and four inner boundaries cover every device row/column exactly
  once; zero missing or double-painted device pixels and zero seam tolerance;
- mirror variants preserve the same protected bands and source coverage.

Illustrations are fixed-aspect content, never nine-sliced:

- aspect-ratio error is at most 1%; aspect-fit bars larger than 8 logical points
  in either direction fail and require a new authoritative destination Rect;
- Lobby thumbnails render at least 72×46 logical points; result vista renders at
  least 128×72;
- no crop, stretch, baked runtime text, or text overlap; text over a dedicated
  ribbon uses only its declared safe center;
- illustration remains secondary to player copy and controls, but orchard
  identity must still be recognizable with copy hidden.

## 7. Route hierarchy

1. **Bootstrap:** title → finite state/error → Retry when available. One status
   badge is enough; the modal must not contain unexplained empty height.
2. **Lobby:** title ribbon → three illustrated level cards → selected state →
   primary CTA → optional finite status. Thumbnail/title are primary, body is
   secondary, and internal content IDs are hidden.
3. **Battle:** three readable header metrics and pause/speed → protected
   battlefield → board status/wave action → tool tray → nursery → refresh →
   optional detail/modal. Compactness cannot erase labels or state cues.
4. **Settlement:** route title → outcome banner → aspect-correct orchard vista +
   aligned metric group → Retry primary → Return quiet → optional status. Only
   one result-state badge appears in the declared result region.

Across all routes, one theme/ArtSet identity is required; review boards and
source masters are not release dependencies. Default skin, legacy chrome,
mixed-set binding, filename lookup, and fallback resources are hard failures.

## 8. Review gate

A candidate fails immediately on any open Blocker/High in
[`severity-ranked-defects.md`](severity-ranked-defects.md). Passing source
measurement alone is insufficient: the final gate requires packaged-font Editor
measurement and original-resolution WebGL review of every required state at the
supported matrix, including measured contrast, four-sided nine-slice probes,
and real input/hash evidence.
