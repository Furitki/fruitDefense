# Reference-faithful Battle component decomposition

## Status and measurement basis

- Reference: `sky-paper-orchard-reference.png`, 850×1850.
- Gate A target: 402×874 logical points.
- The values below translate visible component bands into explicit project-owned
  layout rectangles. They are authoring targets with a ±2 logical-point raster
  tolerance, not runtime image sampling or a release dependency.
- Copy, numbers, gameplay entities, commands, and map topology remain owned by
  the game. The reference owns component anatomy, relative scale, nesting, and
  top-to-bottom rhythm for this gate.

## Page-level composition

| Component | Reference visual bounds (approx.) | 402×874 target | Semantic owner |
| --- | --- | --- | --- |
| Floating Header | `(30,76,790,241)` | `(14,36,374,114)` | `surface.panel-raised` |
| Warm-paper page shell | `(30,327,790,1476)` | `(14,154,374,698)` | `surface.safe-area` |
| Inset gameplay stage | `(52,357,746,706)` | `(22,168,358,338)` | `surface.gameplay-stage` |
| Phase/Wave row | `(59,1098,738,102)` | `(24,518,354,52)` | `surface.status` + `action.primary` |
| Build/Context section | `(49,1228,751,179)` | `(24,578,354,88)` | `surface.panel-standard` + `slot.tool` |
| Nursery section | `(49,1427,751,187)` | `(24,674,354,92)` | `surface.panel-standard` + `slot.nursery` |
| Bottom refresh action | `(52,1640,744,132)` | `(24,774,354,64)` | `action.secondary` |

The Header and page shell share the same outer gutter. The stage and lower
sections are nested inside the page shell. The page shell is a light paper
surface; it does not duplicate the gameplay-stage heavy outline.

## Header anatomy

| Child | 402×874 target | Required construction |
| --- | --- | --- |
| Screen title | `(40,52,210,38)` | large soil-brown display face, left aligned |
| Pause | `(264,50,48,48)` | ivory outer rim, yellow face, highlight, outline, bottom shadow, brown glyph |
| Speed | `(318,50,56,48)` | same family as Pause; compact multiplier remains runtime text |
| Sun capsule | `(28,101,112,40)` | independent raised cream metric capsule |
| Core capsule | `(145,101,112,40)` | same geometry and baseline |
| Wave capsule | `(262,101,112,40)` | same geometry and baseline |

The v1 divider-only metric row is invalid. Every metric has its own visible
surface and one optically centered icon/label/value group.

## Stage and lower-section anatomy

- The stage is inset by 8 points from the page shell and retains the one
  authoritative `BattlefieldProjection` for drawing, hits, dragging, range,
  and acceptance coordinates. `BoardPadding=2` yields a 354-point content/grid
  width and 44.25-point cells across eight columns; interaction targets must not
  shrink below 44 points.
- The stage frame has an ivory outer rail and short shadow around the soil field;
  gameplay terrain remains separate content and must not be baked into the UI
  frame.
- The phase block is sunlight yellow with the same rounded rim/shadow language.
  When Wave is actionable, status is `(24,518,168,52)` and Wave is
  `(204,518,174,52)` with a 12-point gap. Without an action, status owns the
  full `(24,518,354,52)` rectangle and there is no hidden Wave hit target.
- Context title is `(32,582,120,24)`. Four recipe cards use y=610, height=48,
  x=`32/118.5/205/291.5`, width=78.5, gap=8. Each has a roughly 34-point main
  icon, centered multiplication mark, and 18–22-point right-side pot/target
  glyph. Positive inventory is a count-only lower-left corner badge; zero is
  communicated by the disabled card treatment instead of a repeated `×0` row.
  Inventory is never a second body column. The
  visible recipe composition does not change tool commands, drag targets, or
  inventory values.
- Nursery title is `(32,678,120,24)`. Its five slots are
  `(32/102/172/242/312,706,58,52)`, with 12-point gaps. Empty slots show an
  ivory paper interior, sunlight dashed border, and small restrained leaf corner;
  occupied content remains gameplay art on the unchanged hit/drag rectangle.
- Refresh is a full-width, thick, rounded saturated-green action with a cream
  outer rail, darker lower edge, upper highlight, and one centered icon/label
  group. Its semantic role stays `action.secondary`, but its content polarity is
  inverse white (`LightOnDark`) rather than the v1 dark-on-light treatment.

## Material layer contract

Every card/action surface that is visually raised contains these independently
visible layers in the final raster:

1. transparent exterior gutter;
2. short warm-brown bottom shadow;
3. cream or semantic outer rim;
4. soil-brown key outline;
5. rounded semantic face;
6. restrained upper/inner highlight;
7. protected center/slice region for runtime content.

A flat gradient with a thin border is not an acceptable substitute. Nine-slice
corners, rim, outline, and shadow must remain stable at the narrowest recipe slot
and widest bottom action.

## V1 paths to replace or delete

- Replace `BattleUiLayout`'s zero-gutter `FullWidthTrack` Header/Stage geometry
  with explicit floating Header, page shell, and inset Stage owners.
- Replace `DrawStandardPanel(... layout.Header)` with the raised Header anatomy.
- Replace divider-only compact metrics with three real `surface.metric` cards;
  metric dividers are not drawn in Battle Header.
- Add one explicit page-shell draw before the stage/lower tracks; do not emulate
  it by coloring the screen background or by expanding the stage frame.
- Replace the v1 flat `surface.panel-*`, `surface.metric`, `surface.status`,
  `action.*`, `slot.tool`, `slot.nursery`, and `surface.gameplay-stage` masters in
  place through the deterministic exporter. Do not keep a selectable v1 set,
  source branch, fallback, or Presenter-local color patch.
- Remove Battle-specific assumptions that metric surfaces are invisible,
  Context/Nursery are free-standing on sky, or screen corners are absent.
- Preserve the already-removed in-stage Wave/control-strip path; do not restore
  it to obtain reference similarity.

## Gate A human checklist

- The screen reads as one floating Header plus one large rounded paper page,
  rather than disconnected flat rows on blue.
- Yellow compact buttons and green actions visibly show rim, face, highlight,
  outline, and short shadow at final WebGL scale.
- Three metric capsules, four recipe cards, and five nursery slots form clearly
  repeated families with reference-like scale and padding.
- Stage, phase, build, nursery, and refresh bands follow the target composition
  before safe-area scaling.
- Similar palette without the above anatomy remains a failed Gate A.
